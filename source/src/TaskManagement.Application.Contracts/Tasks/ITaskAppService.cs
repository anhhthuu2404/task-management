using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks;

public interface ITaskAppService : IApplicationService
{
    Task<TaskDto> GetAsync(Guid id);
    Task<PagedResultDto<TaskDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<TaskDto> CreateAsync(CreateTaskInputDto input);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskInputDto input);
    Task DeleteAsync(Guid id);
}