using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace TaskManagement.Tasks;

[Authorize(TaskManagementPermissions.Tasks.Default)]
public class TaskAppService(
    IRepository<TaskItem, Guid> repository,
    IRepository<IdentityUser, Guid> userRepository,
    IWebHostEnvironment environment,
    IRepository<SubTask, Guid> subTaskRepository,
    IRepository<TaskChecklistItem, Guid> checklistItemRepository,
    IRepository<TaskActivityLog, Guid> activityLogRepository,
    IRepository<TaskComment, Guid> commentRepository)
    : CrudAppService<
        TaskItem,
        TaskDto,
        Guid,
        GetTaskListInputDto,
        CreateTaskInputDto,
        UpdateTaskInputDto>(repository), ITaskAppService
{
    private readonly string[] _allowedExtensions = [".pdf", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".xlsx"];
    private const int MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB

    protected override string? GetPolicyName { get; set; } = TaskManagementPermissions.Tasks.Default;
    protected override string? GetListPolicyName { get; set; } = TaskManagementPermissions.Tasks.Default;
    protected override string? CreatePolicyName { get; set; } = TaskManagementPermissions.Tasks.Create;
    protected override string? UpdatePolicyName { get; set; } = TaskManagementPermissions.Tasks.Edit;
    protected override string? DeletePolicyName { get; set; } = TaskManagementPermissions.Tasks.Delete;

    #region Query & Sorting Filter
    protected override async Task<IQueryable<TaskItem>> CreateFilteredQueryAsync(GetTaskListInputDto input)
    {
        var query = await Repository.GetQueryableAsync();
        var currentUserId = CurrentUser.Id;
        var searchKeyword = !string.IsNullOrWhiteSpace(input.Keyword) ? input.Keyword : input.Filter;

        return query
            .WhereIf(!string.IsNullOrWhiteSpace(searchKeyword), x =>
                x.Title.Contains(searchKeyword!) || (x.Description != null && x.Description.Contains(searchKeyword!)))
            .WhereIf(input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty, x => x.CategoryId == input.CategoryId!.Value)
            .WhereIf(input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty, x => x.AssigneeId == input.AssigneeId!.Value)
            .WhereIf(input.Priority.HasValue, x => x.Priority == input.Priority!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.OnlyMyTasks && currentUserId.HasValue, x => x.AssigneeId == currentUserId!.Value);
    }

    protected override IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, GetTaskListInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            return query.OrderByDescending(x => x.CreationTime);
        }

        try
        {
            return base.ApplySorting(query, input);
        }
        catch
        {
            var sorting = input.Sorting.Trim();
            var isDescending = sorting.EndsWith("DESC", StringComparison.OrdinalIgnoreCase);
            var sortField = sorting.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant() ?? "";

            return sortField switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "priority" => isDescending ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority),
                "status" => isDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
                "duedate" => isDescending ? query.OrderByDescending(x => x.DueDate) : query.OrderBy(x => x.DueDate),
                "creationtime" => isDescending ? query.OrderByDescending(x => x.CreationTime) : query.OrderBy(x => x.CreationTime),
                _ => query.OrderByDescending(x => x.CreationTime)
            };
        }
    }
    #endregion

    #region Task Detail & CRUD
    [HttpGet("/api/app/task/{id}/detail")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<TaskDetailDto> GetTaskDetailAsync(Guid id)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy công việc có ID: {id}");

        var dto = ObjectMapper.Map<TaskItem, TaskDetailDto>(entity);
        dto.FileName = entity.FileName;
        dto.FileUrl = entity.FileUrl;

        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            var user = await userRepository.FindAsync(entity.AssigneeId.Value);
            if (user != null)
            {
                dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty);
                dto.AssigneeUserName = user.UserName;
            }
        }

        var subTasks = await subTaskRepository.GetListAsync(x => x.TaskId == id);
        var subTaskDtos = ObjectMapper.Map<List<SubTask>, List<SubTaskDto>>(subTasks);

        var subTaskAssigneeIds = subTasks
            .Where(x => x.AssigneeId.HasValue && x.AssigneeId.Value != Guid.Empty)
            .Select(x => x.AssigneeId!.Value)
            .Distinct()
            .ToList();

        if (subTaskAssigneeIds.Count > 0)
        {
            var userQuery = await userRepository.GetQueryableAsync();
            var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => subTaskAssigneeIds.Contains(x.Id)));
            var userDict = users.ToDictionary(u => u.Id);

            foreach (var subDto in subTaskDtos)
            {
                if (subDto.AssigneeId.HasValue && userDict.TryGetValue(subDto.AssigneeId.Value, out var u))
                {
                    subDto.AssigneeName = !string.IsNullOrWhiteSpace(u.Name) ? u.Name : (u.UserName ?? string.Empty);
                }
            }
        }
        dto.SubTasks = subTaskDtos;

        var checklists = await checklistItemRepository.GetListAsync(x => x.TaskId == id);
        dto.ChecklistItems = ObjectMapper.Map<List<TaskChecklistItem>, List<ChecklistItemDto>>(checklists);

        var logs = await activityLogRepository.GetListAsync(x => x.TaskId == id);
        dto.ActivityLogs = ObjectMapper.Map<List<TaskActivityLog>, List<TaskActivityLogDto>>(logs.OrderByDescending(x => x.CreationTime).ToList());

        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == id));
        var sortedComments = comments.OrderByDescending(x => x.CreationTime).ToList();

        var creatorIds = sortedComments
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        var commentUserDict = new Dictionary<Guid, IdentityUser>();
        if (creatorIds.Count > 0)
        {
            var userQuery = await userRepository.GetQueryableAsync();
            var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => creatorIds.Contains(x.Id)));
            commentUserDict = users.ToDictionary(u => u.Id);
        }

        var commentDtos = new List<TaskCommentDto>();
        foreach (var c in sortedComments)
        {
            var cDto = ObjectMapper.Map<TaskComment, TaskCommentDto>(c);
            cDto.CreatorName = (c.CreatorId.HasValue && commentUserDict.TryGetValue(c.CreatorId.Value, out var user))
                ? (!string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty))
                : "Hệ thống";

            var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(c.Attachments?.ToList() ?? []);
            if (attachments.Count == 0)
            {
                attachments = ParseCommentAttachments(c.FileUrl, c.FileName);
            }
            cDto.Attachments = attachments;

            commentDtos.Add(cDto);
        }
        dto.Comments = commentDtos;

        var lastSubmissionComment = comments
            .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (lastSubmissionComment != null)
        {
            var rawText = lastSubmissionComment.Text;

            if (rawText.Contains("[NỘP TRÌNH DUYỆT]:"))
            {
                dto.SubmissionNote = rawText.Substring(rawText.IndexOf("[NỘP TRÌNH DUYỆT]:") + "[NỘP TRÌNH DUYỆT]:".Length).Trim();
            }
            else if (rawText.Contains("[NỘP TRÌNH DUYỆT]"))
            {
                dto.SubmissionNote = rawText.Substring(rawText.IndexOf("[NỘP TRÌNH DUYỆT]") + "[NỘP TRÌNH DUYỆT]".Length).Trim();
            }
            else
            {
                dto.SubmissionNote = rawText;
            }

            dto.SubmittedAt = lastSubmissionComment.CreationTime;

            var submissionAttachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(lastSubmissionComment.Attachments?.ToList() ?? []);
            if (submissionAttachments.Count == 0)
            {
                submissionAttachments = ParseCommentAttachments(lastSubmissionComment.FileUrl, lastSubmissionComment.FileName);
            }

            dto.SubmissionFiles = submissionAttachments
                .Select(a => new TaskFileDto
                {
                    FileName = a.FileName,
                    FileUrl = a.FileUrl
                })
                .ToList();
        }

        return dto;
    }

    [HttpGet("/api/app/task/{taskId}/timeline")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskActivityLogDto>> GetTaskTimelineAsync(Guid taskId)
    {
        if (!await Repository.AnyAsync(x => x.Id == taskId))
        {
            throw new UserFriendlyException("Không tìm thấy công việc.");
        }

        var logs = await activityLogRepository.GetListAsync(x => x.TaskId == taskId);
        var sortedLogs = logs.OrderByDescending(x => x.CreationTime).ToList();

        return ObjectMapper.Map<List<TaskActivityLog>, List<TaskActivityLogDto>>(sortedLogs);
    }

    [UnitOfWork]
    public override async Task<TaskDto> CreateAsync(CreateTaskInputDto input)
    {
        var entity = await MapToEntityAsync(input);
        entity.ProgressPercent = CalculateProgressByStatus(entity.Status, entity.ProgressPercent);

        await ProcessTaskAttachmentsAsync(input.Attachments, entity);

        await Repository.InsertAsync(entity, autoSave: true);
        await LogActivityAsync(entity.Id, $"Đã tạo công việc: '{entity.Title}' (Tiến độ: {entity.ProgressPercent}%)");

        return await MapToGetOutputDtoAsync(entity);
    }

    [UnitOfWork]
    public override async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskInputDto input)
    {
        var entity = await GetEntityByIdAsync(id);
        await MapToEntityAsync(input, entity);

        entity.ProgressPercent = CalculateProgressByStatus(entity.Status, entity.ProgressPercent);

        await ProcessTaskAttachmentsAsync(input.Attachments, entity);

        await Repository.UpdateAsync(entity, autoSave: true);
        await LogActivityAsync(entity.Id, $"Đã cập nhật thông tin công việc (Tiến độ: {entity.ProgressPercent}%)");

        return await MapToGetOutputDtoAsync(entity);
    }

    [HttpPut("/api/app/task/{id}/status")]
    [HttpPost("/api/app/task/{id}/status")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDto> UpdateStatusAsync(Guid id, [FromQuery] TaskItemStatus status)
    {
        var entity = await GetEntityByIdAsync(id);
        var oldStatus = entity.Status;

        entity.Status = status;
        entity.ProgressPercent = CalculateProgressByStatus(status, entity.ProgressPercent);

        await Repository.UpdateAsync(entity, autoSave: true);
        await LogActivityAsync(id, $"Thay đổi trạng thái từ '{oldStatus}' sang '{status}' (Tiến độ: {entity.ProgressPercent}%)");

        return await MapToGetOutputDtoAsync(entity);
    }

    [HttpPost("/api/app/task/{id}/assignee")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDto> UpdateAssigneeAsync(Guid id, [FromQuery] Guid? assigneeId)
    {
        var entity = await GetEntityByIdAsync(id);
        entity.AssigneeId = assigneeId.HasValue && assigneeId.Value != Guid.Empty ? assigneeId : null;

        await Repository.UpdateAsync(entity, autoSave: true);

        string assigneeName = "Chưa giao";
        if (entity.AssigneeId.HasValue)
        {
            var user = await userRepository.FindAsync(entity.AssigneeId.Value);
            assigneeName = user?.Name ?? user?.UserName ?? entity.AssigneeId.Value.ToString();
        }
        await LogActivityAsync(id, $"Giao công việc cho: {assigneeName}");

        return await MapToGetOutputDtoAsync(entity);
    }
    #endregion

    #region Submission & Review
    [HttpPost("/api/app/task/{id}/submit-for-review")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> SubmitForReviewAsync(Guid id, [FromBody] SubmitReviewInputDto? input = null)
    {
        var task = await Repository.GetAsync(id);

        if (!task.AssigneeId.HasValue)
        {
            throw new UserFriendlyException("Công việc chưa được phân công cho ai nên không thể nộp duyệt!");
        }

        if (task.AssigneeId != CurrentUser.Id)
        {
            throw new UserFriendlyException("Chỉ người thực hiện công việc này mới có quyền nộp duyệt!");
        }

        if (task.Status == TaskItemStatus.Completed || task.Status == TaskItemStatus.Canceled)
        {
            throw new UserFriendlyException("Công việc đã hoàn thành hoặc đã bị hủy, không thể nộp duyệt!");
        }

        var noteContent = input?.Note?.Trim() ?? string.Empty;
        var hasAttachments = input?.Attachments != null && input.Attachments.Count > 0;

        if (string.IsNullOrWhiteSpace(noteContent) && !hasAttachments)
        {
            throw new UserFriendlyException("Vui lòng nhập nội dung ghi chú hoặc đính kèm tệp báo cáo!");
        }

        var commentText = string.IsNullOrWhiteSpace(noteContent)
            ? "[NỘP TRÌNH DUYỆT]"
            : $"[NỘP TRÌNH DUYỆT]: {noteContent}";

        var commentAttachments = input?.Attachments?.Select(a => new CommentAttachmentDto
        {
            FileName = a.FileName,
            FileContent = a.FileContent
        }).ToList();

        await CreateCommentAsync(id, new CreateTaskCommentDto
        {
            Text = commentText,
            Attachments = commentAttachments
        });

        task.Status = TaskItemStatus.InReview;
        await Repository.UpdateAsync(task, autoSave: true);

        var logDetail = hasAttachments ? " (kèm tệp báo cáo kết quả)" : "";
        await LogActivityAsync(id, $"Đã gửi yêu cầu phê duyệt công việc{logDetail}");

        return await GetTaskDetailAsync(id);
    }

    [HttpPut("/api/app/task/{id}/submission")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> UpdateSubmissionAsync(Guid id, [FromBody] SubmitReviewInputDto input)
    {
        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == id));

        var lastSubmissionComment = comments
            .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault() ?? throw new UserFriendlyException("Không tìm thấy thông tin nộp bài duyệt cần chỉnh sửa.");

        if (lastSubmissionComment.CreatorId != CurrentUser.Id)
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa mục nộp bài này!");
        }

        var noteContent = input?.Note?.Trim() ?? string.Empty;
        var newText = string.IsNullOrWhiteSpace(noteContent) ? "[NỘP TRÌNH DUYỆT]" : $"[NỘP TRÌNH DUYỆT]: {noteContent}";

        lastSubmissionComment.Text = newText;

        if (input?.Attachments != null && input.Attachments.Count > 0)
        {
            lastSubmissionComment.Attachments.Clear();
            await ProcessCommentAttachmentsAsync(input.Attachments.Select(a => new CommentAttachmentDto
            {
                FileName = a.FileName,
                FileContent = a.FileContent
            }).ToList(), lastSubmissionComment);

            if (lastSubmissionComment.Attachments != null && lastSubmissionComment.Attachments.Count > 0)
            {
                lastSubmissionComment.FileUrl = string.Join(";", lastSubmissionComment.Attachments.Select(a => a.FileUrl));
                lastSubmissionComment.FileName = string.Join(";", lastSubmissionComment.Attachments.Select(a => a.FileName));
            }
        }

        await commentRepository.UpdateAsync(lastSubmissionComment, autoSave: true);
        await LogActivityAsync(id, "Đã cập nhật lại nội dung nộp bài duyệt");

        return await GetTaskDetailAsync(id);
    }

    [HttpDelete("/api/app/task/{id}/submission")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> DeleteSubmissionAsync(Guid id)
    {
        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == id));

        var lastSubmissionComment = comments
            .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault() ?? throw new UserFriendlyException("Không tìm thấy thông tin nộp bài duyệt để xóa.");

        if (lastSubmissionComment.CreatorId != CurrentUser.Id)
        {
            throw new UserFriendlyException("Bạn không có quyền xóa mục nộp bài này!");
        }

        await commentRepository.DeleteAsync(lastSubmissionComment.Id, autoSave: true);

        var task = await Repository.GetAsync(id);
        if (task.Status == TaskItemStatus.InReview)
        {
            task.Status = TaskItemStatus.InProgress;
            await Repository.UpdateAsync(task, autoSave: true);
        }

        await LogActivityAsync(id, "Đã hủy/xóa lượt nộp bài duyệt");

        return await GetTaskDetailAsync(id);
    }

    [HttpPost("/api/app/task/{id}/approve")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> ApproveAsync(Guid id)
    {
        var task = await Repository.GetAsync(id);

        var isCreator = task.CreatorId.HasValue && task.CreatorId == CurrentUser.Id;
        var isManagerOrAdmin = CurrentUser.IsInRole("Manager") || CurrentUser.IsInRole("admin") || CurrentUser.IsInRole("Admin");

        if (!isCreator && !isManagerOrAdmin)
        {
            throw new UserFriendlyException("Bạn không có quyền phê duyệt công việc này!");
        }

        if (task.Status != TaskItemStatus.InReview)
        {
            throw new UserFriendlyException("Công việc này chưa ở trạng thái chờ duyệt (InReview)!");
        }

        task.Status = TaskItemStatus.Completed;
        task.ProgressPercent = 100;
        await Repository.UpdateAsync(task, autoSave: true);

        await LogActivityAsync(id, "Đã phê duyệt công việc (Đã hoàn thành)");

        return await GetTaskDetailAsync(id);
    }

    [HttpPost("/api/app/task/{id}/reject")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> RejectAsync(Guid id, [FromBody] RejectTaskInputDto input)
    {
        var task = await Repository.GetAsync(id);

        var isCreator = task.CreatorId.HasValue && task.CreatorId == CurrentUser.Id;
        var isManagerOrAdmin = CurrentUser.IsInRole("Manager") || CurrentUser.IsInRole("admin") || CurrentUser.IsInRole("Admin");

        if (!isCreator && !isManagerOrAdmin)
        {
            throw new UserFriendlyException("Bạn không có quyền từ chối công việc này!");
        }

        if (task.Status != TaskItemStatus.InReview)
        {
            throw new UserFriendlyException("Công việc này không ở trạng thái chờ duyệt (InReview)!");
        }

        task.Status = TaskItemStatus.InProgress;
        await Repository.UpdateAsync(task, autoSave: true);

        await CreateCommentAsync(id, new CreateTaskCommentDto
        {
            Text = $"[TỪ CHỐI DUYỆT]: {input.Reason}"
        });

        await LogActivityAsync(id, $"Đã từ chối duyệt. Lý do: {input.Reason}");

        return await GetTaskDetailAsync(id);
    }
    #endregion

    #region SubTask Management
    [HttpPost("/api/app/task/{taskId}/sub-task")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> CreateSubTaskAsync(Guid taskId, [FromBody] CreateUpdateSubTaskDto input)
    {
        if (!await Repository.AnyAsync(x => x.Id == taskId))
            throw new UserFriendlyException("Công việc gốc không tồn tại.");

        var subTask = new SubTask(GuidGenerator.Create())
        {
            TaskId = taskId,
            Title = input.Title,
            AssigneeId = input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty ? input.AssigneeId : null,
            IsCompleted = false
        };

        await subTaskRepository.InsertAsync(subTask, autoSave: true);
        await LogActivityAsync(taskId, $"Đã thêm công việc con: '{input.Title}'");

        return ObjectMapper.Map<SubTask, SubTaskDto>(subTask);
    }

    [HttpPut("/api/app/task/sub-task/{subTaskId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> UpdateSubTaskAsync(Guid subTaskId, [FromBody] CreateUpdateSubTaskDto input)
    {
        var subTask = await subTaskRepository.GetAsync(subTaskId);
        subTask.Title = input.Title;
        subTask.AssigneeId = input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty ? input.AssigneeId : null;

        await subTaskRepository.UpdateAsync(subTask, autoSave: true);
        await LogActivityAsync(subTask.TaskId, $"Đã cập nhật công việc con: '{input.Title}'");

        return ObjectMapper.Map<SubTask, SubTaskDto>(subTask);
    }

    [HttpPut("/api/app/task/sub-task/{subTaskId}/toggle")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task ToggleSubTaskStatusAsync(Guid subTaskId)
    {
        var subTask = await subTaskRepository.GetAsync(subTaskId);
        subTask.IsCompleted = !subTask.IsCompleted;

        await subTaskRepository.UpdateAsync(subTask, autoSave: true);
        await LogActivityAsync(subTask.TaskId, $"Đã cập nhật công việc con '{subTask.Title}' sang {(subTask.IsCompleted ? "Hoàn thành" : "Đang làm")}");
    }

    [HttpDelete("/api/app/task/sub-task/{subTaskId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task DeleteSubTaskAsync(Guid subTaskId)
    {
        var subTask = await subTaskRepository.FindAsync(subTaskId);
        if (subTask != null)
        {
            await subTaskRepository.DeleteAsync(subTaskId);
            await LogActivityAsync(subTask.TaskId, $"Đã xóa công việc phụ: '{subTask.Title}'");
        }
    }
    #endregion

    #region Checklist Management
    [HttpPost("/api/app/task/{taskId}/checklist-item")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<ChecklistItemDto> CreateChecklistItemAsync(Guid taskId, [FromBody] CreateUpdateChecklistItemDto input)
    {
        if (!await Repository.AnyAsync(x => x.Id == taskId))
            throw new UserFriendlyException("Công việc gốc không tồn tại.");

        var item = new TaskChecklistItem(GuidGenerator.Create())
        {
            TaskId = taskId,
            Title = input.Title,
            IsDone = false
        };

        await checklistItemRepository.InsertAsync(item, autoSave: true);
        await LogActivityAsync(taskId, $"Đã thêm hạng mục kiểm tra: '{input.Title}'");

        return ObjectMapper.Map<TaskChecklistItem, ChecklistItemDto>(item);
    }

    [HttpPut("/api/app/task/checklist-item/{itemId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, [FromBody] CreateUpdateChecklistItemDto input)
    {
        var item = await checklistItemRepository.GetAsync(itemId);
        item.Title = input.Title;

        await checklistItemRepository.UpdateAsync(item, autoSave: true);
        await LogActivityAsync(item.TaskId, $"Đã cập nhật mục kiểm tra: '{input.Title}'");

        return ObjectMapper.Map<TaskChecklistItem, ChecklistItemDto>(item);
    }

    [HttpPut("/api/app/task/checklist-item/{itemId}/toggle")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task ToggleChecklistItemStatusAsync(Guid itemId)
    {
        var item = await checklistItemRepository.GetAsync(itemId);
        item.IsDone = !item.IsDone;

        await checklistItemRepository.UpdateAsync(item, autoSave: true);
        await LogActivityAsync(item.TaskId, $"Đã cập nhật trạng thái mục kiểm tra '{item.Title}' sang {(item.IsDone ? "Hoàn thành" : "Chưa hoàn thành")}");
    }

    [HttpDelete("/api/app/task/checklist-item/{itemId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task DeleteChecklistItemAsync(Guid itemId)
    {
        var item = await checklistItemRepository.FindAsync(itemId);
        if (item != null)
        {
            await checklistItemRepository.DeleteAsync(itemId);
            await LogActivityAsync(item.TaskId, $"Đã xóa mục kiểm tra: '{item.Title}'");
        }
    }
    #endregion

    #region Comments Management
    [HttpGet("/api/app/task/{taskId}/comments")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId)
    {
        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == taskId));
        var sortedComments = comments.OrderByDescending(x => x.CreationTime).ToList();

        var creatorIds = sortedComments
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        var userDict = new Dictionary<Guid, IdentityUser>();
        if (creatorIds.Count > 0)
        {
            var userQuery = await userRepository.GetQueryableAsync();
            var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => creatorIds.Contains(x.Id)));
            userDict = users.ToDictionary(u => u.Id);
        }

        var dtos = new List<TaskCommentDto>();
        foreach (var c in sortedComments)
        {
            var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(c);
            dto.CreatorName = (c.CreatorId.HasValue && userDict.TryGetValue(c.CreatorId.Value, out var user))
                ? (!string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty))
                : "Hệ thống";

            var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(c.Attachments?.ToList() ?? []);
            if (attachments.Count == 0)
            {
                attachments = ParseCommentAttachments(c.FileUrl, c.FileName);
            }
            dto.Attachments = attachments;

            dtos.Add(dto);
        }

        return dtos;
    }

    [HttpPost("/api/app/task/{taskId}/comment")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task<TaskCommentDto> CreateCommentAsync(Guid taskId, [FromBody] CreateTaskCommentDto input)
    {
        if (!await Repository.AnyAsync(x => x.Id == taskId))
            throw new UserFriendlyException("Công việc không tồn tại.");

        if (string.IsNullOrWhiteSpace(input.Text) && (input.Attachments == null || input.Attachments.Count == 0))
            throw new UserFriendlyException("Nội dung bình luận hoặc tệp đính kèm không được để trống.");

        var comment = new TaskComment(GuidGenerator.Create())
        {
            TaskId = taskId,
            Text = input.Text ?? string.Empty,
            CreatorId = CurrentUser.Id
        };

        await ProcessCommentAttachmentsAsync(input.Attachments, comment);

        if (comment.Attachments != null && comment.Attachments.Count > 0)
        {
            comment.FileUrl = string.Join(";", comment.Attachments.Select(a => a.FileUrl));
            comment.FileName = string.Join(";", comment.Attachments.Select(a => a.FileName));
        }

        var insertedComment = await commentRepository.InsertAsync(comment, autoSave: true);
        await LogActivityAsync(taskId, $"Đã thêm bình luận mới{(comment.Attachments?.Count > 0 || !string.IsNullOrEmpty(comment.FileName) ? " (kèm tệp đính kèm)" : "")}");

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(insertedComment);

        dto.Id = insertedComment.Id;
        dto.TaskId = taskId;
        dto.CreatorId = CurrentUser.Id;
        dto.CreationTime = insertedComment.CreationTime;

        dto.CreatorName = !string.IsNullOrWhiteSpace(CurrentUser.Name)
            ? CurrentUser.Name
            : (CurrentUser.UserName ?? "Hệ thống");

        if (insertedComment.Attachments != null && insertedComment.Attachments.Count > 0)
        {
            dto.Attachments = insertedComment.Attachments.Select(a => new CommentAttachmentDto
            {
                FileName = a.FileName,
                FileUrl = a.FileUrl
            }).ToList();
        }
        else
        {
            dto.Attachments = ParseCommentAttachments(insertedComment.FileUrl, insertedComment.FileName);
        }

        return dto;
    }

    [HttpPut("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task<TaskCommentDto> UpdateCommentAsync(Guid commentId, [FromBody] UpdateTaskCommentDto input)
    {
        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comment = await AsyncExecuter.FirstOrDefaultAsync(commentQuery.Where(x => x.Id == commentId))
            ?? throw new UserFriendlyException("Không tìm thấy bình luận.");

        if (comment.CreatorId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Edit))
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa bình luận này.");
        }

        comment.Text = input.Text ?? string.Empty;
        await commentRepository.UpdateAsync(comment, autoSave: true);
        await LogActivityAsync(comment.TaskId, "Đã chỉnh sửa bình luận");

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(comment);
        dto.CreatorName = !string.IsNullOrWhiteSpace(CurrentUser.Name) ? CurrentUser.Name : (CurrentUser.UserName ?? "Hệ thống");

        var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(comment.Attachments?.ToList() ?? []);
        if (attachments.Count == 0)
        {
            attachments = ParseCommentAttachments(comment.FileUrl, comment.FileName);
        }
        dto.Attachments = attachments;

        return dto;
    }

    [HttpDelete("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task DeleteCommentAsync(Guid commentId)
    {
        var comment = await commentRepository.FindAsync(commentId);
        if (comment != null)
        {
            if (comment.CreatorId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Delete))
            {
                throw new UserFriendlyException("Bạn không có quyền xóa bình luận này.");
            }

            await commentRepository.DeleteAsync(commentId);
            await LogActivityAsync(comment.TaskId, "Đã xóa một bình luận");
        }
    }
    #endregion

    #region Helper Methods
    protected override async Task<TaskDto> MapToGetOutputDtoAsync(TaskItem entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        dto.FileName = entity.FileName;
        dto.FileUrl = entity.FileUrl;

        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            var user = await userRepository.FindAsync(entity.AssigneeId.Value);
            if (user != null)
            {
                dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty);
                dto.AssigneeUserName = user.UserName;
            }
        }

        var commentQuery = await commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == entity.Id));
        var lastSubmissionComment = comments
            .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (lastSubmissionComment != null)
        {
            var rawText = lastSubmissionComment.Text;

            if (rawText.Contains("[NỘP TRÌNH DUYỆT]:"))
            {
                dto.SubmissionNote = rawText.Substring(rawText.IndexOf("[NỘP TRÌNH DUYỆT]:") + "[NỘP TRÌNH DUYỆT]:".Length).Trim();
            }
            else if (rawText.Contains("[NỘP TRÌNH DUYỆT]"))
            {
                dto.SubmissionNote = rawText.Substring(rawText.IndexOf("[NỘP TRÌNH DUYỆT]") + "[NỘP TRÌNH DUYỆT]".Length).Trim();
            }
            else
            {
                dto.SubmissionNote = rawText;
            }

            var submissionAttachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(lastSubmissionComment.Attachments?.ToList() ?? []);
            if (submissionAttachments.Count == 0)
            {
                submissionAttachments = ParseCommentAttachments(lastSubmissionComment.FileUrl, lastSubmissionComment.FileName);
            }

            dto.SubmissionFiles = submissionAttachments
                .Select(a => new TaskFileDto
                {
                    FileName = a.FileName,
                    FileUrl = a.FileUrl
                })
                .ToList();
        }

        return dto;
    }

    protected override async Task<List<TaskDto>> MapToGetListOutputDtosAsync(List<TaskItem> entities)
    {
        var dtos = await base.MapToGetListOutputDtosAsync(entities);
        if (dtos.Count == 0) return [];

        for (int i = 0; i < entities.Count; i++)
        {
            dtos[i].FileName = entities[i].FileName;
            dtos[i].FileUrl = entities[i].FileUrl;
        }

        var assigneeIds = entities
            .Where(x => x.AssigneeId.HasValue && x.AssigneeId.Value != Guid.Empty)
            .Select(x => x.AssigneeId!.Value)
            .Distinct()
            .ToList();

        if (assigneeIds.Count > 0)
        {
            var queryable = await userRepository.GetQueryableAsync();
            var users = await AsyncExecuter.ToListAsync(queryable.Where(x => assigneeIds.Contains(x.Id)));
            var userDict = users.ToDictionary(u => u.Id);

            foreach (var dto in dtos)
            {
                if (dto.AssigneeId.HasValue && userDict.TryGetValue(dto.AssigneeId.Value, out var user))
                {
                    dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty);
                    dto.AssigneeUserName = user.UserName;
                }
            }
        }

        return dtos;
    }

    private static int CalculateProgressByStatus(TaskItemStatus status, int currentProgress)
    {
        return status switch
        {
            TaskItemStatus.New => 0,
            TaskItemStatus.Completed => 100,
            TaskItemStatus.InProgress => (currentProgress == 0 || currentProgress == 100) ? 50 : currentProgress,
            _ => currentProgress
        };
    }

    private async Task ProcessTaskAttachmentsAsync(List<TaskAttachmentDto>? attachments, TaskItem entity)
    {
        if (attachments == null || attachments.Count == 0) return;

        var fileUrls = new List<string>();
        var fileNames = new List<string>();

        if (!string.IsNullOrWhiteSpace(entity.FileUrl))
            fileUrls.AddRange(entity.FileUrl.Split(';', StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(entity.FileName))
            fileNames.AddRange(entity.FileName.Split(';', StringSplitOptions.RemoveEmptyEntries));

        foreach (var file in attachments)
        {
            if (!string.IsNullOrEmpty(file.FileContent) && !string.IsNullOrEmpty(file.FileName))
            {
                var cleanFileName = Path.GetFileName(file.FileName);
                var url = await SaveBase64FileAsync(cleanFileName, file.FileContent);
                fileUrls.Add(url);
                fileNames.Add(cleanFileName);
            }
        }

        if (fileUrls.Count > 0)
        {
            entity.FileUrl = string.Join(";", fileUrls);
            entity.FileName = string.Join(";", fileNames);
        }
    }

    private async Task ProcessCommentAttachmentsAsync(List<CommentAttachmentDto>? attachments, TaskComment comment)
    {
        if (attachments == null || attachments.Count == 0) return;

        foreach (var file in attachments)
        {
            if (!string.IsNullOrEmpty(file.FileContent) && !string.IsNullOrEmpty(file.FileName))
            {
                var cleanFileName = Path.GetFileName(file.FileName);
                var url = await SaveBase64FileAsync(cleanFileName, file.FileContent);
                comment.AddAttachment(cleanFileName, url);
            }
        }
    }

    private static List<CommentAttachmentDto> ParseCommentAttachments(string? fileUrl, string? fileName)
    {
        if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(fileName)) return [];

        var urls = fileUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var names = fileName.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var attachments = new List<CommentAttachmentDto>(urls.Length);

        for (int i = 0; i < urls.Length; i++)
        {
            attachments.Add(new CommentAttachmentDto
            {
                FileName = i < names.Length ? names[i] : "Attachment",
                FileUrl = urls[i]
            });
        }

        return attachments;
    }

    private async Task<string> SaveBase64FileAsync(string fileName, string base64Content)
    {
        var cleanBase64 = base64Content.Contains(',') ? base64Content.Split(',')[1] : base64Content;

        if ((cleanBase64.Length * 3 / 4) > MaxFileSizeInBytes)
        {
            throw new UserFriendlyException($"Tệp '{fileName}' vượt quá dung lượng tối đa cho phép (10MB).");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(cleanBase64);
        }
        catch
        {
            throw new UserFriendlyException($"Dữ liệu file '{fileName}' không hợp lệ.");
        }

        ValidateFile(fileName, bytes);

        var rootPath = environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(rootPath, "uploads");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        return $"/uploads/{uniqueFileName}";
    }

    private void ValidateFile(string fileName, byte[] bytes)
    {
        if (bytes.Length > MaxFileSizeInBytes)
        {
            throw new UserFriendlyException($"Tệp '{fileName}' vượt quá dung lượng tối đa cho phép (10MB).");
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_allowedExtensions.Contains(ext))
        {
            throw new UserFriendlyException($"Định dạng tệp '{ext}' không được hỗ trợ.");
        }
    }

    private async Task LogActivityAsync(Guid taskId, string action)
    {
        var log = new TaskActivityLog(GuidGenerator.Create())
        {
            TaskId = taskId,
            Action = action
        };
        await activityLogRepository.InsertAsync(log, autoSave: true);
    }
    #endregion
}