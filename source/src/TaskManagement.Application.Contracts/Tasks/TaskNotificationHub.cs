using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Hubs;

[Authorize]
public class TaskNotificationHub : Hub, ITransientDependency
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(Volo.Abp.Security.Claims.AbpClaimTypes.UserId)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(Volo.Abp.Security.Claims.AbpClaimTypes.UserId)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Phương thức phía Client có thể gọi lên Server nếu cần
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}