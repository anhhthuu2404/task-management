using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TaskManagement.Controllers;

[Route("api/app/task")]
public class TaskController(ITaskAppService taskAppService) : AbpControllerBase, ITaskAppService
{
    [HttpGet("{id}")]
    public Task<TaskDto> GetAsync(Guid id) => taskAppService.GetAsync(id);

    [HttpGet]
    public Task<PagedResultDto<TaskDto>> GetListAsync(PagedAndSortedResultRequestDto input) => taskAppService.GetListAsync(input);

    [HttpPost]
    public Task<TaskDto> CreateAsync([FromForm] CreateTaskInputDto input) => taskAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<TaskDto> UpdateAsync(Guid id, [FromBody] UpdateTaskInputDto input) => taskAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => taskAppService.DeleteAsync(id);
}