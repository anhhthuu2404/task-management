using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Users;

namespace TaskManagement.Hubs
{
    [Authorize]
    public class NotificationHub : AbpHub
    {
        private readonly ICurrentUser _currentUser;

        public NotificationHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            // ĐÃ SỬA: Dùng _currentUser thay vì CurrentUser để đúng với khai báo biến phía trên
            var userId = _currentUser.Id?.ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                // Tự động add connection này vào group mang tên chính UserId đó
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_currentUser.Id.HasValue)
            {
                var userId = _currentUser.Id.Value.ToString();
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string message, string? taskId = null)
        {
            if (_currentUser.Id.HasValue)
            {
                var userId = _currentUser.Id.Value.ToString();

                var notificationData = new
                {
                    id = Guid.NewGuid().ToString(),
                    message = message,
                    taskId = taskId,
                    creationTime = DateTime.UtcNow
                };

                await Clients.Group(userId).SendAsync("ReceiveNotification", notificationData);
            }
        }
    }
}