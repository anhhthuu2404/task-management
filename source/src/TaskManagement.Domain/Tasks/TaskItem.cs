using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using TaskManagement.TaskHistories;

namespace TaskManagement.Tasks;

[Table("Tasks")]
public class TaskItem : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public int ProgressPercent { get; set; }
    public int Position { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public string? AssigneeName { get; set; }

    // === BỔ SUNG 3 TRƯỜNG CHO TÍNH NĂNG LẶP LẠI VÀ BACKGROUND WORKER ===
    public bool IsRecurring { get; set; } = false;
    public RecurrenceFrequency? Frequency { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    // ==================================================================

    public virtual ICollection<TaskHistory> Histories { get; set; } = new List<TaskHistory>();
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

    public TaskItem()
    {
        Title = string.Empty;
    }

    
    public TaskItem(
        Guid id,
        string title,
        Guid categoryId,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        DateTime? dueDate = null) : base(id)
    {
        Title = title;
        CategoryId = categoryId;
        Priority = priority;
        Status = TaskItemStatus.New;
        AssigneeId = assigneeId;
        DueDate = dueDate;
    }

    // Giữ nguyên hoàn toàn logic UpdateAssignee cũ
    public void UpdateAssignee(Guid? newAssigneeId, string? newAssigneeName)
    {
        if (AssigneeId != newAssigneeId)
        {
            var oldName = string.IsNullOrEmpty(AssigneeName) ? "Chưa phân công" : AssigneeName;
            var currentName = string.IsNullOrEmpty(newAssigneeName) ? "Chưa phân công" : newAssigneeName;

            AssigneeId = newAssigneeId;
            AssigneeName = newAssigneeName;
            var history = new TaskHistory(
                Guid.NewGuid(),
                Id,
                $"Đã thay đổi người thực hiện từ [{oldName}] thành [{currentName}]",
                "AssigneeId",
                oldName,
                currentName
            );

            Histories.Add(history);
        }
    }
}