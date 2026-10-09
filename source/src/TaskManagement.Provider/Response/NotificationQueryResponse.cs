using System;

namespace TaskManagement.Provider.Response
{
    public class NotificationQueryResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageEn { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
        public Guid? TaskId { get; set; }
        public bool IsDeleted { get; set; }
    }
}