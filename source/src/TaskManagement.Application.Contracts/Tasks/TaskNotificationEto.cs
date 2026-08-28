using System;
using Volo.Abp.EventBus;

namespace TaskManagement.Tasks;

[EventName("task.notification")]
public class TaskNotificationEto
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}