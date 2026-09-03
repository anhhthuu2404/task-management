using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Notifications;

public class Notification : CreationAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    protected Notification() { }

    public Notification(Guid id, Guid userId, string message) : base(id)
    {
        UserId = userId;
        Message = message;
        IsRead = false;
    }
}