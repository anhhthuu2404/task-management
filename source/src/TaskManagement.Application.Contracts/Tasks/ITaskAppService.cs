using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks;

public interface ITaskAppService : ICrudAppService<
    TaskDto,
    Guid,
    GetTaskListInputDto,
    CreateTaskInputDto,
    UpdateTaskInputDto>
{
    Task<TaskDto> UpdateStatusAsync(Guid id, TaskItemStatus status);
    Task<TaskDto> UpdateAssigneeAsync(Guid id, Guid? assigneeId);
}