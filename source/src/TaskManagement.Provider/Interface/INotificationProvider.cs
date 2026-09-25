using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Provider.Response;

namespace TaskManagement.Provider.Interface
{
    public interface INotificationProvider
    {
        Task<List<NotificationQueryResponse>> GetByUserIdAsync(Guid userId);
        Task CreateAsync(NotificationQueryResponse input);
        Task MarkAsReadAsync(Guid id);
    }
}