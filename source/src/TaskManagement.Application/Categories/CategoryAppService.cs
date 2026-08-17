using System;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Categories;

public class CategoryAppService :
    CrudAppService<
        Category,
        CategoryDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateCategoryDto>,
    ICategoryAppService
{
    public CategoryAppService(IRepository<Category, Guid> repository)
        : base(repository)
    {
        GetPolicyName = TaskManagementPermissions.Categories.Default;
        GetListPolicyName = TaskManagementPermissions.Categories.Default;
        CreatePolicyName = TaskManagementPermissions.Categories.Create;
        UpdatePolicyName = TaskManagementPermissions.Categories.Edit;
        DeletePolicyName = TaskManagementPermissions.Categories.Delete;
    }
}