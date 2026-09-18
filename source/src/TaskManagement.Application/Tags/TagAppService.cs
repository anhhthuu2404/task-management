using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Categories;
using TaskManagement.Permissions;
using TaskManagement.Provider.Interface; // Đã đổi từ Providers sang Provider (số ít)
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tags
{
    public class TagAppService : ApplicationService, ITagAppService
    {
        private readonly ITagProvider _tagProvider;
        private readonly IRepository<Category, Guid> _categoryRepository;

        public TagAppService(ITagProvider tagProvider, IRepository<Category, Guid> categoryRepository)
        {
            _tagProvider = tagProvider;
            _categoryRepository = categoryRepository;

            // Cấu hình phân quyền nếu cần
            // GetPolicyName = TaskManagementPermissions.Tags.Default;
        }

        public async Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input)
        {
            return await _tagProvider.GetListAsync(input);
        }

        public async Task<TagDto> GetAsync(Guid id)
        {
            return await _tagProvider.GetByIdAsync(id);
        }

        public async Task<TagDto> CreateAsync(CreateUpdateTagDto input)
        {
            await ValidateCategoryAsync(input.CategoryId);

            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
                Name = input.Name,
                ColorCode = input.ColorCode,
                CategoryId = input.CategoryId
            };

            await _tagProvider.CreateAsync(dto, CurrentUser.Id);
            return await _tagProvider.GetByIdAsync(dto.Id);
        }

        public async Task<TagDto> UpdateAsync(Guid id, CreateUpdateTagDto input)
        {
            await ValidateCategoryAsync(input.CategoryId);
            await _tagProvider.UpdateAsync(id, input, CurrentUser.Id);
            return await _tagProvider.GetByIdAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _tagProvider.DeleteAsync(id, CurrentUser.Id);
        }

        private async Task ValidateCategoryAsync(Guid? categoryId)
        {
            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                var exists = await _categoryRepository.AnyAsync(c => c.Id == categoryId.Value);
                if (!exists)
                {
                    throw new Volo.Abp.UserFriendlyException("Danh mục được chọn không tồn tại trong hệ thống.");
                }
            }
        }
    }
}