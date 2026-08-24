using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeUserName { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public int ProgressPercent { get; set; }

    public string? SubmissionNote { get; set; }
    public List<TaskFileDto> SubmissionFiles { get; set; } = new();
  
}

public class TaskFileDto
{
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? FileContent { get; set; }
}