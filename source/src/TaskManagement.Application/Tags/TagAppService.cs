using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Categories;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tags;

public class TagAppService :
    CrudAppService<
        Tag,
        TagDto,
        Guid,
        PagedAndSortedResultRequestDto, // Giữ nguyên để khớp 100% với ITagAppService
        CreateUpdateTagDto>,
    ITagAppService
{
    private readonly IRepository<Tag, Guid> _repository;
    private readonly IRepository<Category, Guid> _categoryRepository;

    public TagAppService(
        IRepository<Tag, Guid> repository,
        IRepository<Category, Guid> categoryRepository)
        : base(repository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;

        GetPolicyName = TaskManagementPermissions.Tags.Default;
        GetListPolicyName = TaskManagementPermissions.Tags.Default;
        CreatePolicyName = TaskManagementPermissions.Tags.Create;
        UpdatePolicyName = TaskManagementPermissions.Tags.Edit;
        DeletePolicyName = TaskManagementPermissions.Tags.Delete;
    }

    public override async Task<PagedResultDto<TagDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var tagQuery = await _repository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        // Ép kiểu an toàn để lấy Filter và CategoryId từ Angular gửi lên
        if (input is GetTagListInput filterInput)
        {
            if (!string.IsNullOrWhiteSpace(filterInput.Filter))
            {
                var filterTrimmed = filterInput.Filter.Trim();
                tagQuery = tagQuery.Where(x => x.Name.Contains(filterTrimmed));
            }

            if (filterInput.CategoryId.HasValue && filterInput.CategoryId.Value != Guid.Empty)
            {
                tagQuery = tagQuery.Where(x => x.CategoryId == filterInput.CategoryId.Value);
            }
        }

        // LEFT JOIN
        var query = from tag in tagQuery
                    join category in categoryQuery on tag.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    select new { tag, category };

        var totalCount = await AsyncExecuter.CountAsync(query);

        // Xử lý Sorting an toàn với StringComparison
        string sorting = string.IsNullOrWhiteSpace(input.Sorting)
            ? "tag.CreationTime descending"
            : (input.Sorting.Contains("name", StringComparison.OrdinalIgnoreCase)
                ? input.Sorting.Replace("name", "tag.Name", StringComparison.OrdinalIgnoreCase)
                : $"tag.{input.Sorting}");

        query = query.OrderBy(sorting);

        var items = await AsyncExecuter.ToListAsync(
            query.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        // Mapped dữ liệu & dùng Null-coalescing (??) để xử lý triệt để Warning Nullable
        var dtos = items.Select(x => new TagDto
        {
            Id = x.tag.Id,
            Name = x.tag.Name,
            ColorCode = x.tag.ColorCode ?? string.Empty,
            CategoryId = x.tag.CategoryId,
            CategoryName = x.category?.Name,
            CreationTime = x.tag.CreationTime,
            CreatorId = x.tag.CreatorId,
            LastModificationTime = x.tag.LastModificationTime,
            LastModifierId = x.tag.LastModifierId,
            IsDeleted = x.tag.IsDeleted,
            DeleterId = x.tag.DeleterId,
            DeletionTime = x.tag.DeletionTime
        }).ToList();

        return new PagedResultDto<TagDto>(totalCount, dtos);
    }

    public override async Task<TagDto> GetAsync(Guid id)
    {
        var tagQuery = await _repository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        var query = from tag in tagQuery
                    where tag.Id == id
                    join category in categoryQuery on tag.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    select new { tag, category };

        var item = await AsyncExecuter.FirstOrDefaultAsync(query);

        if (item == null)
        {
            throw new UserFriendlyException("Không tìm thấy thẻ này!");
        }

        return new TagDto
        {
            Id = item.tag.Id,
            Name = item.tag.Name,
            ColorCode = item.tag.ColorCode ?? string.Empty,
            CategoryId = item.tag.CategoryId,
            CategoryName = item.category?.Name,
            CreationTime = item.tag.CreationTime,
            CreatorId = item.tag.CreatorId,
            LastModificationTime = item.tag.LastModificationTime,
            LastModifierId = item.tag.LastModifierId,
            IsDeleted = item.tag.IsDeleted,
            DeleterId = item.tag.DeleterId,
            DeletionTime = item.tag.DeletionTime
        };
    }
}

public class GetTagListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
}