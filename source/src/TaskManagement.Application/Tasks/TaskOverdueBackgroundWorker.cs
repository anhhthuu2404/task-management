using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading; 

namespace TaskManagement.Tasks;

public class TaskOverdueBackgroundWorker : AsyncPeriodicBackgroundWorkerBase, ITransientDependency
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // SỬA Ở ĐÂY: Dùng AbpAsyncTimer thay vì AbpTimer
    public TaskOverdueBackgroundWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;

        // Thiết lập chu kỳ chạy: Cứ mỗi 1 phút quét một lần (60,000 ms)
        Timer.Period = 60000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("--- Bắt đầu quét các công việc quá hạn tự động ---");

        using var scope = _serviceScopeFactory.CreateScope();
        var taskRepository = scope.ServiceProvider.GetRequiredService<IRepository<TaskItem, Guid>>();

        var today = DateTime.Now.Date;

        var overdueTasks = await taskRepository.GetListAsync(x =>
            x.DueDate.HasValue &&
            x.DueDate.Value.Date < today &&
            x.Status != TaskItemStatus.Completed &&
            x.Status != TaskItemStatus.Canceled &&
            x.Status != TaskItemStatus.Overdue);

        if (overdueTasks.Count > 0)
        {
            foreach (var task in overdueTasks)
            {
                task.Status = TaskItemStatus.Overdue;
                await taskRepository.UpdateAsync(task, autoSave: true);

                Logger.LogInformation($"Đã chuyển công việc '{task.Title}' (ID: {task.Id}) sang trạng thái Quá hạn.");
            }
        }

        Logger.LogInformation($"--- Hoàn thành quét công việc quá hạn. Đã cập nhật {overdueTasks.Count} task. ---");
    }
}