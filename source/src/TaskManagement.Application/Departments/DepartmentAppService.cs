using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Localization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace TaskManagement.Departments;

[Authorize(TaskManagementPermissions.Departments.Default)]
public class DepartmentAppService : CrudAppService<
    Department,
    DepartmentDto,
    Guid,
    GetDepartmentListDto,
    CreateUpdateDepartmentDto>, IDepartmentAppService
{
    private readonly IRepository<UserDepartment> _userDepartmentRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public DepartmentAppService(
        IRepository<Department, Guid> repository,
        IRepository<UserDepartment> userDepartmentRepository,
        IRepository<IdentityUser, Guid> userRepository)
        : base(repository)
    {
        _userDepartmentRepository = userDepartmentRepository;
        _userRepository = userRepository;

        LocalizationResource = typeof(TaskManagementResource);
        GetPolicyName = TaskManagementPermissions.Departments.Default;
        GetListPolicyName = TaskManagementPermissions.Departments.Default;
        CreatePolicyName = TaskManagementPermissions.Departments.Create;
        UpdatePolicyName = TaskManagementPermissions.Departments.Edit;
        DeletePolicyName = TaskManagementPermissions.Departments.Delete;
    }

    public override async Task<DepartmentDto> GetAsync(Guid id)
    {
        var department = await Repository.GetAsync(id);

        var dto = new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            ParentId = department.ParentId,
            IsActive = department.IsActive,
            CreationTime = department.CreationTime,
            CreatorId = department.CreatorId,
            LastModificationTime = department.LastModificationTime,
            LastModifierId = department.LastModifierId,
            IsDeleted = department.IsDeleted,
            DeleterId = department.DeleterId,
            DeletionTime = department.DeletionTime,
            Members = new List<DepartmentMemberDto>()
        };

        var query = from userDept in await _userDepartmentRepository.GetQueryableAsync()
                    join user in await _userRepository.GetQueryableAsync() on userDept.UserId equals user.Id
                    where userDept.DepartmentId == id
                    select new DepartmentMemberDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        IsManager = userDept.IsManager
                    };

        dto.Members = await AsyncExecuter.ToListAsync(query);

        return dto;
    }

    protected override async Task<IQueryable<Department>> CreateFilteredQueryAsync(GetDepartmentListDto input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            query = query.Where(x => x.Name.Contains(input.Filter) || x.Code.Contains(input.Filter));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        return query;
    }

    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        var departments = await Repository.GetListAsync();
        var departmentDtos = ObjectMapper.Map<List<Department>, List<DepartmentTreeDto>>(departments);

        var lookup = departmentDtos.ToLookup(x => x.ParentId);

        List<DepartmentTreeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId].Select(node =>
            {
                node.Children = BuildTree(node.Id);
                return node;
            }).ToList();
        }

        return BuildTree(null);
    }

    public async Task AssignUserAsync(AssignUserToDepartmentDto input)
    {
        var existing = await _userDepartmentRepository.FirstOrDefaultAsync(x => x.UserId == input.UserId && x.DepartmentId == input.DepartmentId);

        if (existing == null)
        {
            await _userDepartmentRepository.InsertAsync(new UserDepartment
            {
                UserId = input.UserId,
                DepartmentId = input.DepartmentId,
                IsManager = input.IsManager
            });
        }
        else
        {
            existing.IsManager = input.IsManager;
            await _userDepartmentRepository.UpdateAsync(existing);
        }
    }

    public async Task DeleteUserAsync(Guid departmentId, Guid userId)
    {
        var existing = await _userDepartmentRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId);
        if (existing != null)
        {
            await _userDepartmentRepository.DeleteAsync(existing);
        }
    }
}