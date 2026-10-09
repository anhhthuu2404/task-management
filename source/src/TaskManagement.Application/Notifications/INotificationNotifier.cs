using System;
using System.Threading.Tasks;

namespace TaskManagement.Notifications
{
    public interface INotificationNotifier
    {
        Task SendClientNotificationAsync(Guid userId, object notificationData);
    }
}