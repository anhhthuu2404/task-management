using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class TaskComment : FullAuditedEntity<Guid>
{
    public Guid TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }

    public TaskComment() { }

    public TaskComment(Guid id) : base(id) { }
}