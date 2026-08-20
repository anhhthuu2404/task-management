using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Categories;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks;

[AllowAnonymous]
public class TaskAppService(
    IRepository<TaskItem, Guid> taskRepository,
    IRepository<Category, Guid> categoryRepository) : ApplicationService, ITaskAppService
{
    // 🟢 Khởi tạo trực tiếp Mapper của Mapperly
    private readonly TaskManagementTaskItemToTaskDtoMapper _taskMapper = new();
    private readonly TaskManagementCreateTaskInputDtoToTaskItemMapper _createMapper = new();
    private readonly TaskManagementUpdateTaskInputDtoToTaskItemMapper _updateMapper = new();

    public async Task<TaskDto> GetAsync(Guid id)
    {
        var queryable = await taskRepository.WithDetailsAsync(x => x.Attachments);
        var task = await queryable.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (task == null)
        {
            throw new EntityNotFoundException(typeof(TaskItem), id);
        }

        return _taskMapper.Map(task);
    }

    public async Task<PagedResultDto<TaskDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await taskRepository.WithDetailsAsync(x => x.Attachments);
        var count = await taskRepository.GetCountAsync();

        var list = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        // 🟢 Thay ObjectMapper.Map bằng _taskMapper.Map
        var taskDtos = list.Select(_taskMapper.Map).ToList();

        return new PagedResultDto<TaskDto>(count, taskDtos);
    }

    [HttpPost]
    public async Task<TaskDto> CreateAsync([FromForm] CreateTaskInputDto input)
    {
        if (input.CategoryId == Guid.Empty || !await categoryRepository.AnyAsync(x => x.Id == input.CategoryId))
        {
            throw new UserFriendlyException("Danh mục công việc (CategoryId) không tồn tại hoặc không hợp lệ!");
        }

        var priorityEnum = (TaskPriority)(input.Priority ?? (int)TaskPriority.Medium);

        var task = new TaskItem(
            GuidGenerator.Create(),
            input.Title,
            input.CategoryId,
            priorityEnum,
            input.AssigneeId,
            input.DueDate
        )
        {
            Description = input.Description ?? string.Empty
        };

        if (input.Files is { Length: > 0 })
        {
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            foreach (var file in input.Files)
            {
                if (file != null && (file.ContentLength ?? 0) > 0)
                {
                    var safeFileName = file.FileName ?? $"file_{Guid.NewGuid()}";
                    var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using var stream = file.GetStream();
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await stream.CopyToAsync(fileStream);

                    task.Attachments.Add(new TaskAttachment(
                        GuidGenerator.Create(),
                        task.Id,
                        safeFileName,
                        $"/uploads/{uniqueFileName}",
                        file.ContentLength ?? 0
                    ));
                }
            }
        }

        await taskRepository.InsertAsync(task, autoSave: true);
        return _taskMapper.Map(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskInputDto input)
    {
        var task = await taskRepository.GetAsync(id);

        if (input.CategoryId == Guid.Empty || !await categoryRepository.AnyAsync(x => x.Id == input.CategoryId))
        {
            throw new UserFriendlyException("Danh mục công việc không hợp lệ!");
        }

        task.Title = input.Title;
        task.Description = input.Description ?? string.Empty;
        task.CategoryId = input.CategoryId;
        task.Priority = (TaskPriority)input.Priority;
        task.DueDate = input.DueDate;
        task.AssigneeId = input.AssigneeId;

        await taskRepository.UpdateAsync(task, autoSave: true);
        return _taskMapper.Map(task);
    }

    public async Task DeleteAsync(Guid id)
    {
        await taskRepository.DeleteAsync(id);
    }
}