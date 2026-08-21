using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

[Table("Tasks")]
public class TaskItem : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public int ProgressPercent { get; set; }

    public virtual ICollection<TaskAttachment> Attachments { get; set; } = [];

    public TaskItem()
    {
        Title = string.Empty;
        Description = string.Empty;
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
        Description = string.Empty;
        CategoryId = categoryId;
        Priority = priority;
        Status = TaskItemStatus.New;
        AssigneeId = assigneeId;
        DueDate = dueDate;
    }
}