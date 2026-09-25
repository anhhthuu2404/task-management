using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class TaskChecklistItem : FullAuditedEntity<Guid>
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;       // Tiêu đề (Tiếng Việt)
    public string? TitleEn { get; set; }                   // Tiêu đề (Tiếng Anh tự động dịch)
    public bool IsDone { get; set; }

    public virtual TaskItem Task { get; set; } = null!;

    public TaskChecklistItem() { }
    public TaskChecklistItem(Guid id) : base(id) { }
}