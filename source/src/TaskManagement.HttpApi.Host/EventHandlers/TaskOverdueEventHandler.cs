using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using TaskManagement.Hubs;
using TaskManagement.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace TaskManagement.HttpApi.Host.EventHandlers
{
    public class TaskOverdueEventHandler : ILocalEventHandler<TaskOverdueEto>, ITransientDependency
    {
        private readonly IHubContext<TaskNotificationHub> _hubContext;

        public TaskOverdueEventHandler(IHubContext<TaskNotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task HandleEventAsync(TaskOverdueEto eventData)
        {
            // Thêm bảo vệ chống lỗi null hoặc thiếu ID người nhận
            if (eventData == null || eventData.AssigneeId == Guid.Empty || string.IsNullOrWhiteSpace(eventData.Message))
            {
                return;
            }

            await _hubContext.Clients.Group($"User_{eventData.AssigneeId}")
                .SendAsync("ReceiveTaskNotification", new
                {
                    Title = "Công việc đã quá hạn!",
                    Message = eventData.Message,
                    TaskId = eventData.TaskId
                });
        }
    }
}