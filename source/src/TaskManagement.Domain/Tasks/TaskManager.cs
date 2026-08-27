using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Volo.Abp.Linq;

namespace TaskManagement.Tasks
{
    public class TaskManager : DomainService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IClock _clock;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public TaskManager(
            IRepository<TaskItem, Guid> taskRepository,
            IClock clock,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _taskRepository = taskRepository;
            _clock = clock;
            _asyncExecuter = asyncExecuter;
        }

        // 1. Quét và cập nhật Task quá hạn tự động
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
                task.Status = TaskItemStatus.Overdue;
                await _taskRepository.UpdateAsync(task);
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

                    // Khởi tạo task mới bằng constructor gọn, gán trực tiếp Priority qua thuộc tính để tránh lỗi biên dịch namespace
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