using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks;

public class TaskDetailDto : TaskDto
{
    public List<SubTaskDto> SubTasks { get; set; } = [];
    public List<ChecklistItemDto> ChecklistItems { get; set; } = [];
    public List<TaskActivityLogDto> ActivityLogs { get; set; } = [];
    public DateTime? SubmittedAt { get; set; }

    public List<TaskCommentDto> Comments { get; set; } = new List<TaskCommentDto>();
}

public class RejectTaskInputDto
{
    [Required(ErrorMessage = "Lý do từ chối không được để trống")]
    public string Reason { get; set; } = string.Empty;
}