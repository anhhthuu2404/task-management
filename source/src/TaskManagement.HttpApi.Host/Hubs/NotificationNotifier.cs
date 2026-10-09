using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Hubs;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Notifications
{
    public class NotificationNotifier : INotificationNotifier, ITransientDependency
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationNotifier(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendClientNotificationAsync(Guid userId, object notificationData)
        {
            // Bắn SignalR thời gian thực tới đúng Group của User đang online
            await _hubContext.Clients.Group(userId.ToString())
                .SendAsync("ReceiveNotification", notificationData);
        }
    }
}