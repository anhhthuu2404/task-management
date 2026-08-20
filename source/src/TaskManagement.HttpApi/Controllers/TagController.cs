using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManagement;
using TaskManagement.Tags;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Controllers;

[RemoteService(Name = TaskManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TaskManagementRemoteServiceConsts.ModuleName)]
[Route("api/task-management/tags")]
public class TagController(ITagAppService tagAppService) : TaskManagementController, ITagAppService
{
    [HttpGet]
    public Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input)
        => tagAppService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<TagDto> GetAsync(Guid id)
        => tagAppService.GetAsync(id);

    [HttpPost]
    public Task<TagDto> CreateAsync(CreateUpdateTagDto input)
        => tagAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<TagDto> UpdateAsync(Guid id, CreateUpdateTagDto input)
        => tagAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => tagAppService.DeleteAsync(id);
}