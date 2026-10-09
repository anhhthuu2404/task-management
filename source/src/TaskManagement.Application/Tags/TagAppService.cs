using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using TaskManagement.Categories;
using TaskManagement.Permissions;
using TaskManagement.Provider.Interface;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using GTranslate.Translators;

namespace TaskManagement.Tags
{
    public class TagAppService : ApplicationService, ITagAppService
    {
        private readonly ITagProvider _tagProvider;
        private readonly IRepository<Tag, Guid> _tagRepository;
        private readonly IRepository<Category, Guid> _categoryRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITranslator _translator;

        public TagAppService(
            ITagProvider tagProvider,
            IRepository<Tag, Guid> tagRepository,
            IRepository<Category, Guid> categoryRepository,
            IHttpContextAccessor httpContextAccessor,
            ITranslator translator)
        {
            _tagProvider = tagProvider;
            _tagRepository = tagRepository;
            _categoryRepository = categoryRepository;
            _httpContextAccessor = httpContextAccessor;
            _translator = translator;
        }

        private string GetCurrentCulture()
        {
            var currentCulture = _httpContextAccessor.HttpContext?.Request.Headers["Abp-Culture"].ToString();

            if (string.IsNullOrEmpty(currentCulture))
            {
                currentCulture = _httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].ToString();
            }

            if (string.IsNullOrEmpty(currentCulture))
            {
                currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            }

            if (!string.IsNullOrEmpty(currentCulture) && currentCulture.Contains(","))
            {
                currentCulture = currentCulture.Split(',')[0];
            }

            return string.IsNullOrEmpty(currentCulture) ? "vi" : currentCulture;
        }

        public async Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input)
        {
            var culture = GetCurrentCulture();
            return await _tagProvider.GetListAsync(input, culture);
        }

        public async Task<TagDto> GetAsync(Guid id)
        {
            var culture = GetCurrentCulture();
            return await _tagProvider.GetByIdAsync(id, culture);
        }

        public async Task<TagDto> CreateAsync(CreateUpdateTagDto input)
        {
            await ValidateCategoryAsync(input.CategoryId);

            string textToTranslate = !string.IsNullOrEmpty(input.Name) ? input.Name : input.NameVi;
            string translatedEn = textToTranslate;

            try
            {
                // Chỉ truyền ngôn ngữ đích là "en", hệ thống tự động nhận diện ngôn ngữ nguồn
                var translationResult = await _translator.TranslateAsync(textToTranslate, "en");
                if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                {
                    translatedEn = translationResult.Translation;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LỖI DỊCH THUẬT CREATE: " + ex.Message);
                translatedEn = textToTranslate; // Fallback an toàn nếu mất mạng hoặc lỗi thư viện
            }

            var id = Guid.NewGuid();
            var dto = new TagDto
            {
                Id = id,
                NameVi = textToTranslate,
                NameEn = translatedEn, // Lưu chuẩn tiếng Anh vào DB
                Name = textToTranslate,
                ColorCode = input.ColorCode,
                CategoryId = input.CategoryId
            };

            await _tagProvider.CreateAsync(dto, CurrentUser.Id);

            var culture = GetCurrentCulture();
            return await _tagProvider.GetByIdAsync(id, culture);
        }

        public async Task<TagDto> UpdateAsync(Guid id, CreateUpdateTagDto input)
        {
            await ValidateCategoryAsync(input.CategoryId);

            string textToTranslate = !string.IsNullOrEmpty(input.Name) ? input.Name : input.NameVi;
            string translatedEn = input.NameEn;

            try
            {
                // Dịch lại tiếng Anh nếu có thay đổi tên tiếng Việt
                var translationResult = await _translator.TranslateAsync(textToTranslate, "en");
                if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                {
                    translatedEn = translationResult.Translation;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LỖI DỊCH THUẬT UPDATE: " + ex.Message);
                if (string.IsNullOrEmpty(translatedEn))
                {
                    translatedEn = textToTranslate;
                }
            }

            input.NameVi = textToTranslate;
            input.NameEn = translatedEn;
            input.Name = textToTranslate;

            await _tagProvider.UpdateAsync(id, input, CurrentUser.Id);

            var culture = GetCurrentCulture();
            return await _tagProvider.GetByIdAsync(id, culture);
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