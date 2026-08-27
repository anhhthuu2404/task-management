using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.TaskHistories;

public class TaskHistoryDto : CreationAuditedEntityDto<Guid>
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public Guid? OldAssigneeId { get; set; }
    public string? OldAssigneeName { get; set; }
    public Guid? NewAssigneeId { get; set; }
    public string? NewAssigneeName { get; set; }

    public TaskHistoryDto() { }

    public TaskHistoryDto(Guid id, Guid taskId, string action, string fieldName, string oldValue, string newValue)
    {
        Id = id;
        TaskId = taskId;
        Action = action;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}