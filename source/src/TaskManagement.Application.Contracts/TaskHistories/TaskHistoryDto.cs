using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.TaskHistories
{
    public class TaskHistoryDto : CreationAuditedEntityDto<Guid>
    {
        public Guid TaskId { get; set; }
        public string Action { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}