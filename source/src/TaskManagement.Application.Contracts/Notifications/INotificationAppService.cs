using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Notifications;

public interface INotificationAppService : IApplicationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync();
    Task CreateNotificationAsync(Guid targetReviewerId, Guid taskId, string message);
    Task MarkAsReadAsync(Guid id);
}