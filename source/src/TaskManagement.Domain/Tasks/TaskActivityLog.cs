using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class TaskActivityLog : CreationAuditedEntity<Guid>
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Details { get; set; }

    public virtual TaskItem Task { get; set; } = null!;

    public TaskActivityLog() { }
    public TaskActivityLog(Guid id) : base(id) { }
}