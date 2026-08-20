using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManagement;
using TaskManagement.Departments;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Controllers;

[RemoteService(Name = TaskManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TaskManagementRemoteServiceConsts.ModuleName)]
[Route("api/task-management/departments")]
public class DepartmentController(IDepartmentAppService departmentAppService) : TaskManagementController, IDepartmentAppService
{
    [HttpGet]
    public Task<PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input)
        => departmentAppService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<DepartmentDto> GetAsync(Guid id)
        => departmentAppService.GetAsync(id);

    [HttpGet("tree")]
    public Task<List<DepartmentTreeDto>> GetTreeAsync()
        => departmentAppService.GetTreeAsync();

    [HttpPost]
    public Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
        => departmentAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
        => departmentAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => departmentAppService.DeleteAsync(id);

    [HttpPost("assign-user")]
    public Task AssignUserAsync(AssignUserToDepartmentDto input)
        => departmentAppService.AssignUserAsync(input);

    [HttpDelete("{departmentId}/users/{userId}")]
    public Task DeleteUserAsync(Guid departmentId, Guid userId)
        => departmentAppService.DeleteUserAsync(departmentId, userId);
}