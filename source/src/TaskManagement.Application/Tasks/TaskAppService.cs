using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using TaskManagement.Permissions;

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
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IWebHostEnvironment _environment;

    private readonly string[] _allowedExtensions = [".pdf", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".xlsx"];
    private const int MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB

    public TaskAppService(
        IRepository<TaskItem, Guid> repository,
        IRepository<IdentityUser, Guid> userRepository,
        IWebHostEnvironment environment) : base(repository)
    {
        _userRepository = userRepository;
        _environment = environment;

        GetPolicyName = TaskManagementPermissions.Tasks.Default;
        GetListPolicyName = TaskManagementPermissions.Tasks.Default;
        CreatePolicyName = TaskManagementPermissions.Tasks.Create;
        UpdatePolicyName = TaskManagementPermissions.Tasks.Edit;
        DeletePolicyName = TaskManagementPermissions.Tasks.Delete;
    }

    protected override async Task<IQueryable<TaskItem>> CreateFilteredQueryAsync(GetTaskListInputDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Attachments);

        return query
            .WhereIf(!string.IsNullOrWhiteSpace(input.Keyword), x => x.Title.Contains(input.Keyword!) || (x.Description != null && x.Description.Contains(input.Keyword!)))
            .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId!.Value)
            .WhereIf(input.AssigneeId.HasValue, x => x.AssigneeId == input.AssigneeId!.Value)
            .WhereIf(input.Priority.HasValue, x => x.Priority == input.Priority!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.OnlyMyTasks && CurrentUser.Id.HasValue, x => x.AssigneeId == CurrentUser.Id!.Value);
    }

    protected override IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, GetTaskListInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            return query.OrderByDescending(x => x.CreationTime);
        }

        var sorting = input.Sorting.Trim();
        var isDescending = sorting.EndsWith("DESC", StringComparison.OrdinalIgnoreCase);
        var sortField = sorting.Split(' ')[0].ToLowerInvariant();

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

    public override async Task<TaskDto> CreateAsync(CreateTaskInputDto input)
    {
        var entity = await MapToEntityAsync(input);
        await ProcessAttachmentsAsync(input.Attachments, entity);

        await Repository.InsertAsync(entity, autoSave: true);
        return await MapToGetOutputDtoAsync(entity);
    }

    public override async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskInputDto input)
    {
        var entity = await GetEntityByIdAsync(id);
        await MapToEntityAsync(input, entity);
        await ProcessAttachmentsAsync(input.Attachments, entity);

        await Repository.UpdateAsync(entity, autoSave: true);
        return await MapToGetOutputDtoAsync(entity);
    }

    // Cập nhật Trạng thái
    [HttpPost("api/app/task/{id}/status")]
    [HttpPut("api/app/task/{id}/status")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    public async Task<TaskDto> UpdateStatusAsync(Guid id, [FromQuery] TaskItemStatus status)
    {
        var entity = await GetEntityByIdAsync(id);
        entity.Status = status;

        if (status == TaskItemStatus.Completed)
        {
            entity.ProgressPercent = 100;
        }

        await Repository.UpdateAsync(entity, autoSave: true);
        return await MapToGetOutputDtoAsync(entity);
    }

    // Cập nhật Người thực hiện
    [HttpPost("api/app/task/{id}/assignee")]
    [HttpPut("api/app/task/{id}/assignee")]
    [Authorize(TaskManagementPermissions.Tasks.Edit)]
    public async Task<TaskDto> UpdateAssigneeAsync(Guid id, [FromQuery] Guid? assigneeId)
    {
        var entity = await GetEntityByIdAsync(id);
        entity.AssigneeId = assigneeId;

        await Repository.UpdateAsync(entity, autoSave: true);
        return await MapToGetOutputDtoAsync(entity);
    }

    protected override async Task<TaskDto> MapToGetOutputDtoAsync(TaskItem entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);

        if (entity.AssigneeId.HasValue)
        {
            try
            {
                var user = await _userRepository.FindAsync(entity.AssigneeId.Value);
                if (user != null)
                {
                    dto.AssigneeName = user.Name;
                    dto.AssigneeUserName = user.UserName;
                }
            }
            catch
            {
            }
        }

        return dto;
    }

    protected override async Task<List<TaskDto>> MapToGetListOutputDtosAsync(List<TaskItem> entities)
    {
        var dtos = await base.MapToGetListOutputDtosAsync(entities);

        if (dtos == null || dtos.Count == 0)
        {
            return [];
        }

        var assigneeIds = entities
            .Where(x => x.AssigneeId.HasValue)
            .Select(x => x.AssigneeId!.Value)
            .Distinct()
            .ToList();

        if (assigneeIds.Count > 0)
        {
            try
            {
                var queryable = await _userRepository.GetQueryableAsync();
                var users = await AsyncExecuter.ToListAsync(queryable.Where(x => assigneeIds.Contains(x.Id)));
                var userDict = users.ToDictionary(u => u.Id);

                foreach (var dto in dtos)
                {
                    if (dto.AssigneeId.HasValue && userDict.TryGetValue(dto.AssigneeId.Value, out var user))
                    {
                        dto.AssigneeName = user.Name;
                        dto.AssigneeUserName = user.UserName;
                    }
                }
            }
            catch
            {
            }
        }

        return dtos;
    }

    private async Task ProcessAttachmentsAsync(List<TaskAttachmentDto>? attachments, TaskItem entity)
    {
        if (attachments != null && attachments.Count > 0)
        {
            List<string> fileUrls = [];
            List<string> fileNames = [];

            foreach (var file in attachments)
            {
                if (!string.IsNullOrEmpty(file.FileContent) && !string.IsNullOrEmpty(file.FileName))
                {
                    var url = await SaveBase64FileAsync(file.FileName, file.FileContent);
                    fileUrls.Add(url);
                    fileNames.Add(file.FileName);
                }
            }

            if (fileUrls.Count > 0)
            {
                entity.FileUrl = string.Join(";", fileUrls);
                entity.FileName = string.Join(";", fileNames);
            }
        }
    }

    private async Task<string> SaveBase64FileAsync(string fileName, string base64Content)
    {
        if (base64Content.Contains(','))
        {
            base64Content = base64Content.Split(',')[1];
        }

        var bytes = Convert.FromBase64String(base64Content);
        ValidateFile(fileName, bytes);

        var wwwRootPath = _environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var folderPath = Path.Combine(wwwRootPath, "uploads");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
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
}