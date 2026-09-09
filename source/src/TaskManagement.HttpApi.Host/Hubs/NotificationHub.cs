using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;

namespace TaskManagement.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public NotificationHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            if (_currentUser.Id.HasValue)
            {
                var userId = _currentUser.Id.Value.ToString();
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

                // Đóng gói đầy đủ thông tin bao gồm cả thời gian hiện tại
                var notificationData = new
                {
                    message = message,
                    taskId = taskId,
                    creationTime = DateTime.UtcNow // Hoặc DateTime.Now tùy theo cấu hình múi giờ của bạn
                };

                await Clients.Group(userId).SendAsync("ReceiveNotification", notificationData);
            }
        }
    }
}