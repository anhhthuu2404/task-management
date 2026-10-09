using Dapper;
using GTranslate.Translators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.Notifications;
using TaskManagement.Permissions;
using TaskManagement.Projects;
using TaskManagement.TaskHistories;
using TaskManagement.Tasks.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace TaskManagement.Tasks;

[Authorize(TaskManagementPermissions.Tasks.Default)]
public class TaskAppService : CrudAppService<
    TaskItem,
    TaskDto,
    Guid,
    GetTaskListInputDto,
    CreateTaskInputDto,
    UpdateTaskInputDto>, ITaskAppService
{
    private readonly string[] _allowedExtensions = [".pdf", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".xlsx", ".csv"];
    private const int MaxFileSizeInBytes = 10 * 1024 * 1024; // Giới hạn 10MB

    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly IRepository<SubTask, Guid> _subTaskRepository;
    private readonly IRepository<TaskChecklistItem, Guid> _checklistItemRepository;
    private readonly IRepository<TaskActivityLog, Guid> _activityLogRepository;
    private readonly IRepository<TaskComment, Guid> _commentRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectMilestone, Guid> _milestoneRepository;
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly ITaskProvider _taskProvider;
    private readonly ITranslator _translator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaskAppService(
        IRepository<TaskItem, Guid> repository,
        IRepository<IdentityUser, Guid> userRepository,
        IWebHostEnvironment environment,
        IRepository<SubTask, Guid> subTaskRepository,
        IRepository<TaskChecklistItem, Guid> checklistItemRepository,
        IRepository<TaskActivityLog, Guid> activityLogRepository,
        IRepository<TaskComment, Guid> commentRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<Notification, Guid> notificationRepository,
        IDistributedEventBus distributedEventBus,
        IRepository<ProjectMilestone, Guid> milestoneRepository,
        ITaskProvider taskProvider,
        ITranslator translator,
        IHttpContextAccessor httpContextAccessor)
        : base(repository)
    {
        _userRepository = userRepository;
        _environment = environment;
        _subTaskRepository = subTaskRepository;
        _checklistItemRepository = checklistItemRepository;
        _activityLogRepository = activityLogRepository;
        _commentRepository = commentRepository;
        _projectRepository = projectRepository;
        _milestoneRepository = milestoneRepository;
        _notificationRepository = notificationRepository;
        _distributedEventBus = distributedEventBus;
        _taskProvider = taskProvider;
        _translator = translator;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override string? GetPolicyName { get; set; } = TaskManagementPermissions.Tasks.Default;
    protected override string? GetListPolicyName { get; set; } = TaskManagementPermissions.Tasks.Default;
    protected override string? CreatePolicyName { get; set; } = TaskManagementPermissions.Tasks.Create;
    protected override string? UpdatePolicyName { get; set; } = TaskManagementPermissions.Tasks.Edit;
    protected override string? DeletePolicyName { get; set; } = TaskManagementPermissions.Tasks.Delete;

    #region Entity Mapping Overrides
    protected override async Task<TaskItem> MapToEntityAsync(CreateTaskInputDto input)
    {
        var entity = await base.MapToEntityAsync(input);
        entity.MilestoneId = input.MilestoneId;
        entity.ProjectId = input.ProjectId;

        await MapLocalizedTitleAsync(entity, input.Title);
        await MapLocalizedDescriptionAsync(entity, input.Description);
        return entity;
    }

    protected override async Task MapToEntityAsync(UpdateTaskInputDto input, TaskItem entity)
    {
        await base.MapToEntityAsync(input, entity);
        entity.MilestoneId = input.MilestoneId;
        entity.ProjectId = input.ProjectId;

        await MapLocalizedTitleAsync(entity, input.Title);
        await MapLocalizedDescriptionAsync(entity, input.Description);
    }

    private async Task MapLocalizedTitleAsync(TaskItem entity, string? rawTitle)
    {
        var trimmedTitle = rawTitle?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            return;
        }

        entity.Title = trimmedTitle;
        entity.TitleEn = await TranslateToEnglishOrDefaultAsync(trimmedTitle);
    }

    private async Task MapLocalizedDescriptionAsync(TaskItem entity, string? rawDescription)
    {
        var trimmedDescription = rawDescription?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedDescription))
        {
            entity.Description = null;
            entity.DescriptionEn = null;
            return;
        }

        entity.Description = trimmedDescription;
        entity.DescriptionEn = await TranslateToEnglishOrDefaultAsync(trimmedDescription);
    }

    private async Task<string> TranslateToEnglishOrDefaultAsync(string text)
    {
        try
        {
            var translation = await _translator.TranslateAsync(text, "en");
            if (translation != null && !string.IsNullOrEmpty(translation.Translation))
            {
                return translation.Translation;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"--- TRANSLATE ERROR ---: {ex.Message}");
        }

        return text;
    }
    #endregion

    #region Lookups
    [HttpGet("/api/app/task/category-lookup")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskLookupDto>> GetCategoryLookupAsync()
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        string textToTranslate = "Tất cả danh mục";
        string translatedText = textToTranslate;

        if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var translationResult = await _translator.TranslateAsync(textToTranslate, "vi", "en");
                if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                {
                    translatedText = translationResult.Translation;
                }
            }
            catch
            {
                translatedText = "All categories";
            }
        }

        return [new() { Id = Guid.Empty, DisplayName = translatedText }];
    }

    [HttpGet("/api/app/task/project-lookup")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskLookupDto>> GetProjectLookupAsync()
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        string textToTranslate = "Tất cả dự án";
        string translatedText = textToTranslate;

        if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var translationResult = await _translator.TranslateAsync(textToTranslate, "vi", "en");
                if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                {
                    translatedText = translationResult.Translation;
                }
            }
            catch
            {
                translatedText = "All projects";
            }
        }

        return [new() { Id = Guid.Empty, DisplayName = translatedText }];
    }

    [HttpGet("/api/app/task/status-lookup")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskLookupDto>> GetStatusLookupAsync()
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        async Task<string> TranslateSafeAsync(string viText, string enFallback)
        {
            if (!currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                return viText;
            }
            try
            {
                var res = await _translator.TranslateAsync(viText, "vi", "en");
                return (!string.IsNullOrEmpty(res?.Translation)) ? res.Translation : enFallback;
            }
            catch
            {
                return enFallback;
            }
        }

        return
        [
            new() { Id = Guid.Empty, DisplayName = await TranslateSafeAsync("Tất cả trạng thái", "All statuses") },
            new() { Id = GetStatusGuid(TaskItemStatus.New), DisplayName = await TranslateSafeAsync("Mới", "New") },
            new() { Id = GetStatusGuid(TaskItemStatus.InProgress), DisplayName = await TranslateSafeAsync("Đang thực hiện", "In Progress") },
            new() { Id = GetStatusGuid(TaskItemStatus.InReview), DisplayName = await TranslateSafeAsync("Chờ duyệt", "In Review") },
            new() { Id = GetStatusGuid(TaskItemStatus.Completed), DisplayName = await TranslateSafeAsync("Hoàn thành", "Completed") },
            new() { Id = GetStatusGuid(TaskItemStatus.Canceled), DisplayName = await TranslateSafeAsync("Đã hủy", "Canceled") }
        ];
    }

    private static Guid GetStatusGuid(TaskItemStatus status)
    {
        int val = (int)status;
        return new Guid(val, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
    #endregion

    #region Query & Sorting Filter
    public override async Task<PagedResultDto<TaskDto>> GetListAsync(GetTaskListInputDto input)
    {
        var currentUserId = CurrentUser.Id;

        var isManagerOrAdmin = CurrentUser.IsInRole("Manager") ||
                               CurrentUser.IsInRole("admin") ||
                               CurrentUser.IsInRole("Admin");

        bool effectiveOnlyMyTasks = input.OnlyMyTasks;
        Guid? effectiveAssigneeId = input.AssigneeId;

        if (!isManagerOrAdmin)
        {
            effectiveAssigneeId = currentUserId;
        }
        else if (effectiveOnlyMyTasks)
        {
            effectiveAssigneeId = currentUserId;
        }

        Guid? effectiveProjectId = (input.ProjectId.HasValue && input.ProjectId.Value != Guid.Empty)
            ? input.ProjectId
            : null;

        var request = new TaskManagement.Tasks.Request.TaskGetListRequest
        {
            Keyword = !string.IsNullOrWhiteSpace(input.Keyword) ? input.Keyword : input.Filter,
            CategoryId = input.CategoryId,
            ProjectId = effectiveProjectId,
            AssigneeId = effectiveAssigneeId,
            DepartmentId = input.DepartmentId,
            DepartmentIds = input.DepartmentIds,
            Priority = input.Priority.HasValue ? (int?)input.Priority.Value : null,
            Status = input.Status.HasValue ? (int?)input.Status.Value : null,
            OnlyMyTasks = effectiveOnlyMyTasks,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };

        var (items, totalCount) = await _taskProvider.GetListAsync(request, currentUserId);

        var dtos = new List<TaskDto>();
        foreach (var x in items)
        {
            dtos.Add(new TaskDto
            {
                Id = x.Id,
                Title = await GetLocalizedTitleAsync(x.Title, x.TitleEn),
                Description = await GetLocalizedDescriptionAsync(x.Description, x.DescriptionEn),
                TitleEn = x.TitleEn,
                DescriptionEn = x.DescriptionEn,
                Status = (TaskItemStatus)x.Status,
                Priority = (TaskPriority)x.Priority,
                ProgressPercent = x.ProgressPercent,
                DueDate = x.DueDate,
                ProjectId = x.ProjectId,
                CategoryId = x.CategoryId,
                AssigneeId = x.AssigneeId,
                DepartmentId = x.DepartmentId,
                CreationTime = x.CreationTime,
                CreatorId = x.CreatorId,
                FileName = x.FileName,
                FileUrl = x.FileUrl
            });
        }

        await EnrichTaskDtosAsync(dtos);

        return new PagedResultDto<TaskDto>(totalCount, dtos);
    }

    protected override async Task<IQueryable<TaskItem>> CreateFilteredQueryAsync(GetTaskListInputDto input)
    {
        var query = await Repository.GetQueryableAsync();
        var currentUserId = CurrentUser.Id;
        var searchKeyword = !string.IsNullOrWhiteSpace(input.Keyword) ? input.Keyword : input.Filter;

        var isManagerOrAdmin = CurrentUser.IsInRole("Manager") || CurrentUser.IsInRole("admin") || CurrentUser.IsInRole("Admin");

        return query
            .WhereIf(!isManagerOrAdmin && currentUserId.HasValue,
                x => x.AssigneeId == currentUserId.Value)
            .WhereIf(isManagerOrAdmin && input.OnlyMyTasks && currentUserId.HasValue,
                x => x.AssigneeId == currentUserId!.Value || x.CreatorId == currentUserId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(searchKeyword), x =>
                x.Title.Contains(searchKeyword!) || (x.Description != null && x.Description.Contains(searchKeyword!)))
            .WhereIf(input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty, x => x.CategoryId == input.CategoryId!.Value)
            .WhereIf(input.ProjectId.HasValue && input.ProjectId.Value != Guid.Empty, x => x.ProjectId == input.ProjectId!.Value)
            .WhereIf(input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty, x => x.AssigneeId == input.AssigneeId!.Value)
            .WhereIf(input.DepartmentIds != null && input.DepartmentIds.Count > 0,
                x => x.DepartmentId != null && input.DepartmentIds.Contains(x.DepartmentId.Value))
            .WhereIf((input.DepartmentIds == null || input.DepartmentIds.Count == 0) && input.DepartmentId.HasValue && input.DepartmentId.Value != Guid.Empty,
                x => x.DepartmentId == input.DepartmentId!.Value)
            .WhereIf(input.Priority.HasValue, x => x.Priority == input.Priority!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value);
    }

    protected override IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, GetTaskListInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            return query.OrderByDescending(x => x.CreationTime);
        }

        try
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
                _ => base.ApplySorting(query, input)
            };
        }
        catch
        {
            return query.OrderByDescending(x => x.CreationTime);
        }
    }
    #endregion

    #region Task Detail & CRUD
    [HttpGet("/api/app/task/{id}/detail")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<TaskDetailDto> GetTaskDetailAsync(Guid id)
    {
        var taskQuery = await Repository.WithDetailsAsync(x => x.Histories);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(taskQuery.Where(x => x.Id == id))
            ?? await Repository.FindAsync(id);

        if (entity == null)
        {
            throw new UserFriendlyException("Công việc này đã bị xóa hoặc không còn tồn tại trong hệ thống.");
        }

        var dto = ObjectMapper.Map<TaskItem, TaskDetailDto>(entity);
        dto.Title = await GetLocalizedTitleAsync(entity.Title, entity.TitleEn);
        dto.Description = await GetLocalizedDescriptionAsync(entity.Description, entity.DescriptionEn);
        dto.FileName = entity.FileName;
        dto.FileUrl = entity.FileUrl;

        try
        {
            var dbContext = await Repository.GetDbContextAsync();
            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync();
            }

            var attachmentsQuery = await dbConnection.QueryAsync<TaskAttachmentDto>(
                "SELECT FileName, FileUrl FROM TaskAttachments WHERE TaskId = @TaskId",
                new { TaskId = id }
            );

            dto.Attachments = attachmentsQuery.ToList();
        }
        catch (Exception)
        {
            dto.Attachments = [];
        }

        if (entity.ProjectId.HasValue)
        {
            var project = await _projectRepository.FindAsync(entity.ProjectId.Value);
            dto.ProjectName = project?.Name;
        }

        if (entity.Histories != null && entity.Histories.Count > 0)
        {
            dto.Histories = [.. entity.Histories
            .OrderByDescending(x => x.CreationTime)
            .Select(x => ObjectMapper.Map<TaskHistory, TaskHistoryDto>(x))];
        }

        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            try
            {
                var user = await _userRepository.FindAsync(entity.AssigneeId.Value, cancellationToken: CancellationToken.None);
                if (user != null)
                {
                    dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty);
                    dto.AssigneeUserName = user.UserName;
                }
            }
            catch (TaskCanceledException) { }
        }

        var subTasks = await _subTaskRepository.GetListAsync(x => x.TaskId == id);
        var subTaskDtos = ObjectMapper.Map<List<SubTask>, List<SubTaskDto>>(subTasks);
        foreach (var subDto in subTaskDtos)
        {
            var originalSubTask = subTasks.FirstOrDefault(s => s.Id == subDto.Id);
            if (originalSubTask != null)
            {
                ApplySubTaskLocalization(subDto, originalSubTask.TitleEn);
            }
        }

        var subTaskAssigneeIds = subTasks
            .Where(x => x.AssigneeId.HasValue && x.AssigneeId.Value != Guid.Empty)
            .Select(x => x.AssigneeId!.Value)
            .Distinct()
            .ToList();

        if (subTaskAssigneeIds.Count > 0)
        {
            try
            {
                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => subTaskAssigneeIds.Contains(x.Id)), cancellationToken: CancellationToken.None);
                var userDict = users.ToDictionary(u => u.Id);

                foreach (var subDto in subTaskDtos)
                {
                    if (subDto.AssigneeId.HasValue && userDict.TryGetValue(subDto.AssigneeId.Value, out var u))
                    {
                        subDto.AssigneeName = !string.IsNullOrWhiteSpace(u.Name) ? u.Name : (u.UserName ?? string.Empty);
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        dto.SubTasks = subTaskDtos;

        var checklists = await _checklistItemRepository.GetListAsync(x => x.TaskId == id);
        var checklistDtos = ObjectMapper.Map<List<TaskChecklistItem>, List<ChecklistItemDto>>(checklists);
        foreach (var checkDto in checklistDtos)
        {
            var originalItem = checklists.FirstOrDefault(c => c.Id == checkDto.Id);
            if (originalItem != null)
            {
                ApplyChecklistLocalization(checkDto, originalItem.TitleEn);
            }
        }
        dto.ChecklistItems = checklistDtos;

      
        // ==========================================
        // XỬ LÝ ACTIVITY LOGS & ÁP DỤNG ĐA NGÔN NGỮ CHI TIẾT
        // ==========================================
        var logs = await _activityLogRepository.GetListAsync(x => x.TaskId == id);
        var sortedLogs = logs.OrderByDescending(x => x.CreationTime).ToList();

        // 1. Lấy danh sách ID người tạo log để truy vấn tên
        var logCreatorIds = sortedLogs
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, IdentityUser> logUserDict = [];
        if (logCreatorIds.Count > 0)
        {
            try
            {
                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => logCreatorIds.Contains(x.Id)), cancellationToken: CancellationToken.None);
                logUserDict = users.ToDictionary(u => u.Id);
            }
            catch (TaskCanceledException) { }
        }

        var activityLogDtos = new List<TaskActivityLogDto>();
        foreach (var log in sortedLogs)
        {
            var logDto = ObjectMapper.Map<TaskActivityLog, TaskActivityLogDto>(log);

            // 2. Gán tên người tạo cho từng dòng log
            logDto.CreatorName = (log.CreatorId.HasValue && logUserDict.TryGetValue(log.CreatorId.Value, out var logUser) && logUser != null)
                ? (!string.IsNullOrWhiteSpace(logUser.Name) ? logUser.Name : (logUser.UserName ?? string.Empty))
                : "Hệ thống";

            // 3. Đa ngôn ngữ action
            ApplyActivityLogLocalization(logDto, log.ActionEn);
            activityLogDtos.Add(logDto);
        }
        dto.ActivityLogs = activityLogDtos;
        var commentQuery = await _commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == id));
        var sortedComments = comments.OrderByDescending(x => x.CreationTime).ToList();

        var creatorIds = sortedComments
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, IdentityUser> commentUserDict = [];
        if (creatorIds.Count > 0)
        {
            try
            {
                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => creatorIds.Contains(x.Id)), cancellationToken: CancellationToken.None);
                commentUserDict = users.ToDictionary(u => u.Id);
            }
            catch (TaskCanceledException) { }
        }

        var commentDtos = new List<TaskCommentDto>();
        foreach (var c in sortedComments)
        {
            var cDto = ObjectMapper.Map<TaskComment, TaskCommentDto>(c);
            cDto.CreatorName = (c.CreatorId.HasValue && commentUserDict.TryGetValue(c.CreatorId.Value, out var commentUser) && commentUser != null)
                ? (!string.IsNullOrWhiteSpace(commentUser.Name) ? commentUser.Name : (commentUser.UserName ?? string.Empty))
                : "Hệ thống";

            var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(c.Attachments?.ToList() ?? []);
            if (attachments.Count == 0)
            {
                attachments = ParseCommentAttachments(c.FileUrl, c.FileName);
            }
            cDto.Attachments = attachments;
            ApplyCommentLocalization(cDto, c.TextEn);

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
                dto.SubmissionNote = rawText[(rawText.IndexOf("[NỘP TRÌNH DUYỆT]:") + "[NỘP TRÌNH DUYỆT]:".Length)..].Trim();
            }
            else if (rawText.Contains("[NỘP TRÌNH DUYỆT]"))
            {
                dto.SubmissionNote = rawText[(rawText.IndexOf("[NỘP TRÌNH DUYỆT]") + "[NỘP TRÌNH DUYỆT]".Length)..].Trim();
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

            dto.SubmissionFiles = [.. submissionAttachments.Select(a => new TaskFileDto
        {
            FileName = a.FileName,
            FileUrl = a.FileUrl
        })];
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

        var logs = await _activityLogRepository.GetListAsync(x => x.TaskId == taskId);
        var sortedLogs = logs.OrderByDescending(x => x.CreationTime).ToList();

        var creatorIds = sortedLogs
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, IdentityUser> userDict = [];
        if (creatorIds.Count > 0)
        {
            try
            {
                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => creatorIds.Contains(x.Id)), cancellationToken: CancellationToken.None);
                userDict = users.ToDictionary(u => u.Id);
            }
            catch (TaskCanceledException) { }
        }

        var dtos = new List<TaskActivityLogDto>();
        foreach (var log in sortedLogs)
        {
            var dto = ObjectMapper.Map<TaskActivityLog, TaskActivityLogDto>(log);
            dto.CreatorName = (log.CreatorId.HasValue && userDict.TryGetValue(log.CreatorId.Value, out var timelineUser) && timelineUser != null)
                ? (!string.IsNullOrWhiteSpace(timelineUser.Name) ? timelineUser.Name : (timelineUser.UserName ?? string.Empty))
                : "Hệ thống";

            // Áp dụng dịch ngôn ngữ cho từng item trong timeline
            ApplyActivityLogLocalization(dto, log.ActionEn);

            dtos.Add(dto);
        }

        return dtos;
    }

    [UnitOfWork]
    public override async Task DeleteAsync(Guid id)
    {
        var task = await Repository.FindAsync(id);
        if (task == null)
        {
            throw new UserFriendlyException("Không tìm thấy công việc cần xóa.");
        }

        var taskTitle = task.Title;

        if (task.MilestoneId.HasValue && task.MilestoneId.Value != Guid.Empty)
        {
            var milestoneId = task.MilestoneId.Value;
            await _milestoneRepository.DeleteAsync(milestoneId);
        }

        await LogActivityAsync(id, $"Đã xóa công việc: '{taskTitle}'");
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{taskTitle}' đã bị xóa khỏi hệ thống.");

        await base.DeleteAsync(id);
    }

    [UnitOfWork]
    public override async Task<TaskDto> CreateAsync(CreateTaskInputDto input)
    {
        if (input.ProjectId.HasValue && input.ProjectId.Value != Guid.Empty && input.DueDate.HasValue)
        {
            var project = await _projectRepository.FindAsync(input.ProjectId.Value);
            if (project != null && project.EndDate.HasValue)
            {
                if (input.DueDate.Value.Date > project.EndDate.Value.Date.AddDays(7))
                {
                    throw new UserFriendlyException($"Hạn chót của công việc không được lớn hơn quá 7 ngày so với ngày kết thúc của dự án ({project.EndDate.Value:dd/MM/yyyy})!");
                }
            }
        }

        if ((!input.AssigneeId.HasValue || input.AssigneeId.Value == Guid.Empty) && input.DepartmentId.HasValue && input.DepartmentId.Value != Guid.Empty)
        {
            var defaultUserInDept = await GetDefaultUserForDepartmentAsync(input.DepartmentId.Value);
            if (defaultUserInDept.HasValue)
            {
                input.AssigneeId = defaultUserInDept;
            }
        }

        var entity = await MapToEntityAsync(input);
        entity.ProgressPercent = CalculateProgressByStatus(entity.Status, entity.ProgressPercent);

        await ProcessTaskAttachmentsAsync(input.Attachments, entity);

        await Repository.InsertAsync(entity, autoSave: true);
        await LogActivityAsync(entity.Id, $"Đã tạo công việc: '{entity.Title}' (Tiến độ: {entity.ProgressPercent}%)");
        await NotifyTaskStakeholdersAsync(entity, $"Công việc mới đã được tạo: '{entity.Title}'");

        return await MapToGetOutputDtoAsync(entity);
    }

    [UnitOfWork]
    public override async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskInputDto input)
    {
        var entity = await GetEntityByIdAsync(id);
        var oldStatus = entity.Status;

        var targetProjectId = input.ProjectId ?? entity.ProjectId;
        var targetDueDate = input.DueDate ?? entity.DueDate;

        if (targetProjectId.HasValue && targetProjectId.Value != Guid.Empty && targetDueDate.HasValue)
        {
            var project = await _projectRepository.FindAsync(targetProjectId.Value);
            if (project != null && project.EndDate.HasValue)
            {
                if (targetDueDate.Value.Date > project.EndDate.Value.Date.AddDays(7))
                {
                    throw new UserFriendlyException($"Hạn chót của công việc không được lớn hơn quá 7 ngày so với ngày kết thúc của dự án ({project.EndDate.Value:dd/MM/yyyy})!");
                }
            }
        }

        if ((!input.AssigneeId.HasValue || input.AssigneeId.Value == Guid.Empty) && (!entity.AssigneeId.HasValue || entity.AssigneeId.Value == Guid.Empty))
        {
            var targetDeptId = input.DepartmentId ?? entity.DepartmentId;
            if (targetDeptId.HasValue && targetDeptId.Value != Guid.Empty)
            {
                var defaultUserInDept = await GetDefaultUserForDepartmentAsync(targetDeptId.Value);
                if (defaultUserInDept.HasValue)
                {
                    input.AssigneeId = defaultUserInDept;
                }
            }
        }

        var oldFileUrl = entity.FileUrl;
        var oldFileName = entity.FileName;

        await MapToEntityAsync(input, entity);

        if (input.Attachments == null)
        {
            entity.FileUrl = oldFileUrl;
            entity.FileName = oldFileName;
        }
        else
        {
            var newFilesWithContent = input.Attachments.Where(f => !string.IsNullOrEmpty(f.FileContent)).ToList();
            if (newFilesWithContent.Count > 0)
            {
                await ProcessTaskAttachmentsAsync(newFilesWithContent, entity);
            }

            var incomingFileUrls = input.Attachments
                .Where(f => !string.IsNullOrEmpty(f.FileUrl))
                .Select(f => f.FileUrl)
                .Distinct()
                .ToList();

            var incomingFileNames = input.Attachments
                .Where(f => !string.IsNullOrEmpty(f.FileName))
                .Select(f => f.FileName)
                .ToList();

            if (incomingFileUrls.Count > 0)
            {
                entity.FileUrl = string.Join(";", incomingFileUrls);
                entity.FileName = incomingFileNames.Count > 0 ? string.Join(";", incomingFileNames) : oldFileName;
            }
            else if (input.Attachments.Count == 0)
            {
                entity.FileUrl = null;
                entity.FileName = null;
            }

            var dbContext = await Repository.GetDbContextAsync();
            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync();
            }
            var currentTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();

            if (incomingFileUrls.Count > 0)
            {
                await dbConnection.ExecuteAsync(
                    "DELETE FROM TaskAttachments WHERE TaskId = @TaskId AND FileUrl NOT IN @FileUrls",
                    new { TaskId = id, FileUrls = incomingFileUrls },
                    currentTransaction
                );
            }
            else
            {
                await dbConnection.ExecuteAsync(
                    "DELETE FROM TaskAttachments WHERE TaskId = @TaskId",
                    new { TaskId = id },
                    currentTransaction
                );
            }

            foreach (var file in input.Attachments)
            {
                if (!string.IsNullOrEmpty(file.FileUrl))
                {
                    var countExist = await dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM TaskAttachments WHERE TaskId = @TaskId AND FileUrl = @FileUrl",
                        new { TaskId = id, FileUrl = file.FileUrl },
                        currentTransaction
                    );

                    if (countExist == 0)
                    {
                        await dbConnection.ExecuteAsync(
                            "INSERT INTO TaskAttachments (Id, TaskId, FileName, FileUrl, FilePath) VALUES (@AttachmentId, @TaskId, @FileName, @FileUrl, @FilePath)",
                            new
                            {
                                AttachmentId = Guid.NewGuid(),
                                TaskId = id,
                                FileName = file.FileName,
                                FileUrl = file.FileUrl,
                                FilePath = file.FileUrl
                            },
                            currentTransaction
                        );
                    }
                }
            }
        }

        entity.ProgressPercent = CalculateProgressByStatus(entity.Status, entity.ProgressPercent);

        await Repository.UpdateAsync(entity, autoSave: true);

        if (entity.Status == TaskItemStatus.Completed && oldStatus != TaskItemStatus.Completed)
        {
            await GenerateNextRecurringTaskIfNeededAsync(entity);
        }

        await LogActivityAsync(entity.Id, $"Đã cập nhật thông tin công việc (Tiến độ: {entity.ProgressPercent}%)");
        await NotifyTaskStakeholdersAsync(entity, $"Công việc '{entity.Title}' vừa được cập nhật thông tin.");

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

        if (status == TaskItemStatus.Completed && oldStatus != TaskItemStatus.Completed)
        {
            await GenerateNextRecurringTaskIfNeededAsync(entity);
        }

        await LogActivityAsync(id, $"Thay đổi trạng thái từ '{oldStatus}' sang '{status}' (Tiến độ: {entity.ProgressPercent}%)");
        await NotifyTaskStakeholdersAsync(entity, $"Công việc '{entity.Title}' đã chuyển sang trạng thái: {status}");

        return await MapToGetOutputDtoAsync(entity);
    }

    [HttpPut("/api/app/task/{id}/schedule")]
    [HttpPost("/api/app/task/{id}/schedule")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDto> UpdateScheduleAsync(Guid id, [FromBody] UpdateTaskScheduleDto input)
    {
        var entity = await GetEntityByIdAsync(id);

        if (entity.ProjectId.HasValue && entity.ProjectId.Value != Guid.Empty && input.DueDate.HasValue)
        {
            var project = await _projectRepository.FindAsync(entity.ProjectId.Value);
            if (project != null && project.EndDate.HasValue && input.DueDate.Value.Date > project.EndDate.Value.Date)
            {
                throw new UserFriendlyException($"Hạn chót của công việc không được lớn hơn ngày kết thúc của dự án ({project.EndDate.Value:dd/MM/yyyy})!");
            }
        }

        entity.DueDate = input.DueDate;

        await Repository.UpdateAsync(entity, autoSave: true);
        await LogActivityAsync(id, "Đã cập nhật lại hạn chót công việc qua lịch");
        await NotifyTaskStakeholdersAsync(entity, $"Hạn chót của công việc '{entity.Title}' đã được cập nhật lại.");

        return await MapToGetOutputDtoAsync(entity);
    }

    [HttpPost("/api/app/task/{id}/assignee")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDto> UpdateAssigneeAsync(Guid id, [FromQuery] Guid? assigneeId)
    {
        var entity = await GetEntityByIdAsync(id);

        string assigneeName = "Chưa phân công";
        Guid? newAssigneeId = assigneeId.HasValue && assigneeId.Value != Guid.Empty ? assigneeId : null;

        if (newAssigneeId.HasValue)
        {
            try
            {
                var user = await _userRepository.FindAsync(newAssigneeId.Value, cancellationToken: CancellationToken.None);
                assigneeName = !string.IsNullOrWhiteSpace(user?.Name) ? user.Name : (user?.UserName ?? newAssigneeId.Value.ToString());
            }
            catch (TaskCanceledException) { }
        }

        entity.UpdateAssignee(newAssigneeId, assigneeName);

        await Repository.UpdateAsync(entity, autoSave: true);
        if (entity.MilestoneId.HasValue && entity.MilestoneId.Value != Guid.Empty)
        {
            var milestone = await _milestoneRepository.FindAsync(entity.MilestoneId.Value);
            if (milestone != null)
            {
                milestone.AssigneeUserId = newAssigneeId;
                await _milestoneRepository.UpdateAsync(milestone, autoSave: true);
            }
        }
        await LogActivityAsync(id, $"Giao công việc cho: {assigneeName}");
        await NotifyTaskStakeholdersAsync(entity, $"Công việc '{entity.Title}' đã được phân công lại cho: {assigneeName}");

        return await MapToGetOutputDtoAsync(entity);
    }

    [HttpDelete("/api/app/task/{id}/attachment")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDto> DeleteTaskAttachmentAsync(Guid id, [FromQuery] string fileUrl)
    {
        var entity = await GetEntityByIdAsync(id);

        var dbContext = await Repository.GetDbContextAsync();
        var dbConnection = dbContext.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }
        var currentTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();

        await dbConnection.ExecuteAsync(
            "DELETE FROM TaskAttachments WHERE TaskId = @TaskId AND FileUrl = @FileUrl",
            new { TaskId = id, FileUrl = fileUrl },
            currentTransaction
        );

        if (!string.IsNullOrWhiteSpace(entity.FileUrl))
        {
            var urls = entity.FileUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            var names = !string.IsNullOrEmpty(entity.FileName)
                ? entity.FileName.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                : [];

            var index = urls.IndexOf(fileUrl);
            if (index >= 0)
            {
                urls.RemoveAt(index);
                if (index < names.Count)
                {
                    names.RemoveAt(index);
                }
            }

            entity.FileUrl = urls.Count > 0 ? string.Join(";", urls) : null;
            entity.FileName = names.Count > 0 ? string.Join(";", names) : null;

            await Repository.UpdateAsync(entity, autoSave: true);
        }

        await LogActivityAsync(id, "Đã xóa một tệp đính kèm khỏi công việc");
        await NotifyTaskStakeholdersAsync(entity, $"Công việc '{entity.Title}' đã bị xóa một tệp đính kèm.");

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
            FileContent = a.FileContent,
            FileUrl = a.FileUrl
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
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' đã được nộp để chờ phê duyệt.");

        return await GetTaskDetailAsync(id);
    }

    [HttpPut("/api/app/task/{id}/submission")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> UpdateSubmissionAsync(Guid id, [FromBody] SubmitReviewInputDto input)
    {
        var dbContextProvider = LazyServiceProvider.GetRequiredService<IDbContextProvider<TaskManagementDbContext>>();
        var dbContext = await dbContextProvider.GetDbContextAsync();

        var targetComment = dbContext.Set<TaskComment>()
            .Where(x => x.TaskId == id && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (targetComment == null)
        {
            throw new UserFriendlyException("Không tìm thấy thông tin nộp bài duyệt cần chỉnh sửa.");
        }

        if (targetComment.CreatorId != CurrentUser.Id)
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa mục nộp bài này!");
        }

        var noteContent = input?.Note?.Trim() ?? string.Empty;
        var newText = string.IsNullOrWhiteSpace(noteContent) ? "[NỘP TRÌNH DUYỆT]" : $"[NỘP TRÌNH DUYỆT]: {noteContent}";

        var newFileUrls = new List<string>();
        var newFileNames = new List<string>();

        if (input?.Attachments != null)
        {
            foreach (var a in input.Attachments)
            {
                var fileUrl = a.FileUrl;
                var cleanFileName = !string.IsNullOrEmpty(a.FileName) ? Path.GetFileName(a.FileName) : "Attachment";

                if (!string.IsNullOrEmpty(a.FileContent) && !string.IsNullOrEmpty(a.FileName))
                {
                    fileUrl = await SaveBase64FileAsync(cleanFileName, a.FileContent);
                }

                if (!string.IsNullOrEmpty(fileUrl))
                {
                    newFileUrls.Add(fileUrl);
                    newFileNames.Add(cleanFileName);
                }
            }
        }

        var finalFileUrl = newFileUrls.Count > 0 ? string.Join(";", newFileUrls) : null;
        var finalFileName = newFileNames.Count > 0 ? string.Join(";", newFileNames) : null;

        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM TaskCommentAttachments WHERE TaskCommentId = {0}", targetComment.Id);

        for (int i = 0; i < newFileUrls.Count; i++)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO TaskCommentAttachments (Id, TaskCommentId, FileName, FileUrl) VALUES ({0}, {1}, {2}, {3})",
                Guid.NewGuid(), targetComment.Id, newFileNames[i], newFileUrls[i]);
        }

        targetComment.Text = newText;
        targetComment.FileUrl = finalFileUrl;
        targetComment.FileName = finalFileName;

        var entry = dbContext.Entry(targetComment);
        entry.State = EntityState.Modified;
        if (entry.Metadata.FindProperty("ConcurrencyStamp") != null)
        {
            entry.Property("ConcurrencyStamp").IsModified = false;
        }

        await dbContext.SaveChangesAsync();

        await LogActivityAsync(id, "Đã cập nhật lại nội dung nộp bài duyệt");

        var task = await Repository.GetAsync(id);
        await NotifyTaskStakeholdersAsync(task, $"Nội dung nộp duyệt của công việc '{task.Title}' vừa được cập nhật.");

        return await GetTaskDetailAsync(id);
    }

    [HttpDelete("/api/app/task/{id}/submission")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<TaskDetailDto> DeleteSubmissionAsync(Guid id)
    {
        var query = await _commentRepository.WithDetailsAsync(x => x.Attachments);

        var lastSubmissionComment = query
            .Where(x => x.TaskId == id && !string.IsNullOrEmpty(x.Text) && x.Text.Contains("[NỘP TRÌNH DUYỆT]"))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (lastSubmissionComment == null)
        {
            throw new UserFriendlyException("Không tìm thấy thông tin nộp bài duyệt để xóa.");
        }

        if (lastSubmissionComment.CreatorId != CurrentUser.Id)
        {
            throw new UserFriendlyException("Bạn không có quyền xóa mục nộp bài này!");
        }

        await _commentRepository.DeleteAsync(lastSubmissionComment, autoSave: true);

        var task = await Repository.GetAsync(id);
        if (task.Status == TaskItemStatus.InReview)
        {
            task.Status = TaskItemStatus.InProgress;
            await Repository.UpdateAsync(task, autoSave: true);
        }

        await LogActivityAsync(id, "Đã hủy/xóa lượt nộp bài duyệt");
        await NotifyTaskStakeholdersAsync(task, $"Lượt nộp duyệt của công việc '{task.Title}' đã bị hủy.");

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

        await GenerateNextRecurringTaskIfNeededAsync(task);

        await LogActivityAsync(id, "Đã phê duyệt công việc (Đã hoàn thành)");
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' đã được phê duyệt hoàn thành.");

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
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' bị từ chối duyệt. Lý do: {input.Reason}");

        return await GetTaskDetailAsync(id);
    }
    #endregion

    #region SubTask Management
    private void ApplySubTaskLocalization(SubTaskDto dto, string? titleEn)
    {
        if (IsCurrentCultureEnglish() && !string.IsNullOrEmpty(titleEn))
        {
            dto.Title = titleEn;
        }
    }

    [HttpPost("/api/app/task/{taskId}/sub-task")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> CreateSubTaskAsync(Guid taskId, [FromBody] CreateUpdateSubTaskDto input)
    {
        var task = await Repository.GetAsync(taskId);
        if (task == null) throw new UserFriendlyException("Công việc gốc không tồn tại.");

        var title = input.Title?.Trim() ?? string.Empty;
        string titleEn = string.Empty;

        if (!string.IsNullOrWhiteSpace(title))
        {
            titleEn = await TranslateToEnglishOrDefaultAsync(title);
        }

        var subTask = new SubTask(GuidGenerator.Create())
        {
            TaskId = taskId,
            Title = title,
            TitleEn = titleEn,
            AssigneeId = input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty ? input.AssigneeId : null,
            IsCompleted = false
        };

        await _subTaskRepository.InsertAsync(subTask, autoSave: true);
        await LogActivityAsync(taskId, $"Đã thêm công việc con: '{title}'");
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' có thêm công việc con mới: '{title}'");

        var dto = ObjectMapper.Map<SubTask, SubTaskDto>(subTask);
        ApplySubTaskLocalization(dto, subTask.TitleEn);

        return dto;
    }

    [HttpPut("/api/app/task/sub-task/{subTaskId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> UpdateSubTaskAsync(Guid subTaskId, [FromBody] CreateUpdateSubTaskDto input)
    {
        var subTask = await _subTaskRepository.GetAsync(subTaskId);
        var title = input.Title?.Trim() ?? string.Empty;

        subTask.Title = title;

        if (!string.IsNullOrWhiteSpace(title))
        {
            subTask.TitleEn = await TranslateToEnglishOrDefaultAsync(title);
        }
        else
        {
            subTask.TitleEn = null;
        }

        subTask.AssigneeId = input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty ? input.AssigneeId : null;

        await _subTaskRepository.UpdateAsync(subTask, autoSave: true);
        await LogActivityAsync(subTask.TaskId, $"Đã cập nhật công việc con: '{title}'");

        var task = await Repository.GetAsync(subTask.TaskId);
        if (task != null)
        {
            await NotifyTaskStakeholdersAsync(task, $"Công việc con của '{task.Title}' vừa được cập nhật.");
        }

        var dto = ObjectMapper.Map<SubTask, SubTaskDto>(subTask);
        ApplySubTaskLocalization(dto, subTask.TitleEn);

        return dto;
    }

    [HttpPut("/api/app/task/sub-task/{subTaskId}/toggle")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<SubTaskDto> ToggleSubTaskStatusAsync(Guid subTaskId)
    {
        var subTask = await _subTaskRepository.GetAsync(subTaskId);
        subTask.IsCompleted = !subTask.IsCompleted;

        await _subTaskRepository.UpdateAsync(subTask, autoSave: true);
        await LogActivityAsync(subTask.TaskId, $"Đã cập nhật công việc con '{subTask.Title}' sang {(subTask.IsCompleted ? "Hoàn thành" : "Đang làm")}");

        var task = await Repository.GetAsync(subTask.TaskId);
        if (task != null)
        {
            await NotifyTaskStakeholdersAsync(task, $"Trạng thái công việc con '{subTask.Title}' trong '{task.Title}' đã thay đổi.");
        }

        var dto = ObjectMapper.Map<SubTask, SubTaskDto>(subTask);
        ApplySubTaskLocalization(dto, subTask.TitleEn);

        return dto;
    }

    [HttpDelete("/api/app/task/sub-task/{subTaskId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task DeleteSubTaskAsync(Guid subTaskId)
    {
        var subTask = await _subTaskRepository.FindAsync(subTaskId);
        if (subTask != null)
        {
            var taskId = subTask.TaskId;
            var subTaskTitle = subTask.Title;

            await _subTaskRepository.DeleteAsync(subTaskId);
            await LogActivityAsync(taskId, $"Đã xóa công việc phụ: '{subTaskTitle}'");

            var task = await Repository.GetAsync(taskId);
            if (task != null)
            {
                await NotifyTaskStakeholdersAsync(task, $"Công việc con '{subTaskTitle}' trong '{task.Title}' đã bị xóa.");
            }
        }
    }
    #endregion

    #region Checklist Management
    private void ApplyChecklistLocalization(ChecklistItemDto dto, string? titleEn)
    {
        if (IsCurrentCultureEnglish() && !string.IsNullOrEmpty(titleEn))
        {
            dto.Title = titleEn;
        }
    }

    [HttpPost("/api/app/task/{taskId}/checklist-item")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<ChecklistItemDto> CreateChecklistItemAsync(Guid taskId, [FromBody] CreateUpdateChecklistItemDto input)
    {
        var task = await Repository.GetAsync(taskId);
        if (task == null) throw new UserFriendlyException("Công việc gốc không tồn tại.");

        var dbContextProvider = LazyServiceProvider.GetRequiredService<IDbContextProvider<TaskManagementDbContext>>();
        var dbContext = await dbContextProvider.GetDbContextAsync();

        var newItemId = GuidGenerator.Create();
        var title = input.Title?.Trim() ?? string.Empty;

        string titleEn = title;
        if (!string.IsNullOrWhiteSpace(title))
        {
            titleEn = await TranslateToEnglishOrDefaultAsync(title);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO TaskChecklistItems (Id, TaskId, Title, TitleEn, IsDone, CreationTime, CreatorId) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
            newItemId, taskId, title, titleEn, false, DateTime.Now, CurrentUser.Id);
        var displayTitle = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(titleEn) ? titleEn : title;
        await LogActivityAsync(taskId, $"Đã thêm hạng mục kiểm tra: '{displayTitle}'");
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' có thêm mục kiểm tra mới: '{displayTitle}'.");

        var dto = new ChecklistItemDto
        {
            Id = newItemId,
            TaskId = taskId,
            Title = title,
            TitleEn = titleEn,
            IsDone = false
        };

        ApplyChecklistLocalization(dto, titleEn);

        return dto;
    }

    [HttpPut("/api/app/task/checklist-item/{itemId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, [FromBody] CreateUpdateChecklistItemDto input)
    {
        var item = await _checklistItemRepository.GetAsync(itemId);
        var title = input.Title?.Trim() ?? string.Empty;

        item.Title = title;

        if (!string.IsNullOrWhiteSpace(title))
        {
            item.TitleEn = await TranslateToEnglishOrDefaultAsync(title);
        }
        else
        {
            item.TitleEn = null;
        }

        await _checklistItemRepository.UpdateAsync(item, autoSave: true);
        var displayTitle = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(item.TitleEn) ? item.TitleEn : title;

        await LogActivityAsync(item.TaskId, $"Đã cập nhật mục kiểm tra: '{displayTitle}'");

        var task = await Repository.GetAsync(item.TaskId);
        if (task != null)
        {
            await NotifyTaskStakeholdersAsync(task, $"Mục kiểm tra trong công việc '{task.Title}' vừa được cập nhật: '{displayTitle}'.");
        }

        var dto = ObjectMapper.Map<TaskChecklistItem, ChecklistItemDto>(item);
        ApplyChecklistLocalization(dto, item.TitleEn);

        return dto;
    }

    [HttpPut("/api/app/task/checklist-item/{itemId}/toggle")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task ToggleChecklistItemStatusAsync(Guid itemId)
    {
        var item = await _checklistItemRepository.GetAsync(itemId);
        item.IsDone = !item.IsDone;

        await _checklistItemRepository.UpdateAsync(item, autoSave: true);
        var displayTitle = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(item.TitleEn) ? item.TitleEn : item.Title;

        await LogActivityAsync(item.TaskId, $"Đã cập nhật trạng thái mục kiểm tra '{displayTitle}' sang {(item.IsDone ? "Hoàn thành" : "Chưa hoàn thành")}");

        var task = await Repository.GetAsync(item.TaskId);
        if (task != null)
        {
            await NotifyTaskStakeholdersAsync(task, $"Trạng thái mục kiểm tra '{displayTitle}' của công việc '{task.Title}' đã thay đổi.");
        }
    }

    [HttpDelete("/api/app/task/checklist-item/{itemId}")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    [UnitOfWork]
    public async Task DeleteChecklistItemAsync(Guid itemId)
    {
        var item = await _checklistItemRepository.FindAsync(itemId);
        if (item != null)
        {
            var taskId = item.TaskId;
            var displayTitle = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(item.TitleEn) ? item.TitleEn : item.Title;

            await _checklistItemRepository.DeleteAsync(itemId);
            await LogActivityAsync(taskId, $"Đã xóa mục kiểm tra: '{displayTitle}'");

            var task = await Repository.GetAsync(taskId);
            if (task != null)
            {
                await NotifyTaskStakeholdersAsync(task, $"Mục kiểm tra '{displayTitle}' của công việc '{task.Title}' đã bị xóa.");
            }
        }
    }
    #endregion

    #region Comments Management
    private bool IsCurrentCultureEnglish()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers != null &&
            httpContext.Request.Headers.TryGetValue("Accept-Language", out var acceptLangValues))
        {
            var acceptLanguage = acceptLangValues.ToString();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                return acceptLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            }
        }

        var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return currentLang.Equals("en", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyCommentLocalization(TaskCommentDto dto, string? textEn)
    {
        if (IsCurrentCultureEnglish() && !string.IsNullOrEmpty(textEn))
        {
            dto.Text = textEn;
        }
    }
    [HttpGet("/api/app/task/{taskId}/comments")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    public async Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId)
    {
        var commentQuery = await _commentRepository.WithDetailsAsync(x => x.Attachments);
        var comments = await AsyncExecuter.ToListAsync(commentQuery.Where(x => x.TaskId == taskId));
        var sortedComments = comments.OrderByDescending(x => x.CreationTime).ToList();

        var creatorIds = sortedComments
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, IdentityUser> userDict = [];
        if (creatorIds.Count > 0)
        {
            try
            {
                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => creatorIds.Contains(x.Id)), cancellationToken: CancellationToken.None);
                userDict = users.ToDictionary(u => u.Id);
            }
            catch (TaskCanceledException) { }
        }

        var dtos = new List<TaskCommentDto>();
        foreach (var c in sortedComments)
        {
            var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(c);
            dto.CreatorName = (c.CreatorId.HasValue && userDict.TryGetValue(c.CreatorId.Value, out var commentUser) && commentUser != null)
                ? (!string.IsNullOrWhiteSpace(commentUser.Name) ? commentUser.Name : (commentUser.UserName ?? string.Empty))
                : "Hệ thống";

            var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(c.Attachments?.ToList() ?? []);
            if (attachments.Count == 0)
            {
                attachments = ParseCommentAttachments(c.FileUrl, c.FileName);
            }
            dto.Attachments = attachments;

            ApplyCommentLocalization(dto, c.TextEn);

            dtos.Add(dto);
        }

        return dtos;
    }

    [HttpPost("/api/app/task/{taskId}/comment")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task<TaskCommentDto> CreateCommentAsync(Guid taskId, [FromBody] CreateTaskCommentDto input)
    {
        var task = await Repository.GetAsync(taskId);
        if (task == null) throw new UserFriendlyException("Công việc không tồn tại.");

        if (string.IsNullOrWhiteSpace(input.Text) && (input.Attachments == null || input.Attachments.Count == 0))
            throw new UserFriendlyException("Nội dung bình luận hoặc tệp đính kèm không được để trống.");

        var commentText = input.Text?.Trim() ?? string.Empty;

        var comment = new TaskComment(GuidGenerator.Create())
        {
            TaskId = taskId,
            Text = commentText,
            CreatorId = CurrentUser.Id
        };

        if (!string.IsNullOrWhiteSpace(commentText))
        {
            comment.TextEn = await TranslateToEnglishOrDefaultAsync(commentText);
        }

        await ProcessCommentAttachmentsAsync(input.Attachments, comment);

        if (comment.Attachments != null && comment.Attachments.Count > 0)
        {
            comment.FileUrl = string.Join(";", comment.Attachments.Select(a => a.FileUrl));
            comment.FileName = string.Join(";", comment.Attachments.Select(a => a.FileName));
        }

        var insertedComment = await _commentRepository.InsertAsync(comment, autoSave: true);

        var displayText = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(insertedComment.TextEn) ? insertedComment.TextEn : commentText;
        await LogActivityAsync(taskId, $"Đã thêm bình luận mới{(comment.Attachments?.Count > 0 || !string.IsNullOrEmpty(comment.FileName) ? " (kèm tệp đính kèm)" : "")}");
        await NotifyTaskStakeholdersAsync(task, $"Công việc '{task.Title}' có bình luận mới.");

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(insertedComment);
        dto.Id = insertedComment.Id;
        dto.TaskId = taskId;
        dto.CreatorId = CurrentUser.Id;
        dto.CreationTime = insertedComment.CreationTime;
        dto.CreatorName = !string.IsNullOrWhiteSpace(CurrentUser.Name) ? CurrentUser.Name : (CurrentUser.UserName ?? "Hệ thống");

        if (insertedComment.Attachments != null && insertedComment.Attachments.Count > 0)
        {
            dto.Attachments = [.. insertedComment.Attachments.Select(a => new CommentAttachmentDto
        {
            FileName = a.FileName,
            FileUrl = a.FileUrl
        })];
        }
        else
        {
            dto.Attachments = ParseCommentAttachments(insertedComment.FileUrl, insertedComment.FileName);
        }

        ApplyCommentLocalization(dto, insertedComment.TextEn);

        return dto;
    }

    [HttpPut("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task<TaskCommentDto> UpdateCommentAsync(Guid commentId, [FromBody] UpdateTaskCommentDto input)
    {
        var commentQuery = await _commentRepository.WithDetailsAsync(x => x.Attachments);
        var comment = await AsyncExecuter.FirstOrDefaultAsync(commentQuery.Where(x => x.Id == commentId))
            ?? throw new UserFriendlyException("Không tìm thấy bình luận.");

        if (comment.CreatorId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Edit))
        {
            throw new UserFriendlyException("Bạn không có quyền chỉnh sửa bình luận này.");
        }

        var newText = input.Text?.Trim() ?? string.Empty;
        comment.Text = newText;

        if (!string.IsNullOrWhiteSpace(newText))
        {
            comment.TextEn = await TranslateToEnglishOrDefaultAsync(newText);
        }
        else
        {
            comment.TextEn = null;
        }

        await _commentRepository.UpdateAsync(comment, autoSave: true);

        var displayText = IsCurrentCultureEnglish() && !string.IsNullOrEmpty(comment.TextEn) ? comment.TextEn : newText;
        await LogActivityAsync(comment.TaskId, "Đã chỉnh sửa bình luận");

        var task = await Repository.GetAsync(comment.TaskId);
        if (task != null)
        {
            await NotifyTaskStakeholdersAsync(task, $"Bình luận trong công việc '{task.Title}' vừa được chỉnh sửa.");
        }

        var dto = ObjectMapper.Map<TaskComment, TaskCommentDto>(comment);
        dto.CreatorName = !string.IsNullOrWhiteSpace(CurrentUser.Name) ? CurrentUser.Name : (CurrentUser.UserName ?? "Hệ thống");

        var attachments = ObjectMapper.Map<List<CommentAttachment>, List<CommentAttachmentDto>>(comment.Attachments?.ToList() ?? []);
        if (attachments.Count == 0)
        {
            attachments = ParseCommentAttachments(comment.FileUrl, comment.FileName);
        }
        dto.Attachments = attachments;

        ApplyCommentLocalization(dto, comment.TextEn);

        return dto;
    }

    [HttpDelete("/api/app/task/comment/{commentId}")]
    [Authorize(TaskManagementPermissions.Tasks.Default)]
    [UnitOfWork]
    public async Task DeleteCommentAsync(Guid commentId)
    {
        var comment = await _commentRepository.FindAsync(commentId);
        if (comment != null)
        {
            if (comment.CreatorId != CurrentUser.Id && !await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Delete))
            {
                throw new UserFriendlyException("Bạn không có quyền xóa bình luận này.");
            }

            var taskId = comment.TaskId;
            await _commentRepository.DeleteAsync(commentId);
            await LogActivityAsync(taskId, "Đã xóa một bình luận");

            var task = await Repository.GetAsync(taskId);
            if (task != null)
            {
                await NotifyTaskStakeholdersAsync(task, $"Một bình luận trong công việc '{task.Title}' đã bị xóa.");
            }
        }
    }
    #endregion

    #region Helper Methods
    protected override async Task<TaskItem> GetEntityByIdAsync(Guid id)
    {
        return await Repository.GetAsync(id);
    }

    private async Task<string> GetLocalizedTitleAsync(string? titleVi, string? titleEn)
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(titleEn))
            {
                return titleEn;
            }

            if (!string.IsNullOrWhiteSpace(titleVi))
            {
                try
                {
                    var translation = await _translator.TranslateAsync(titleVi, "vi", "en");
                    if (translation != null && !string.IsNullOrEmpty(translation.Translation))
                    {
                        return translation.Translation;
                    }
                }
                catch
                {
                }
                return titleVi;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(titleVi))
            {
                return titleVi;
            }

            if (!string.IsNullOrWhiteSpace(titleEn))
            {
                try
                {
                    var translation = await _translator.TranslateAsync(titleEn, "en", "vi");
                    if (translation != null && !string.IsNullOrEmpty(translation.Translation))
                    {
                        return translation.Translation;
                    }
                }
                catch
                {
                }
                return titleEn;
            }
        }

        return titleVi ?? titleEn ?? string.Empty;
    }

    private async Task<string?> GetLocalizedDescriptionAsync(string? descVi, string? descEn)
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(descEn))
            {
                return descEn;
            }

            if (!string.IsNullOrWhiteSpace(descVi))
            {
                try
                {
                    var translation = await _translator.TranslateAsync(descVi, "vi", "en");
                    if (translation != null && !string.IsNullOrEmpty(translation.Translation))
                    {
                        return translation.Translation;
                    }
                }
                catch
                {
                }
                return descVi;
            }
        }

        return descVi ?? descEn;
    }

    private async Task<Guid?> GetDefaultUserForDepartmentAsync(Guid departmentId)
    {
        try
        {
            var userQuery = await _userRepository.GetQueryableAsync();
            var user = await AsyncExecuter.FirstOrDefaultAsync(
                userQuery.Where(u => u.GetProperty<Guid?>("DepartmentId") == departmentId)
            );
            return user?.Id;
        }
        catch
        {
            return null;
        }
    }

    private async Task EnrichTaskDtosAsync(List<TaskDto> dtos, CancellationToken cancellationToken = default)
    {
        if (dtos.Count == 0) return;

        var projectIds = dtos.Where(x => x.ProjectId.HasValue && x.ProjectId.Value != Guid.Empty).Select(x => x.ProjectId!.Value).Distinct().ToList();
        if (projectIds.Count > 0)
        {
            var projects = await _projectRepository.GetListAsync(x => projectIds.Contains(x.Id), cancellationToken: cancellationToken);
            var projectDict = projects.ToDictionary(p => p.Id);
            foreach (var dto in dtos)
            {
                if (dto.ProjectId.HasValue && projectDict.TryGetValue(dto.ProjectId.Value, out var proj))
                {
                    dto.ProjectName = proj.Name;
                }
            }
        }

        var assigneeIds = dtos.Where(x => x.AssigneeId.HasValue && x.AssigneeId.Value != Guid.Empty).Select(x => x.AssigneeId!.Value).Distinct().ToList();
        if (assigneeIds.Count > 0)
        {
            var users = await _userRepository.GetListAsync(x => assigneeIds.Contains(x.Id), cancellationToken: cancellationToken);
            var userDict = users.ToDictionary(u => u.Id);
            foreach (var dto in dtos)
            {
                if (dto.AssigneeId.HasValue && userDict.TryGetValue(dto.AssigneeId.Value, out var usr))
                {
                    dto.AssigneeName = !string.IsNullOrWhiteSpace(usr.Name) ? usr.Name : (usr.UserName ?? string.Empty);
                    dto.AssigneeUserName = usr.UserName;
                }
            }
        }
    }

    private async Task GenerateNextRecurringTaskIfNeededAsync(TaskItem currentTask)
    {
        if (!currentTask.IsRecurring || !currentTask.DueDate.HasValue) return;

        DateTime dueDateVal = currentTask.DueDate.Value;
        DateTime nextDueDate = currentTask.Frequency switch
        {
            RecurrenceFrequency.Daily => dueDateVal.AddDays(1),
            RecurrenceFrequency.Weekly => dueDateVal.AddDays(7),
            RecurrenceFrequency.Monthly => dueDateVal.AddMonths(1),
            _ => dueDateVal.AddDays(1)
        };

        var existingNextTask = await Repository.FirstOrDefaultAsync(t =>
            t.Title == currentTask.Title && t.DueDate.HasValue && t.DueDate.Value.Date == nextDueDate.Date);

        if (existingNextTask == null)
        {
            var nextTask = new TaskItem
            {
                Title = currentTask.Title,
                Description = currentTask.Description,
                CategoryId = currentTask.CategoryId,
                AssigneeId = currentTask.AssigneeId,
                DepartmentId = currentTask.DepartmentId,
                MilestoneId = currentTask.MilestoneId,
                ProjectId = currentTask.ProjectId,
                Priority = currentTask.Priority,
                Status = TaskItemStatus.New,
                ProgressPercent = 0,
                DueDate = nextDueDate,
                IsRecurring = true,
                Frequency = currentTask.Frequency,
                FileName = currentTask.FileName,
                FileUrl = currentTask.FileUrl
            };

            await Repository.InsertAsync(nextTask, autoSave: true);
            await LogActivityAsync(nextTask.Id, $"Được tự động sinh ra từ công việc lặp lại: '{currentTask.Title}'");
        }
    }

    protected override async Task<TaskDto> MapToGetOutputDtoAsync(TaskItem entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        dto.Title = await GetLocalizedTitleAsync(entity.Title, entity.TitleEn);
        dto.Description = await GetLocalizedDescriptionAsync(entity.Description, entity.DescriptionEn);
        dto.FileName = entity.FileName;
        dto.FileUrl = entity.FileUrl;

        if (entity.ProjectId.HasValue)
        {
            try
            {
                var project = await _projectRepository.FindAsync(entity.ProjectId.Value, cancellationToken: CancellationToken.None);
                dto.ProjectName = project?.Name;
            }
            catch (TaskCanceledException) { }
        }

        if (entity.AssigneeId.HasValue && entity.AssigneeId.Value != Guid.Empty)
        {
            try
            {
                var user = await _userRepository.FindAsync(entity.AssigneeId.Value, cancellationToken: CancellationToken.None);
                if (user != null)
                {
                    dto.AssigneeName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.UserName ?? string.Empty);
                    dto.AssigneeUserName = user.UserName;
                }
            }
            catch (TaskCanceledException) { }
        }

        try
        {
            var commentQuery = await _commentRepository.WithDetailsAsync(x => x.Attachments);
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
                    dto.SubmissionNote = rawText[(rawText.IndexOf("[NỘP TRÌNH DUYỆT]:") + "[NỘP TRÌNH DUYỆT]:".Length)..].Trim();
                }
                else if (rawText.Contains("[NỘP TRÌNH DUYỆT]"))
                {
                    dto.SubmissionNote = rawText[(rawText.IndexOf("[NỘP TRÌNH DUYỆT]") + "[NỘP TRÌNH DUYỆT]".Length)..].Trim();
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

                dto.SubmissionFiles = [.. submissionAttachments.Select(a => new TaskFileDto
                {
                    FileName = a.FileName,
                    FileUrl = a.FileUrl
                })];
            }
        }
        catch (TaskCanceledException) { }

        return dto;
    }

    protected override async Task<List<TaskDto>> MapToGetListOutputDtosAsync(List<TaskItem> entities)
    {
        var dtos = await base.MapToGetListOutputDtosAsync(entities);
        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
            if (entity != null)
            {
                dto.Title = await GetLocalizedTitleAsync(entity.Title, entity.TitleEn);
                dto.Description = await GetLocalizedDescriptionAsync(entity.Description, entity.DescriptionEn);
            }
        }
        await EnrichTaskDtosAsync(dtos);
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

        var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
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
        string actionEn = action;
        try
        {
            if (!string.IsNullOrWhiteSpace(action))
            {
                actionEn = await TranslateToEnglishOrDefaultAsync(action);
            }
        }
        catch
        {
            actionEn = action;
        }

        var log = new TaskActivityLog(GuidGenerator.Create())
        {
            TaskId = taskId,
            Action = action,
            ActionEn = actionEn // Đảm bảo entity TaskActivityLog của bạn có thuộc tính ActionEn (hoặc TextEn tùy theo cách bạn đặt tên cột trong DB)
        };
        await _activityLogRepository.InsertAsync(log, autoSave: false);
    }
    private void ApplyActivityLogLocalization(TaskActivityLogDto dto, string? actionEn)
    {
        if (IsCurrentCultureEnglish() && !string.IsNullOrWhiteSpace(actionEn))
        {
            dto.Action = actionEn;
        }
    }
    private async Task NotifyTaskStakeholdersAsync(TaskItem task, string message, Guid? excludeUserId = null)
    {
        var userIds = new HashSet<Guid>();

        if (task.AssigneeId.HasValue && task.AssigneeId.Value != Guid.Empty)
            userIds.Add(task.AssigneeId.Value);

        if (task.CreatorId.HasValue && task.CreatorId.Value != Guid.Empty)
            userIds.Add(task.CreatorId.Value);

        try
        {
            var userQuery = await _userRepository.GetQueryableAsync();
            var allUsers = await AsyncExecuter.ToListAsync(userQuery);

            foreach (var user in allUsers)
            {
                var userName = user.UserName?.ToLower() ?? string.Empty;
                var email = user.Email?.ToLower() ?? string.Empty;

                if (userName.Contains("admin") ||
                    userName.Contains("manager") ||
                    userName.Contains("quanly") ||
                    email.Contains("admin"))
                {
                    userIds.Add(user.Id);
                }
            }
        }
        catch { }

        foreach (var userId in userIds.Distinct())
        {
            if (excludeUserId.HasValue && userId == excludeUserId.Value && userId != task.AssigneeId)
                continue;

            try
            {
                var notification = new Notification(
                    GuidGenerator.Create(),
                    userId,
                    message,
                    task.Id.ToString()
                );

                await _notificationRepository.InsertAsync(notification, autoSave: false);

                await _distributedEventBus.PublishAsync(new TaskNotificationEto
                {
                    UserId = userId,
                    TaskId = task.Id,
                    Message = message
                });
            }
            catch { }
        }
    }
    #endregion
}