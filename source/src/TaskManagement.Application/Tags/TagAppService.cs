using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Categories;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tags;

public class TagAppService : CrudAppService<Tag, TagDto, Guid, GetTagListInput, CreateUpdateTagDto>, ITagAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;

    public TagAppService(
        IRepository<Tag, Guid> repository,
        IRepository<Category, Guid> categoryRepository)
        : base(repository)
    {
        _categoryRepository = categoryRepository;

        // Cấu hình Phân quyền (Permissions)
        GetPolicyName = TaskManagementPermissions.Tags.Default;
        GetListPolicyName = TaskManagementPermissions.Tags.Default;
        CreatePolicyName = TaskManagementPermissions.Tags.Create;
        UpdatePolicyName = TaskManagementPermissions.Tags.Edit;
        DeletePolicyName = TaskManagementPermissions.Tags.Delete;
    }

    public override async Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input)
    {
        var tagQuery = await Repository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim();
            tagQuery = tagQuery.Where(x => x.Name.Contains(filter));
        }

        if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
        {
            tagQuery = tagQuery.Where(x => x.CategoryId == input.CategoryId.Value);
        }

        var query = from tag in tagQuery
                    join category in categoryQuery on tag.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    select new { tag, category };

        var totalCount = await AsyncExecuter.CountAsync(query);

        string sorting = "tag.CreationTime descending";
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            var parts = input.Sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = parts[0].ToLowerInvariant();
            var dir = parts.Length > 1 && parts[1].StartsWith("desc", StringComparison.OrdinalIgnoreCase) ? "descending" : "ascending";

            var mappedField = field switch
            {
                "name" => "tag.Name",
                "categoryname" => "category.Name",
                "creationtime" => "tag.CreationTime",
                _ => "tag.CreationTime"
            };
            sorting = $"{mappedField} {dir}";
        }

        query = query.OrderBy(sorting);

        var items = await AsyncExecuter.ToListAsync(
            query.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        var dtos = items.Select(x => new TagDto
        {
            Id = x.tag.Id,
            Name = x.tag.Name,
            ColorCode = x.tag.ColorCode,
            CategoryId = x.tag.CategoryId,
            CategoryName = x.category?.Name,
            CreationTime = x.tag.CreationTime
        }).ToList();

        return new PagedResultDto<TagDto>(totalCount, dtos);
    }

    public override async Task<TagDto> CreateAsync(CreateUpdateTagDto input)
    {
        await ValidateCategoryAsync(input.CategoryId);
        return await base.CreateAsync(input);
    }

    public override async Task<TagDto> UpdateAsync(Guid id, CreateUpdateTagDto input)
    {
        await ValidateCategoryAsync(input.CategoryId);
        return await base.UpdateAsync(id, input);
    }

    private async Task ValidateCategoryAsync(Guid? categoryId)
    {
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            var exists = await _categoryRepository.AnyAsync(c => c.Id == categoryId.Value);
            if (!exists)
            {
                throw new UserFriendlyException("Danh mục được chọn không tồn tại trong hệ thống.");
            }
        }
    }
}