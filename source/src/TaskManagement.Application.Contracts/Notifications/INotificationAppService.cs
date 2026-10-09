using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManagement.Notifications
{
    public interface INotificationAppService
    {
        Task<List<NotificationDto>> GetUserNotificationsAsync();

        // Bổ sung phương thức này để đồng bộ với NotificationAppService và ProjectAppService
        Task CreateTaskNotificationAsync(Guid? assignedUserId, Guid taskId, string message);

        Task CreateNotificationAsync(Guid targetReviewerId, Guid taskId, string message);
        Task MarkAsReadAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}