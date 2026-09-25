using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TaskManagement.Categories;
using TaskManagement.Permissions;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Request;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Categories;

public class CategoryAppService :
    CrudAppService<
        Category,
        CategoryDto,
        Guid,
        PagedAndSortedResultRequestDto, // Giữ nguyên để khớp với ICategoryAppService
        CreateUpdateCategoryDto>,
    ICategoryAppService
{
    private readonly ICategoryProvider _categoryProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CategoryAppService(
        IRepository<Category, Guid> repository,
        ICategoryProvider categoryProvider,
        IHttpContextAccessor httpContextAccessor)
        : base(repository)
    {
        _categoryProvider = categoryProvider;
        _httpContextAccessor = httpContextAccessor;

        GetPolicyName = TaskManagementPermissions.Categories.Default;
        GetListPolicyName = TaskManagementPermissions.Categories.Default;
        CreatePolicyName = TaskManagementPermissions.Categories.Create;
        UpdatePolicyName = TaskManagementPermissions.Categories.Edit;
        DeletePolicyName = TaskManagementPermissions.Categories.Delete;
    }

    public override async Task<PagedResultDto<CategoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        string? filter = null;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request != null)
        {
            filter = httpContext.Request.Query["filter"];
            if (string.IsNullOrEmpty(filter))
            {
                filter = httpContext.Request.Query["keyword"];
            }
        }

        var request = new CategoryGetListRequest
        {
            Filter = filter,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };

        var (items, totalCount) = await _categoryProvider.GetPagedListAsync(request);

        return new PagedResultDto<CategoryDto>(totalCount, items);
    }

    public override async Task<CategoryDto> GetAsync(Guid id)
    {
        var item = await _categoryProvider.GetByIdAsync(id);
        if (item == null) throw new Exception("Không tìm thấy danh mục!");
        return item;
    }

    public override async Task<CategoryDto> CreateAsync(CreateUpdateCategoryDto input)
    {
        var id = Guid.NewGuid();
        var dto = new CategoryDto
        {
            Id = id,
            Name = input.Name,
            Description = input.Description
        };

        await _categoryProvider.InsertAsync(dto);
        return await GetAsync(id);
    }

    public override async Task<CategoryDto> UpdateAsync(Guid id, CreateUpdateCategoryDto input)
    {
        var existing = await _categoryProvider.GetByIdAsync(id);
        if (existing == null) throw new Exception("Không tìm thấy danh mục!");

        existing.Name = input.Name;
        existing.Description = input.Description;

        await _categoryProvider.UpdateAsync(id, existing);
        return await GetAsync(id);
    }

    public override async Task DeleteAsync(Guid id)
    {
        await _categoryProvider.DeleteAsync(id, CurrentUser.Id);
    }
}