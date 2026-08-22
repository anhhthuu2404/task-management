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

    // --- QUERY & SEARCH LOGIC ---

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

    // --- MAIN TASK APIS ---

    [HttpGet("/api/app/task/{id}/detail")]
    public async Task<TaskDetailDto> GetTaskDetailAsync(Guid id)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy công việc có ID: {id}");

        var dto = ObjectMapper.Map<TaskItem, TaskDetailDto>(entity);

        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            var user = await userRepository.FindAsync(entity.AssigneeId.Value);
            if (user != null)
            {
                dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.UserName;
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
                    subDto.AssigneeName = !string.IsNullOrWhiteSpace(u.Name) ? u.Name : u.UserName;
                }
            }
        }
        dto.SubTasks = subTaskDtos;

        var checklists = await checklistItemRepository.GetListAsync(x => x.TaskId == id);
        dto.ChecklistItems = ObjectMapper.Map<List<TaskChecklistItem>, List<ChecklistItemDto>>(checklists);

        var logs = await activityLogRepository.GetListAsync(x => x.TaskId == id);
        dto.ActivityLogs = ObjectMapper.Map<List<TaskActivityLog>, List<TaskActivityLogDto>>(logs.OrderByDescending(x => x.CreationTime).ToList());

        return dto;
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

    // --- SUBTASK APIS ---

    [HttpPost("/api/app/task/{taskId}/sub-task")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> CreateSubTaskAsync(Guid taskId, CreateUpdateSubTaskDto input)
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
    public async Task<SubTaskDto> UpdateSubTaskAsync(Guid subTaskId, CreateUpdateSubTaskDto input)
    {
        var subTask = await subTaskRepository.GetAsync(subTaskId);
        subTask.Title = input.Title;
        subTask.AssigneeId = input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty ? input.AssigneeId : null;

        await subTaskRepository.UpdateAsync(subTask, autoSave: true);
        await LogActivityAsync(subTask.TaskId, $"Đã cập nhật công việc phụ: '{input.Title}'");

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

    // --- CHECKLIST APIS ---

    [HttpPost("/api/app/task/{taskId}/checklist-item")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<ChecklistItemDto> CreateChecklistItemAsync(Guid taskId, CreateUpdateChecklistItemDto input)
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
    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, CreateUpdateChecklistItemDto input)
    {
        var item = await checklistItemRepository.GetAsync(itemId);
        item.Title = input.Title;

        await checklistItemRepository.UpdateAsync(item, autoSave: true);
        await LogActivityAsync(item.TaskId, $"Đã cập nhật hạng mục kiểm tra: '{input.Title}'");

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

    // --- COMMENT APIS ---

    [HttpGet("/api/app/task/{taskId}/comments")]
    public async Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId)
    {
        var comments = await commentRepository.GetListAsync(x => x.TaskId == taskId);
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
                ? (!string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.UserName)
                : "Hệ thống";

            dto.Attachments = ParseCommentAttachments(c.FileUrl, c.FileName);
            dtos.Add(dto);
        }

        return dtos;
    }

    [HttpPost("/api/app/task/{taskId}/comment")]
    [UnitOfWork]
    public async Task<TaskCommentDto> CreateCommentAsync(Guid taskId, CreateTaskCommentDto input)
    {
        if (!await Repository.AnyAsync(x => x.Id == taskId))
            throw new UserFriendlyException("Công việc không tồn tại.");

        if (string.IsNullOrWhiteSpace(input.Text) && (input.Attachments == null || input.Attachments.Count == 0))
            throw new UserFriendlyException("Nội dung bình luận hoặc tệp đính kèm không được để trống.");

        var comment = new TaskComment(GuidGenerator.Create())
        {
            TaskId = taskId,
            Text = input.Text ?? string.Empty
        };

        await ProcessCommentAttachmentsAsync(input.Attachments, comment);

        await commentRepository.InsertAsync(comment, autoSave: true);
        await LogActivityAsync(taskId, $"Đã thêm bình luận mới{(string.IsNullOrEmpty(comment.FileName) ? "" : " (kèm tệp đính kèm)")}");

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(comment);
        dto.CreatorName = CurrentUser.Name ?? CurrentUser.UserName ?? "Hệ thống";
        dto.Attachments = ParseCommentAttachments(comment.FileUrl, comment.FileName);

        return dto;
    }

    [HttpPut("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskCommentDto> UpdateCommentAsync(Guid commentId, UpdateTaskCommentDto input)
    {
        var comment = await commentRepository.GetAsync(commentId);

        if (comment.CreatorId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Edit))
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa bình luận này.");
        }

        if (string.IsNullOrWhiteSpace(input.Text) && (input.Attachments == null || input.Attachments.Count == 0))
            throw new UserFriendlyException("Nội dung bình luận không được để trống.");

        comment.Text = input.Text ?? string.Empty;
        await ProcessCommentAttachmentsAsync(input.Attachments, comment);

        await commentRepository.UpdateAsync(comment, autoSave: true);
        await LogActivityAsync(comment.TaskId, "Đã cập nhật một bình luận");

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(comment);
        dto.CreatorName = CurrentUser.Name ?? CurrentUser.UserName ?? "Hệ thống";
        dto.Attachments = ParseCommentAttachments(comment.FileUrl, comment.FileName);

        return dto;
    }

    [HttpDelete("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Delete)]
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

    // --- HELPER METHODS ---

    protected override async Task<TaskDto> MapToGetOutputDtoAsync(TaskItem entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            var user = await userRepository.FindAsync(entity.AssigneeId.Value);
            if (user != null)
            {
                dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.UserName;
                dto.AssigneeUserName = user.UserName;
            }
        }
        return dto;
    }

    protected override async Task<List<TaskDto>> MapToGetListOutputDtosAsync(List<TaskItem> entities)
    {
        var dtos = await base.MapToGetListOutputDtosAsync(entities);
        if (dtos.Count == 0) return [];

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
                    dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.UserName;
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

        List<string> fileUrls = [];
        List<string> fileNames = [];

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

        List<string> fileUrls = [];
        List<string> fileNames = [];

        if (!string.IsNullOrWhiteSpace(comment.FileUrl))
            fileUrls.AddRange(comment.FileUrl.Split(';', StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(comment.FileName))
            fileNames.AddRange(comment.FileName.Split(';', StringSplitOptions.RemoveEmptyEntries));

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
            comment.FileUrl = string.Join(";", fileUrls);
            comment.FileName = string.Join(";", fileNames);
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
        if (base64Content.Contains(','))
        {
            base64Content = base64Content.Split(',')[1];
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Content);
        }
        catch
        {
            throw new UserFriendlyException($"Dữ liệu file '{fileName}' không hợp lệ.");
        }

        ValidateFile(fileName, bytes);

        var wwwRootPath = environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var folderPath = Path.Combine(wwwRootPath, "uploads");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var safeFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{GuidGenerator.Create()}_{safeFileName}";
        var filePath = Path.Combine(folderPath, uniqueFileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        return $"/uploads/{uniqueFileName}";
    }

    private void ValidateFile(string fileName, byte[] bytes)
    {
        var ext = Path.GetExtension(fileName)?.ToLower();
        if (string.IsNullOrEmpty(ext) || !_allowedExtensions.Contains(ext))
        {
            throw new UserFriendlyException($"Định dạng file '{fileName}' không được hỗ trợ.");
        }

        if (bytes.Length > MaxFileSizeInBytes)
        {
            throw new UserFriendlyException($"File '{fileName}' vượt quá dung lượng cho phép (10MB).");
        }
    }

    private async Task LogActivityAsync(Guid taskId, string action, string? details = null)
    {
        var log = new TaskActivityLog(GuidGenerator.Create())
        {
            TaskId = taskId,
            Action = action,
            UserName = CurrentUser.Name ?? CurrentUser.UserName ?? "System",
            Details = details
        };
        await activityLogRepository.InsertAsync(log, autoSave: true);
    }
}