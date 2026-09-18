using System;

namespace TaskManagement.Tasks
{
    public class TaskOverdueEto
    {
        public Guid TaskId { get; set; }
        public Guid AssigneeId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}