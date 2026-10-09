using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskProvider(
            IDbContextProvider<TaskManagementDbContext> dbContextProvider,
            IObjectMapper objectMapper,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContextProvider = dbContextProvider;
            _objectMapper = objectMapper;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<DbConnection> GetDbConnectionAsync()
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            return connection;
        }

        #region --- Helper: Auto Translation & Language Adjustment ---

        private async Task<string?> AutoTranslateToEnglishAsync(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=en&dt=t&q={Uri.EscapeDataString(text)}";
                var response = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var translatedText = doc.RootElement[0][0][0].GetString();

                return translatedText ?? text;
            }
            catch
            {
                return text;
            }
        }

        // Cải tiến kiểm tra thông minh: Kết hợp CultureInfo, Query String, HTTP Headers và Cookies của ABP
        private bool IsEnglishRequest()
        {
            var currentLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (currentLang.Equals("en", StringComparison.OrdinalIgnoreCase))
                return true;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false;

            // 1. Kiểm tra Query String (phòng trường hợp URL có chứa ?culture=en hoặc ?lang=en)
            if (httpContext.Request.Query != null)
            {
                if (httpContext.Request.Query.TryGetValue("culture", out var queryCulture) &&
                    !string.IsNullOrEmpty(queryCulture) && queryCulture.ToString().Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (httpContext.Request.Query.TryGetValue("lang", out var queryLang) &&
                    !string.IsNullOrEmpty(queryLang) && queryLang.ToString().Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 2. Kiểm tra HTTP Headers (Bổ sung thêm X-Culture, Accept-Language, culture)
            var headers = httpContext.Request.Headers;
            if (headers != null)
            {
                if (headers.TryGetValue("Accept-Language", out var acceptLang) &&
                    acceptLang.ToString().Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (headers.TryGetValue("culture", out var cultureHeader) &&
                    cultureHeader.ToString().Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (headers.TryGetValue("X-Culture", out var xCultureHeader) &&
                    xCultureHeader.ToString().Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 3. Kiểm tra Cookies của ABP / ASP.NET Core khi F5 trang
            var cookies = httpContext.Request.Cookies;
            if (cookies != null)
            {
                if (cookies.TryGetValue(".AspNetCore.Culture", out var cookieCulture) &&
                    !string.IsNullOrEmpty(cookieCulture) && cookieCulture.Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (cookies.TryGetValue("Abp.Localization.CultureName", out var abpCookie) &&
                    !string.IsNullOrEmpty(abpCookie) && abpCookie.Contains("en", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void AdjustLanguageForTask(TaskQueryResponse task)
        {
            if (task == null) return;

            if (IsEnglishRequest())
            {
                if (!string.IsNullOrEmpty(task.TitleEn))
                {
                    task.Title = task.TitleEn;
                }
                if (!string.IsNullOrEmpty(task.DescriptionEn))
                {
                    task.Description = task.DescriptionEn;
                }
            }
        }

        private void AdjustLanguageForComment(TaskCommentDto comment)
        {
            if (comment == null) return;

            if (IsEnglishRequest())
            {
                if (!string.IsNullOrEmpty(comment.TextEn))
                {
                    comment.Text = comment.TextEn;
                }
            }
        }

        private void AdjustLanguageForChecklist(ChecklistItemDto checklist)
        {
            if (checklist == null) return;

            if (IsEnglishRequest())
            {
                if (!string.IsNullOrEmpty(checklist.TitleEn))
                {
                    checklist.Title = checklist.TitleEn;
                }
            }
        }

        private void AdjustLanguageForSubTask(SubTaskDto subTask)
        {
            if (subTask == null) return;

            if (IsEnglishRequest())
            {
                if (!string.IsNullOrEmpty(subTask.TitleEn))
                {
                    subTask.Title = subTask.TitleEn;
                }
            }
        }

        #endregion

        #region --- Task Core Operations ---

        public async Task<(List<TaskQueryResponse> Items, int TotalCount)> GetListAsync(TaskGetListRequest input, Guid? currentUserId)
        {
            var connection = await GetDbConnectionAsync();

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

            var result = await connection.QueryAsync<TaskQueryResponse>(
                "sp_Task_GetList",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = result.ToList();

            foreach (var item in list)
            {
                AdjustLanguageForTask(item);
            }

            var totalCount = list.FirstOrDefault()?.TotalCount ?? 0;

            return (list, totalCount);
        }

        public async Task<TaskQueryResponse> CreateAsync(CreateTaskInputDto input, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = string.IsNullOrWhiteSpace(input.TitleEn)
                ? await AutoTranslateToEnglishAsync(input.Title)
                : input.TitleEn;

            var descriptionEn = string.IsNullOrWhiteSpace(input.DescriptionEn)
                ? await AutoTranslateToEnglishAsync(input.Description)
                : input.DescriptionEn;

            var parameters = new DynamicParameters();
            parameters.Add("@Id", Guid.NewGuid());
            parameters.Add("@Title", input.Title);
            parameters.Add("@Description", input.Description);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@DescriptionEn", descriptionEn);
            parameters.Add("@Priority", input.Priority);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DueDate", input.DueDate);
            parameters.Add("@CategoryId", input.CategoryId);
            parameters.Add("@AssigneeId", input.AssigneeId);
            parameters.Add("@AssigneeName", input.AssigneeName);
            parameters.Add("@AssigneeUserName", input.AssigneeUserName);
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

            AdjustLanguageForTask(task);

            return task ?? throw new InvalidOperationException("Không thể tạo mới công việc.");
        }

        public async Task<TaskQueryResponse> UpdateAsync(Guid id, UpdateTaskInputDto input, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = string.IsNullOrWhiteSpace(input.TitleEn)
                ? await AutoTranslateToEnglishAsync(input.Title)
                : input.TitleEn;

            var descriptionEn = string.IsNullOrWhiteSpace(input.DescriptionEn)
                ? await AutoTranslateToEnglishAsync(input.Description)
                : input.DescriptionEn;

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Title", input.Title);
            parameters.Add("@Description", input.Description);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@DescriptionEn", descriptionEn);
            parameters.Add("@Priority", input.Priority);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DueDate", input.DueDate);
            parameters.Add("@CategoryId", input.CategoryId);
            parameters.Add("@AssigneeId", input.AssigneeId);
            parameters.Add("@AssigneeName", input.AssigneeName);
            parameters.Add("@AssigneeUserName", input.AssigneeUserName);
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

            AdjustLanguageForTask(task);

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc để cập nhật.");
        }

        public async Task<TaskQueryResponse> UpdateStatusAsync(Guid id, int status, int progressPercent, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

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

            AdjustLanguageForTask(task);

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc.");
        }

        public async Task<TaskQueryResponse> UpdateAssigneeAsync(Guid id, Guid? assigneeId, string? assigneeName, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

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

            AdjustLanguageForTask(task);

            return task ?? throw new InvalidOperationException("Không tìm thấy công việc.");
        }

        public async Task DeleteAsync(Guid id, Guid? deleterId)
        {
            var connection = await GetDbConnectionAsync();

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

            AdjustLanguageForTask(taskResponse);

            return _objectMapper.Map<TaskQueryResponse, TaskDto>(taskResponse);
        }

        public async Task<TaskDetailDto> GetDetailAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

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

            AdjustLanguageForTask(taskResponse);

            var detailDto = _objectMapper.Map<TaskQueryResponse, TaskDetailDto>(taskResponse);

            detailDto.ChecklistItems = await GetChecklistsByTaskIdAsync(id);
            detailDto.SubTasks = await GetSubTasksByTaskIdAsync(id);
            detailDto.ActivityLogs = await GetActivityLogsByTaskIdAsync(id);
            detailDto.Histories = await GetHistoriesByTaskIdAsync(id);
            detailDto.Comments = await GetCommentsByTaskIdAsync(id);
            detailDto.Attachments = await GetAttachmentsByTaskIdAsync(id);

            return detailDto;
        }

        #endregion

        #region --- Task Checklists ---

        public async Task<List<ChecklistItemDto>> GetChecklistsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<ChecklistItemDto>(
                "sp_TaskChecklist_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var checklists = result.AsList();
            foreach (var item in checklists)
            {
                AdjustLanguageForChecklist(item);
            }

            return checklists;
        }

        public async Task<ChecklistItemDto> CreateChecklistAsync(Guid id, Guid taskId, CreateUpdateChecklistItemDto input, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = await AutoTranslateToEnglishAsync(input.Title);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Title", input.Title);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@IsDone", input.IsDone);
            parameters.Add("@CreatorId", creatorId);

            var checklist = await connection.QueryFirstOrDefaultAsync<ChecklistItemDto>(
                "sp_TaskChecklist_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            AdjustLanguageForChecklist(checklist);

            return checklist ?? throw new InvalidOperationException("Không thể tạo checklist.");
        }

        public async Task<ChecklistItemDto> UpdateChecklistAsync(Guid id, CreateUpdateChecklistItemDto input, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = await AutoTranslateToEnglishAsync(input.Title);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Title", input.Title);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@IsDone", input.IsDone);
            parameters.Add("@ModifierId", modifierId);

            var checklist = await connection.QueryFirstOrDefaultAsync<ChecklistItemDto>(
                "sp_TaskChecklist_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            AdjustLanguageForChecklist(checklist);

            return checklist ?? throw new InvalidOperationException("Không tìm thấy mục checklist để cập nhật.");
        }

        public async Task DeleteChecklistAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_TaskChecklist_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        #endregion

        #region --- Task Sub-tasks ---

        public async Task<List<SubTaskDto>> GetSubTasksByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<SubTaskDto>(
                "sp_SubTask_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var subTasks = result.AsList();
            foreach (var item in subTasks)
            {
                AdjustLanguageForSubTask(item);
            }

            return subTasks;
        }

        public async Task<SubTaskDto> CreateSubTaskAsync(Guid id, Guid taskId, CreateUpdateSubTaskDto input, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = await AutoTranslateToEnglishAsync(input.Title);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Title", input.Title);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@CreatorId", creatorId);

            var subTask = await connection.QueryFirstOrDefaultAsync<SubTaskDto>(
                "sp_SubTask_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            AdjustLanguageForSubTask(subTask);

            return subTask ?? throw new InvalidOperationException("Không thể tạo sub-task.");
        }

        public async Task<SubTaskDto> UpdateSubTaskAsync(Guid id, CreateUpdateSubTaskDto input, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

            var titleEn = await AutoTranslateToEnglishAsync(input.Title);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Title", input.Title);
            parameters.Add("@TitleEn", titleEn);
            parameters.Add("@ModifierId", modifierId);

            var subTask = await connection.QueryFirstOrDefaultAsync<SubTaskDto>(
                "sp_SubTask_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            AdjustLanguageForSubTask(subTask);

            return subTask ?? throw new InvalidOperationException("Không tìm thấy sub-task để cập nhật.");
        }

        public async Task DeleteSubTaskAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_SubTask_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        #endregion

        #region --- Task Attachments ---

        public async Task<List<TaskAttachmentDto>> GetAttachmentsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskAttachmentDto>(
                "sp_TaskAttachment_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<TaskAttachmentDto> CreateAttachmentAsync(Guid id, Guid taskId, string fileName, string fileUrl, long fileSize, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@FileName", fileName);
            parameters.Add("@FileUrl", fileUrl);
            parameters.Add("@FileSize", fileSize);
            parameters.Add("@CreatorId", creatorId);

            var attachment = await connection.QueryFirstOrDefaultAsync<TaskAttachmentDto>(
                "sp_TaskAttachment_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return attachment ?? new TaskAttachmentDto
            {
                FileName = fileName,
                FileUrl = fileUrl
            };
        }

        public async Task DeleteAttachmentAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_TaskAttachment_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        #endregion

        #region --- Task Activity Logs & Histories ---

        public async Task<List<TaskActivityLogDto>> GetActivityLogsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskActivityLogDto>(
                "sp_TaskActivityLog_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task CreateActivityLogAsync(Guid id, Guid taskId, string action, string actionEn, string description, Guid? creatorId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Action", action);
            parameters.Add("@ActionEn", actionEn); // Thêm tham số ActionEn vào đây
            parameters.Add("@Description", description);
            parameters.Add("@CreatorId", creatorId);

            await connection.ExecuteAsync(
                "sp_TaskActivityLog_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<TaskHistoryDto>> GetHistoriesByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

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

        #endregion

        #region --- Task Comments & Comment Attachments ---

        public async Task<List<TaskCommentDto>> GetCommentsByTaskIdAsync(Guid taskId)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@TaskId", taskId);

            var result = await connection.QueryAsync<TaskCommentDto>(
                "sp_TaskComment_GetByTaskId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var comments = result.AsList();
            foreach (var comment in comments)
            {
                comment.Attachments = await GetCommentAttachmentsByCommentIdAsync(comment.Id);
                AdjustLanguageForComment(comment);
            }

            return comments;
        }

        public async Task<TaskCommentDto> GetCommentByIdAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var comment = await connection.QueryFirstOrDefaultAsync<TaskCommentDto>(
                "sp_TaskComment_GetById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (comment == null)
            {
                throw new InvalidOperationException("Không tìm thấy bình luận.");
            }

            comment.Attachments = await GetCommentAttachmentsByCommentIdAsync(comment.Id);
            AdjustLanguageForComment(comment);
            return comment;
        }

        public async Task<TaskCommentDto> CreateCommentAsync(Guid id, Guid taskId, string text, string? fileName, string? fileUrl, Guid? userId, Guid? creatorId, Guid? tenantId)
        {
            var connection = await GetDbConnectionAsync();

            var textEn = await AutoTranslateToEnglishAsync(text);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@TaskId", taskId);
            parameters.Add("@Text", text);
            parameters.Add("@TextEn", textEn);
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

            AdjustLanguageForComment(comment);

            return comment ?? throw new InvalidOperationException("Không thể tạo bình luận mới.");
        }

        public async Task<TaskCommentDto> UpdateCommentAsync(Guid id, string text, Guid? modifierId)
        {
            var connection = await GetDbConnectionAsync();

            var textEn = await AutoTranslateToEnglishAsync(text);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Text", text);
            parameters.Add("@TextEn", textEn);
            parameters.Add("@ModifierId", modifierId);

            var comment = await connection.QueryFirstOrDefaultAsync<TaskCommentDto>(
                "sp_TaskComment_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            AdjustLanguageForComment(comment);

            return comment ?? throw new InvalidOperationException("Không tìm thấy bình luận để cập nhật.");
        }

        public async Task DeleteCommentAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_TaskComment_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<CommentAttachmentDto>> GetCommentAttachmentsByCommentIdAsync(Guid taskCommentId)
        {
            var connection = await GetDbConnectionAsync();

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

        public async Task DeleteCommentAttachmentAsync(Guid id)
        {
            var connection = await GetDbConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_TaskCommentAttachment_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        #endregion
    }
}