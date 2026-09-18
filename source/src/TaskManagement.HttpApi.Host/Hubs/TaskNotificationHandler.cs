using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace TaskManagement.Hubs;

public class TaskNotificationHandler : IDistributedEventHandler<TaskNotificationEto>, ITransientDependency
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public TaskNotificationHandler(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task HandleEventAsync(TaskNotificationEto eventData)
    {
        if (eventData.UserId == Guid.Empty || string.IsNullOrEmpty(eventData.Message))
            return;

        // Tạo payload đầy đủ chứa cả taskId
        var notificationPayload = new
        {
            id = Guid.NewGuid().ToString(),
            message = eventData.Message,
            taskId = eventData.TaskId, // Đã có taskId từ Eto
            creationTime = eventData.CreationTime != default ? eventData.CreationTime : DateTime.UtcNow
        };

        // Phát tín hiệu Real-time qua SignalR và truyền nguyên đối tượng payload đi
        await _hubContext.Clients
            .Group(eventData.UserId.ToString())
            .SendAsync("ReceiveNotification", notificationPayload);
    }
}