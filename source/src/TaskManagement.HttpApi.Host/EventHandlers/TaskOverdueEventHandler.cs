using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using TaskManagement.Hubs; // Namespace chứa NotificationHub trong HttpApi.Host
using TaskManagement.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace TaskManagement.HttpApi.Host.EventHandlers
{
    public class TaskOverdueEventHandler : ILocalEventHandler<TaskOverdueEto>, ITransientDependency
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public TaskOverdueEventHandler(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task HandleEventAsync(TaskOverdueEto eventData)
        {
            if (eventData == null || eventData.AssigneeId == Guid.Empty || string.IsNullOrWhiteSpace(eventData.Message))
            {
                return;
            }

            // Dùng NotificationHub và khớp tên group theo userId thuần túy, phương thức "ReceiveNotification"
            await _hubContext.Clients.Group(eventData.AssigneeId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Công việc đã quá hạn!",
                    message = eventData.Message,
                    taskId = eventData.TaskId,
                    creationTime = DateTime.Now
                });
        }
    }
}