using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks
{
    public class TaskHistory : CreationAuditedEntity<Guid>
    {
        public Guid TaskId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;

        public TaskHistory() { }

        public TaskHistory(Guid id, Guid taskId, string action, string fieldName, string oldValue, string newValue) : base(id)
        {
            TaskId = taskId;
            Action = action;
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}