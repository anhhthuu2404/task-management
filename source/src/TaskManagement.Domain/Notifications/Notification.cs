using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Notifications;

public class Notification : CreationAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;     // Nội dung thông báo (Tiếng Việt)
    public string? MessageEn { get; set; }                 // Nội dung thông báo (Tiếng Anh tự động dịch)
    public bool IsRead { get; set; } = false;
    public Guid? TaskId { get; set; }

    protected Notification() { }

    public Notification(Guid id, Guid userId, string message, string? messageEn = null, Guid? taskId = null) : base(id)
    {
        UserId = userId;
        Message = message;
        MessageEn = messageEn;
        IsRead = false;
        TaskId = taskId;
    }
}