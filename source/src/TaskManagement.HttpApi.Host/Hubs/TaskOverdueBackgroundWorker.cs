using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Hubs;
using TaskManagement.Notifications;
using TaskManagement.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;

namespace TaskManagement;

public class TaskOverdueBackgroundWorker : AsyncPeriodicBackgroundWorkerBase, ITransientDependency
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaskOverdueBackgroundWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        Timer.Period = 30000; // 30 giây quét 1 lần
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var taskRepository = scope.ServiceProvider.GetRequiredService<IRepository<TaskItem, Guid>>();
            var notificationAppService = scope.ServiceProvider.GetRequiredService<INotificationAppService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            var now = DateTime.Now;

            // Lọc các task quá hạn nhưng chưa xử lý
            var overdueTasks = await taskRepository.GetListAsync(x =>
                x.DueDate.HasValue &&
                x.DueDate.Value <= now &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Canceled &&
                x.Status != TaskItemStatus.Overdue);

            if (overdueTasks.Count > 0)
            {
                foreach (var task in overdueTasks)
                {
                    task.Status = TaskItemStatus.Overdue;
                    await taskRepository.UpdateAsync(task);

                    // 1. Gửi thông báo cho NGƯỜI NHẬN VIỆC (Assignee)
                    if (task.AssigneeId.HasValue)
                    {
                        var assigneeMessage = $"Công việc '{task.Title}' của bạn đã bị quá hạn!";

                        // Lưu vào Database
                        await notificationAppService.CreateNotificationAsync(
                            targetReviewerId: task.AssigneeId.Value,
                            taskId: task.Id,
                            message: assigneeMessage
                        );

                        // Gửi Realtime dạng Object chứa TaskId để Angular map và cho phép click
                        await hubContext.Clients
                            .User(task.AssigneeId.Value.ToString())
                            .SendAsync("ReceiveNotification", new
                            {
                                Id = Guid.NewGuid().ToString(),
                                Message = assigneeMessage,
                                TaskId = task.Id,
                                CreationTime = DateTime.Now
                            });
                    }

                    // 2. Gửi thông báo cho NGƯỜI PHÂN CÔNG / TRƯỞNG PHÒNG (CreatorId)
                    if (task.CreatorId.HasValue)
                    {
                        // Tránh gửi trùng lặp nếu người đó tự giao việc cho chính mình
                        if (task.CreatorId != task.AssigneeId)
                        {
                            var assigneeDisplayName = string.IsNullOrEmpty(task.AssigneeName) ? "Nhân viên" : task.AssigneeName;
                            var creatorMessage = $"Công việc '{task.Title}' (Giao cho: {assigneeDisplayName}) đã bị quá hạn!";

                            // Lưu vào Database
                            await notificationAppService.CreateNotificationAsync(
                                targetReviewerId: task.CreatorId.Value,
                                taskId: task.Id,
                                message: creatorMessage
                            );

                            // Gửi Realtime dạng Object chứa TaskId để Angular map và cho phép click
                            await hubContext.Clients
                                .User(task.CreatorId.Value.ToString())
                                .SendAsync("ReceiveNotification", new
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Message = creatorMessage,
                                    TaskId = task.Id,
                                    CreationTime = DateTime.Now
                                });
                        }
                    }

                    Logger.LogInformation($"--> Đã xử lý và gửi thông báo quá hạn cho Task ID: {task.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "--- [BackgroundWorker] Lỗi xảy ra: {Message}", ex.Message);
        }
    }
}