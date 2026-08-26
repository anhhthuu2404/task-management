using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.TaskHistories;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks;

public interface ITaskAppService : ICrudAppService<
    TaskDto,
    Guid,
    GetTaskListInputDto,
    CreateTaskInputDto,
    UpdateTaskInputDto>
{
    Task<TaskDetailDto> GetTaskDetailAsync(Guid id);
    Task<TaskDto> UpdateStatusAsync(Guid id, TaskItemStatus status);
    Task<TaskDto> UpdateAssigneeAsync(Guid id, Guid? assigneeId);

    // Workflow Review
    Task<TaskDetailDto> SubmitForReviewAsync(Guid id, SubmitReviewInputDto? input = null);
    Task<TaskDetailDto> ApproveAsync(Guid id);
    Task<TaskDetailDto> RejectAsync(Guid id, RejectTaskInputDto input);

    // SubTask
    Task<SubTaskDto> CreateSubTaskAsync(Guid taskId, CreateUpdateSubTaskDto input);
    Task<SubTaskDto> UpdateSubTaskAsync(Guid subTaskId, CreateUpdateSubTaskDto input);
    Task ToggleSubTaskStatusAsync(Guid subTaskId);
    Task DeleteSubTaskAsync(Guid subTaskId);

    // Checklist
    Task<ChecklistItemDto> CreateChecklistItemAsync(Guid taskId, CreateUpdateChecklistItemDto input);
    Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, CreateUpdateChecklistItemDto input);
    Task ToggleChecklistItemStatusAsync(Guid itemId);
    Task DeleteChecklistItemAsync(Guid itemId);
    Task<List<TaskActivityLogDto>> GetTaskTimelineAsync(Guid taskId);

    // Comments
    Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId);
    Task<TaskCommentDto> CreateCommentAsync(Guid taskId, CreateTaskCommentDto input);
    Task<TaskCommentDto> UpdateCommentAsync(Guid commentId, UpdateTaskCommentDto input);
    Task DeleteCommentAsync(Guid commentId);
    
}