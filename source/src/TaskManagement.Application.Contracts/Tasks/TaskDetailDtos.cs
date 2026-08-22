using System;
using System.Collections.Generic;

namespace TaskManagement.Tasks;

public class TaskDetailDto : TaskDto
{
    public new string? Description { get; set; } 
    public List<SubTaskDto> SubTasks { get; set; } = [];
    public List<ChecklistItemDto> ChecklistItems { get; set; } = [];
    public List<TaskActivityLogDto> ActivityLogs { get; set; } = [];
}