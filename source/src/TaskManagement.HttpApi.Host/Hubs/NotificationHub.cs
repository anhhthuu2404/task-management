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

            // Nếu user đã đăng nhập, tự động add connection này vào Group mang tên chính UserId của họ
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
    }
}