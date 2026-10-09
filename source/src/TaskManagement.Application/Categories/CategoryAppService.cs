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
using GTranslate.Translators;

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
    private readonly ICategoryProvider _categoryProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITranslator _translator;

    public CategoryAppService(
        IRepository<Category, Guid> repository,
        ICategoryProvider categoryProvider,
        IHttpContextAccessor httpContextAccessor)
        : base(repository)
    {
        _categoryProvider = categoryProvider;
        _httpContextAccessor = httpContextAccessor;
        _translator = new GoogleTranslator();

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

        var currentCulture = httpContext?.Request.Headers["Abp-Culture"].ToString();
        if (string.IsNullOrEmpty(currentCulture))
        {
            currentCulture = httpContext?.Request.Headers["Accept-Language"].ToString();
        }
        if (string.IsNullOrEmpty(currentCulture))
        {
            currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
        }
        if (!string.IsNullOrEmpty(currentCulture) && currentCulture.Contains(","))
        {
            currentCulture = currentCulture.Split(',')[0];
        }

        var request = new CategoryGetListRequest
        {
            Filter = filter,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };

        var (items, totalCount) = await _categoryProvider.GetPagedListAsync(request);

        // Chuyển đổi hiển thị Tên và Mô tả theo ngôn ngữ giao diện
        foreach (var item in items)
        {
            if (currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(item.NameEn)) item.Name = item.NameEn;
                if (!string.IsNullOrEmpty(item.DescriptionEn)) item.Description = item.DescriptionEn;
            }
            else
            {
                if (!string.IsNullOrEmpty(item.NameVi)) item.Name = item.NameVi;
                if (!string.IsNullOrEmpty(item.DescriptionVi)) item.Description = item.DescriptionVi;
            }
        }

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
        // 1. Dịch Tên sang tiếng Anh
        string translatedNameEn = input.Name;
        try
        {
            var res = await _translator.TranslateAsync(input.Name, "en");
            if (res != null && !string.IsNullOrEmpty(res.Translation))
            {
                translatedNameEn = res.Translation;
            }
        }
        catch { }

        // 2. Dịch Mô tả sang tiếng Anh (nếu có)
        string translatedDescEn = input.Description ?? string.Empty;
        if (!string.IsNullOrEmpty(input.Description))
        {
            try
            {
                var resDesc = await _translator.TranslateAsync(input.Description, "en");
                if (resDesc != null && !string.IsNullOrEmpty(resDesc.Translation))
                {
                    translatedDescEn = resDesc.Translation;
                }
            }
            catch { }
        }

        input.NameVi = input.Name;
        input.NameEn = translatedNameEn;

        var id = Guid.NewGuid();
        var dto = new CategoryDto
        {
            Id = id,
            Name = input.NameVi,
            NameVi = input.NameVi,
            NameEn = translatedNameEn,
            Description = input.Description,
            DescriptionVi = input.Description,
            DescriptionEn = translatedDescEn,
            ColorCode = input.ColorCode
        };

        await _categoryProvider.InsertAsync(dto);
        return await GetAsync(id);
    }

    public override async Task<CategoryDto> UpdateAsync(Guid id, CreateUpdateCategoryDto input)
    {
        var existing = await _categoryProvider.GetByIdAsync(id);
        if (existing == null) throw new Exception("Không tìm thấy danh mục!");

        if (string.IsNullOrEmpty(input.Name))
        {
            input.Name = existing.NameVi;
        }

        // Dịch Tên nếu chưa có tiếng Anh
        string translatedNameEn = input.NameEn;
        if (string.IsNullOrEmpty(translatedNameEn))
        {
            try
            {
                var res = await _translator.TranslateAsync(input.Name, "en");
                translatedNameEn = res?.Translation ?? input.Name;
            }
            catch { translatedNameEn = input.Name; }
        }

        // Dịch Mô tả nếu chưa có tiếng Anh
        string translatedDescEn = input.DescriptionEn;
        if (string.IsNullOrEmpty(translatedDescEn) && !string.IsNullOrEmpty(input.Description))
        {
            try
            {
                var resDesc = await _translator.TranslateAsync(input.Description, "en");
                translatedDescEn = resDesc?.Translation ?? input.Description;
            }
            catch { translatedDescEn = input.Description; }
        }

        existing.Name = input.Name;
        existing.NameVi = input.Name;
        existing.NameEn = translatedNameEn;
        existing.Description = input.Description;
        existing.DescriptionVi = input.Description;
        existing.DescriptionEn = translatedDescEn;
        existing.ColorCode = input.ColorCode;

        await _categoryProvider.UpdateAsync(id, existing);
        return await GetAsync(id);
    }

    public override async Task DeleteAsync(Guid id)
    {
        await _categoryProvider.DeleteAsync(id, CurrentUser.Id);
    }
}