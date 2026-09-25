using System;

namespace TaskManagement.Provider.Request
{
    public class NotificationGetListRequest
    {
        public Guid UserId { get; set; }
        public int SkipCount { get; set; } = 0;
        public int MaxResultCount { get; set; } = 10;
    }
}