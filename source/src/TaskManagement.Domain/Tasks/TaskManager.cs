using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Volo.Abp.Linq;
using Volo.Abp.EventBus.Local;
using Volo.Abp.EventBus.Distributed;
using TaskManagement.Notifications;

namespace TaskManagement.Tasks
{
    public class TaskManager : DomainService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IRepository<Notification, Guid> _notificationRepository;
        private readonly ILocalEventBus _localEventBus;
        private readonly IClock _clock;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IDistributedEventBus _distributedEventBus;

        public TaskManager(
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Notification, Guid> notificationRepository,
            ILocalEventBus localEventBus,
            IClock clock,
            IAsyncQueryableExecuter asyncExecuter,
            IDistributedEventBus distributedEventBus)
        {
            _taskRepository = taskRepository;
            _notificationRepository = notificationRepository;
            _localEventBus = localEventBus;
            _clock = clock;
            _asyncExecuter = asyncExecuter;
            _distributedEventBus = distributedEventBus;
        }

        // 1. Quét và cập nhật Task quá hạn tự động kèm sinh thông báo & bắn Event
        [UnitOfWork]
        public virtual async Task ProcessOverdueTasksAsync()
        {
            var now = _clock.Now;
            var query = await _taskRepository.GetQueryableAsync();

            var overdueTasks = await _asyncExecuter.ToListAsync(
                query.Where(t => t.DueDate.HasValue
                            && t.DueDate.Value < now
                            && t.Status != TaskItemStatus.Completed
                            && t.Status != TaskItemStatus.Canceled
                            && t.Status != TaskItemStatus.Overdue)
            );

            if (!overdueTasks.Any()) return;

            // Ép kiểu taskIds sang List<Guid?> để khớp với kiểu dữ liệu của n.TaskId trong Notification
            var taskIds = overdueTasks.Select(t => (Guid?)t.Id).ToList();
            var notificationQuery = await _notificationRepository.GetQueryableAsync();
            var existingNotifications = await _asyncExecuter.ToListAsync(
                notificationQuery.Where(n => n.TaskId.HasValue && taskIds.Contains(n.TaskId) && n.Message.Contains("quá hạn"))
            );
            var notifiedTaskIds = new HashSet<Guid>(existingNotifications.Where(n => n.TaskId.HasValue).Select(n => n.TaskId!.Value));

            foreach (var task in overdueTasks)
            {
                task.Status = TaskItemStatus.Overdue;
                await _taskRepository.UpdateAsync(task);

                // Kiểm tra null an toàn cho AssigneeId (hoặc AssigneeUserId tùy entity của bạn)
                if (!task.AssigneeId.HasValue) continue;

                // Lấy giá trị Guid chuẩn bằng .Value để truyền vào Notification
                Guid assigneeId = task.AssigneeId.Value;

                if (notifiedTaskIds.Contains(task.Id)) continue;

                var message = $"Công việc \"{task.Title}\" của bạn đã bị quá hạn (Hạn chót: {task.DueDate.Value:dd/MM/yyyy})!";

                // Sử dụng biến assigneeId (kiểu Guid) đã ép kiểu an toàn
                var notification = new Notification(Guid.NewGuid(), assigneeId, message)
                {
                    TaskId = task.Id,
                    IsRead = false
                };
                await _notificationRepository.InsertAsync(notification);

                notifiedTaskIds.Add(task.Id);

                await _distributedEventBus.PublishAsync(new TaskNotificationEto
                {
                    UserId = assigneeId,
                    TaskId = task.Id,
                    Message = message,
                    CreationTime = DateTime.UtcNow
                });
            }
        }

        // 2. Tự động sinh Task lặp lại theo cấu hình
        [UnitOfWork]
        public virtual async Task ProcessRecurringTasksAsync()
        {
            var now = _clock.Now;
            var query = await _taskRepository.GetQueryableAsync();

            var recurringTasks = await _asyncExecuter.ToListAsync(
                query.Where(t => t.IsRecurring
                            && t.Frequency.HasValue
                            && t.Status != TaskItemStatus.Completed)
            );

            if (!recurringTasks.Any()) return;

            foreach (var parentTask in recurringTasks)
            {
                var lastCheck = parentTask.LastGeneratedDate ?? parentTask.CreationTime;
                bool shouldGenerate = false;

                switch (parentTask.Frequency)
                {
                    case RecurrenceFrequency.Daily:
                        shouldGenerate = lastCheck.AddDays(1) <= now;
                        break;
                    case RecurrenceFrequency.Weekly:
                        shouldGenerate = lastCheck.AddDays(7) <= now;
                        break;
                    case RecurrenceFrequency.Monthly:
                        shouldGenerate = lastCheck.AddMonths(1) <= now;
                        break;
                }

                if (shouldGenerate)
                {
                    DateTime? newDueDate = parentTask.DueDate.HasValue
                        ? parentTask.DueDate.Value.AddDays(GetDaysOffset(parentTask.Frequency.Value))
                        : null;

                    var newTask = new TaskItem(
                        Guid.NewGuid(),
                        parentTask.Title + " (Lặp lại)",
                        parentTask.CategoryId
                    )
                    {
                        Priority = parentTask.Priority,
                        Status = TaskItemStatus.New,
                        AssigneeId = parentTask.AssigneeId,
                        AssigneeName = parentTask.AssigneeName,
                        DueDate = newDueDate,
                        Description = parentTask.Description,
                        IsRecurring = false
                    };

                    await _taskRepository.InsertAsync(newTask);

                    parentTask.LastGeneratedDate = now;
                    await _taskRepository.UpdateAsync(parentTask);
                }
            }
        }

        private static int GetDaysOffset(RecurrenceFrequency frequency) => frequency switch
        {
            RecurrenceFrequency.Daily => 1,
            RecurrenceFrequency.Weekly => 7,
            RecurrenceFrequency.Monthly => 30,
            _ => 1
        };
    }
}