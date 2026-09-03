using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Notifications;

public class NotificationAppService : ApplicationService, INotificationAppService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;

    public NotificationAppService(IRepository<Notification, Guid> notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync()
    {
        if (!CurrentUser.Id.HasValue) return [];

        var userId = CurrentUser.Id.Value;
        var notifications = await _notificationRepository.GetListAsync(x => x.UserId == userId);

        return [.. notifications
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Message = x.Message,
                IsRead = x.IsRead,
                CreationTime = x.CreationTime
            })];
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var notification = await _notificationRepository.FindAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification, autoSave: true);
        }
    }
}