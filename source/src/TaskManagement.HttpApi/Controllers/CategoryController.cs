using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManagement;
using TaskManagement.Categories;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Controllers;

[RemoteService(Name = TaskManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TaskManagementRemoteServiceConsts.ModuleName)]
[Route("api/task-management/categories")]
public class CategoryController(ICategoryAppService categoryAppService) : TaskManagementController, ICategoryAppService
{
    [HttpGet]
    public Task<PagedResultDto<CategoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        => categoryAppService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<CategoryDto> GetAsync(Guid id)
        => categoryAppService.GetAsync(id);

    [HttpPost]
    public Task<CategoryDto> CreateAsync(CreateUpdateCategoryDto input)
        => categoryAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<CategoryDto> UpdateAsync(Guid id, CreateUpdateCategoryDto input)
        => categoryAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => categoryAppService.DeleteAsync(id);
}