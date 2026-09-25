using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.TaskHistories;
using TaskManagement.Tasks.Dtos;
using TaskManagement.Tasks.Request;
using TaskManagement.Tasks.Response;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.ObjectMapping;

namespace TaskManagement.Tasks
{
    public class TaskProvider : ITaskProvider, ITransientDependency
    {
        private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;
        private readonly IObjectMapper _objectMapper;

        public TaskProvider(
            IDbContextProvider<TaskManagementDbContext> dbContextProvider,
            IObjectMapper objectMapper)
        {
            _dbContextProvider = dbContextProvider;
            _objectMapper = objectMapper;
        }

        private async Task<DbConnection> GetDbConnectionAsync()
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            return dbContext.Database.GetDbConnection();
        }

        public async Task<(List<TaskQueryResponse> Items, int TotalCount)> GetListAsync(TaskGetListRequest input, Guid? currentUserId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // Chuyển List<Guid> thành chuỗi phân tách bằng dấu phẩy (nếu có) để truyền vào SQL STRING_SPLIT
            string? departmentIdsString = (input.DepartmentIds != null && input.DepartmentIds.Count > 0)
                ? string.Join(",", input.DepartmentIds)
                : null;

            var parameters = new DynamicParameters();
            parameters.Add("@Keyword", input.Keyword);
            parameters.Add("@CategoryId", input.CategoryId);
            parameters.Add("@ProjectId", input.ProjectId);
            parameters.Add("@AssigneeId", input.AssigneeId);
            parameters.Add("@DepartmentId", input.DepartmentId);
            parameters.Add("@DepartmentIds", departmentIdsString);
            parameters.Add("@Priority", input.Priority);
            parameters.Add("@Status", input.Status);
            parameters.Add("@OnlyMyTasks", input.OnlyMyTasks);
            parameters.Add("@CurrentUserId", currentUserId);
            parameters.Add("@SkipCount", input.SkipCount);
            parameters.Add("@MaxResultCount", input.MaxResultCount);
            parameters.Add("@Sorting", input.Sorting ?? "CreationTime DESC");

            var result = await connection.QueryAsync<TaskQueryResponse, int, TaskQueryResponse>(
                "sp_Task_GetList",
                (task, totalCount) =>
                {
                    task.TotalCount = totalCount;
                    return task;
                },
                parameters,
                commandType: CommandType.StoredProcedure,
                splitOn: "TotalCount"
            );

            var list = result.ToList();
            var totalCount = list.FirstOrDefault()?.TotalCount ?? 0;

            return (list, totalCount);
        }

        public async Task<TaskQueryResponse> CreateAsync(CreateTaskInputDto input, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", Guid.NewGuid());
            parameters.Add("@Title", input.Title);
            parameters.Add("@Description", input.Description);
            parameters.Add("@Priority", input.Priority);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DueDate", input.DueDate);
            parameters.Add("@CategoryId", input.CategoryId);
            parameters.Add("@AssigneeId", input.AssigneeId);
            parameters.Add("@AssigneeName", input.AssigneeName);
            parameters.Add("@ProjectId", input.ProjectId);
            parameters.Add("@DepartmentId", input.DepartmentId);
            parameters.Add("@MilestoneId", input.MilestoneId);
            parameters.Add("@FileName", input.FileName);
            parameters.Add("@FileUrl", input.FileUrl);
            parameters.Add("@ProgressPercent", input.ProgressPercent);
            parameters.Add("@IsRecurring", input.IsRecurring);
            parameters.Add("@Frequency", input.Frequency);
            parameters.Add("@CreatorId", creatorId);

            var task = await connection.QueryFirstOrDefaultAsync<TaskQueryResponse>(
                "sp_Task_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return task ?? throw new InvalidOperationException("Không thể tạo mới công việc.");
        }

        public async Task<TaskQueryResponse> UpdateAsync(Guid id, UpdateTaskInputDto input, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Title", input.Title);
            parameters.Add("@Description", input.Description);
            parameters.Add("@Priority", input.Priority);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DueDate", input.DueDate);
            parameters.Add("@CategoryId", input.CategoryId);
            parameters.Add("@AssigneeId", input.AssigneeId);
            parameters.Add("@AssigneeName", input.AssigneeName);
            parameters.Add("@ProjectId", input.ProjectId);
            parameters.Add("@DepartmentId", input.DepartmentId);
            parameters.Add("@MilestoneId", input.MilestoneId);
            parameters.Add("@FileName", input.FileName);
            parameters.Add("@FileUrl", input.FileUrl);
            parameters.Add("@ProgressPercent", input.ProgressPercent);
            parameters.Add("@IsRecurring", input.IsRecurring);
            parameters.Add("@Frequency", input.Frequency);
            parameters.Add("@ModifierId", modifierId);

            var task = await connection.QueryFirstOrDefaultAsync<TaskQueryResponse>(
                "sp_Task_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc để cập nhật.");
        }

        public async Task<TaskQueryResponse> UpdateStatusAsync(Guid id, int status, int progressPercent, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Status", status);
            parameters.Add("@ProgressPercent", progressPercent);
            parameters.Add("@ModifierId", modifierId);

            var task = await connection.QueryFirstOrDefaultAsync<TaskQueryResponse>(
                "sp_Task_UpdateStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc.");
        }

        public async Task<TaskQueryResponse> UpdateAssigneeAsync(Guid id, Guid? assigneeId, string? assigneeName, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@AssigneeId", assigneeId);
            parameters.Add("@AssigneeName", assigneeName);
            parameters.Add("@ModifierId", modifierId);

            var task = await connection.QueryFirstOrDefaultAsync<TaskQueryResponse>(
                "sp_Task_UpdateAssignee",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc.");
        }

        public async Task DeleteAsync(Guid id, Guid? deleterId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@DeleterId", deleterId);

            await connection.ExecuteAsync(
                "sp_Task_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<TaskDto> GetByIdAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var taskResponse = await connection.QueryFirstOrDefaultAsync<TaskQueryResponse>(
                "sp_Task_GetById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (taskResponse == null)
            {
                throw new InvalidOperationException("Không tìm thấy công việc.");
            }

            return _objectMapper.Map<TaskQueryResponse, TaskDto>(taskResponse);
        }

        public async Task<List<ChecklistItemDto>> GetChecklistsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<ChecklistItemDto>(
                "sp_TaskChecklist_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<ChecklistItemDto> CreateChecklistAsync(Guid id, Guid taskId, CreateUpdateChecklistItemDto input, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Title", input.Title);
            parameters.Add("@IsDone", input.IsDone);
            parameters.Add("@CreatorId", creatorId);

            var checklist = await connection.QueryFirstOrDefaultAsync<ChecklistItemDto>(
                "sp_TaskChecklist_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return checklist ?? throw new InvalidOperationException("Không thể tạo checklist.");
        }

        public async Task<List<TaskAttachmentDto>> GetAttachmentsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskAttachmentDto>(
                "sp_TaskAttachment_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<List<TaskActivityLogDto>> GetActivityLogsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskActivityLogDto>(
                "sp_TaskActivityLog_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        // --- Task Histories (Dòng thời gian / Lịch sử) ---
        public async Task<List<TaskHistoryDto>> GetHistoriesByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskHistoryDto>(
                "sp_TaskHistory_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<TaskHistoryDto> CreateHistoryAsync(Guid id, Guid taskId, string action, string fieldName, string oldVal, string newVal, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Action", action);
            parameters.Add("@FieldName", fieldName);
            parameters.Add("@OldValue", oldVal);
            parameters.Add("@NewValue", newVal);
            parameters.Add("@CreatorId", creatorId);

            await connection.ExecuteAsync(
                "sp_TaskHistory_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return new TaskHistoryDto
            {
                Id = id,
                TaskId = taskId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldVal,
                NewValue = newVal,
                CreationTime = DateTime.Now,
                CreatorId = creatorId
            };
        }

        // --- Task Comments (Bình luận công việc) ---
        public async Task<List<TaskCommentDto>> GetCommentsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskCommentDto>(
                "sp_TaskComment_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<TaskCommentDto> GetCommentByIdAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var comment = await connection.QueryFirstOrDefaultAsync<TaskCommentDto>(
                "sp_TaskComment_GetById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return comment ?? throw new InvalidOperationException("Không tìm thấy bình luận.");
        }

        public async Task<TaskCommentDto> CreateCommentAsync(Guid id, Guid taskId, string text, string? fileName, string? fileUrl, Guid? userId, Guid? creatorId, Guid? tenantId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Text", text);
            parameters.Add("@FileName", fileName);
            parameters.Add("@FileUrl", fileUrl);
            parameters.Add("@UserId", userId);
            parameters.Add("@CreatorId", creatorId);
            parameters.Add("@TenantId", tenantId);

            var comment = await connection.QueryFirstOrDefaultAsync<TaskCommentDto>(
                "sp_TaskComment_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return comment ?? throw new InvalidOperationException("Không thể tạo bình luận mới.");
        }

        public async Task DeleteCommentAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_TaskComment_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        // --- Task Comment Attachments (File đính kèm bình luận) ---
        public async Task<List<CommentAttachmentDto>> GetCommentAttachmentsByCommentIdAsync(Guid taskCommentId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskCommentId", taskCommentId);

            var result = await connection.QueryAsync<CommentAttachmentDto>(
                "sp_TaskCommentAttachment_GetByCommentId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<CommentAttachmentDto> CreateCommentAttachmentAsync(Guid id, string fileName, string fileUrl, Guid taskCommentId)
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@FileName", fileName);
            parameters.Add("@FileUrl", fileUrl);
            parameters.Add("@TaskCommentId", taskCommentId);

            await connection.ExecuteAsync(
                "sp_TaskCommentAttachment_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return new CommentAttachmentDto
            {
                FileName = fileName,
                FileUrl = fileUrl
            };
        }
    }
}