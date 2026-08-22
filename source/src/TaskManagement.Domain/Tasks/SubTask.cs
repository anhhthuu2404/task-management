using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class SubTask : FullAuditedEntity<Guid>
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public Guid? AssigneeId { get; set; }

    public virtual TaskItem Task { get; set; } = null!;

  
    public SubTask() { }

  
    public SubTask(Guid id) : base(id) { }
}