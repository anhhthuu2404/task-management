using System;
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
        private readonly IDistributedEventBus _distributedEventBus; // Thêm biến Distributed Event Bus

        // Constructor duy nhất, nhận đầy đủ các dependency
        public TaskManager(
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Notification, Guid> notificationRepository,
            ILocalEventBus localEventBus,
            IClock clock,
            IAsyncQueryableExecuter asyncExecuter,
            IDistributedEventBus distributedEventBus) // Inject IDistributedEventBus vào đây
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

            foreach (var task in overdueTasks)
            {
                // Cập nhật trạng thái sang Quá hạn và ép lưu ngay lập tức (autoSave: true)
                task.Status = TaskItemStatus.Overdue;
                await _taskRepository.UpdateAsync(task, autoSave: true);

                // Nếu task không có người thực hiện thì bỏ qua việc gửi thông báo
                if (!task.AssigneeId.HasValue) continue;

                // Tránh tạo trùng thông báo nếu đã tồn tại thông báo quá hạn cho Task này
                var existingNotification = await _notificationRepository.FirstOrDefaultAsync(n =>
                   n.TaskId == task.Id && n.Message.Contains("quá hạn"));

                if (existingNotification != null) continue;

                var message = $"Công việc \"{task.Title}\" của bạn đã bị quá hạn (Hạn chót: {task.DueDate.Value:dd/MM/yyyy})!";

                // Tạo bản ghi thông báo lưu vào Database
                var notification = new Notification(Guid.NewGuid(), task.AssigneeId.Value, message)
                {
                    TaskId = task.Id,
                    IsRead = false
                };
                await _notificationRepository.InsertAsync(notification, autoSave: true);

                // Bắn Distributed Event đúng loại TaskNotificationEto để Handler bắt và đẩy qua SignalR
                await _distributedEventBus.PublishAsync(new TaskNotificationEto
                {
                    UserId = task.AssigneeId.Value,
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

                    await _taskRepository.InsertAsync(newTask, autoSave: true);

                    parentTask.LastGeneratedDate = now;
                    await _taskRepository.UpdateAsync(parentTask, autoSave: true);
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