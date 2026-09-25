using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Response;
using Volo.Abp.Application.Services;

namespace TaskManagement.Notifications
{
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly INotificationProvider _notificationProvider;

        public NotificationAppService(INotificationProvider notificationProvider)
        {
            _notificationProvider = notificationProvider;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync()
        {
            if (!CurrentUser.Id.HasValue) return [];

            var userId = CurrentUser.Id.Value;
            var notifications = await _notificationProvider.GetByUserIdAsync(userId);

            return notifications.Select(x => new NotificationDto
            {
                Id = x.Id,
                Message = x.Message,
                IsRead = x.IsRead,
                CreationTime = x.CreationTime,
                TaskId = x.TaskId
            }).ToList();
        }

        public async Task CreateNotificationAsync(Guid targetReviewerId, Guid taskId, string message)
        {
            var notificationResponse = new NotificationQueryResponse
            {
                Id = GuidGenerator.Create(),
                UserId = targetReviewerId,
                Message = message,
                IsRead = false,
                CreationTime = Clock.Now,
                CreatorId = CurrentUser.Id,
                TaskId = taskId
            };

            await _notificationProvider.CreateAsync(notificationResponse);
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            await _notificationProvider.MarkAsReadAsync(id);
        }
    }
}