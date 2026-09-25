using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.TaskHistories;
using TaskManagement.Tasks.Dtos;
using TaskManagement.Tasks.Request;
using TaskManagement.Tasks.Response;

namespace TaskManagement.Tasks
{
    public interface ITaskProvider
    {
        Task<(List<TaskQueryResponse> Items, int TotalCount)> GetListAsync(TaskGetListRequest input, Guid? currentUserId);
        Task<TaskQueryResponse> CreateAsync(CreateTaskInputDto input, Guid? creatorId);
        Task<TaskQueryResponse> UpdateAsync(Guid id, UpdateTaskInputDto input, Guid? modifierId);
        Task<TaskQueryResponse> UpdateStatusAsync(Guid id, int status, int progressPercent, Guid? modifierId);
        Task<TaskQueryResponse> UpdateAssigneeAsync(Guid id, Guid? assigneeId, string? assigneeName, Guid? modifierId);
        Task DeleteAsync(Guid id, Guid? deleterId);
        Task<TaskDto> GetByIdAsync(Guid id);

        // Checklist methods
        Task<List<ChecklistItemDto>> GetChecklistsByTaskIdAsync(Guid taskId);
        Task<ChecklistItemDto> CreateChecklistAsync(Guid id, Guid taskId, CreateUpdateChecklistItemDto input, Guid? creatorId);

        // Attachment methods
        Task<List<TaskAttachmentDto>> GetAttachmentsByTaskIdAsync(Guid taskId);

        // Activity Log methods
        Task<List<TaskActivityLogDto>> GetActivityLogsByTaskIdAsync(Guid taskId);

        // Task History methods (Dòng thời gian / Lịch sử thay đổi)
        Task<List<TaskHistoryDto>> GetHistoriesByTaskIdAsync(Guid taskId);
        Task<TaskHistoryDto> CreateHistoryAsync(Guid id, Guid taskId, string action, string fieldName, string oldVal, string newVal, Guid? creatorId);

        // --- Task Comment methods (Bình luận công việc) ---
        Task<List<TaskCommentDto>> GetCommentsByTaskIdAsync(Guid taskId);
        Task<TaskCommentDto> GetCommentByIdAsync(Guid id);
        Task<TaskCommentDto> CreateCommentAsync(Guid id, Guid taskId, string text, string? fileName, string? fileUrl, Guid? userId, Guid? creatorId, Guid? tenantId);
        Task DeleteCommentAsync(Guid id);

        // Task Comment Attachment methods (File đính kèm bình luận)
        Task<List<CommentAttachmentDto>> GetCommentAttachmentsByCommentIdAsync(Guid taskCommentId);
        Task<CommentAttachmentDto> CreateCommentAttachmentAsync(Guid id, string fileName, string fileUrl, Guid taskCommentId);
    }
}