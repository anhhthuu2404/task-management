using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Hubs;
using TaskManagement.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace TaskManagement;

public class TaskNotificationEtoHandler :
    IDistributedEventHandler<TaskNotificationEto>,
    ITransientDependency
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public TaskNotificationEtoHandler(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task HandleEventAsync(TaskNotificationEto eventData)
    {
        if (eventData.UserId == Guid.Empty || string.IsNullOrWhiteSpace(eventData.Message))
        {
            return;
        }

      
        await _hubContext.Clients
            .User(eventData.UserId.ToString())
            .SendAsync("ReceiveNotification", eventData.Message, eventData.TaskId);
    }
}