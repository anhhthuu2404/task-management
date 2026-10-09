using GTranslate.Translators;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using TaskManagement.Provider.Interface;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Roles
{
    public class RoleAppService : ApplicationService, IRoleAppService
    {
        private readonly IRoleProvider _roleProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITranslator _translator;

        public RoleAppService(
            IRoleProvider roleProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _roleProvider = roleProvider;
            _httpContextAccessor = httpContextAccessor;
            _translator = new GoogleTranslator();
        }

        // Lấy danh sách có phân trang và dịch tên hiển thị theo ngôn ngữ Header (Abp-Culture / Accept-Language)
        public async Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting)
        {
            var currentCulture = GetCurrentCulture();
            var pagedResult = await _roleProvider.GetListAsync(filter, skipCount, maxResultCount, sorting);

            foreach (var role in pagedResult.Items)
            {
                role.DisplayName = ResolveDisplayName(role, currentCulture);
            }

            return pagedResult;
        }

        // Lấy chi tiết theo ID
        public async Task<RoleDto?> GetByIdAsync(Guid id)
        {
            var role = await _roleProvider.GetByIdAsync(id);
            if (role == null) return null;

            var currentCulture = GetCurrentCulture();
            role.DisplayName = ResolveDisplayName(role, currentCulture);
            return role;
        }

        // Tạo mới vai trò (Tự động dịch sang tiếng Anh nếu NameEn trống với cơ chế Safe Fallback)
        public async Task<RoleDto> CreateAsync(CreateUpdateRoleDto input)
        {
            var (nameVi, nameEn) = await ResolveAndTranslateNamesAsync(input.Name, input.NameVi, input.NameEn);

            var newId = Guid.NewGuid();
            var roleDto = new RoleDto
            {
                Id = newId,
                Name = input.Name,
                NameVi = nameVi,
                NameEn = nameEn,
                NormalizedName = input.Name.ToUpperInvariant(),
                IsDefault = input.IsDefault,
                IsPublic = input.IsPublic,
                IsStatic = false
            };

            await _roleProvider.CreateAsync(roleDto);

            // Trả về dữ liệu đầy đủ kèm theo DisplayName hiện tại
            return await GetByIdAsync(newId);
        }

        // Cập nhật vai trò
        public async Task<RoleDto> UpdateAsync(Guid id, CreateUpdateRoleDto input)
        {
            var (nameVi, nameEn) = await ResolveAndTranslateNamesAsync(input.Name, input.NameVi, input.NameEn);

            // Đồng bộ lại giá trị sau khi xử lý vào input để truyền xuống Provider và Stored Procedure
            input.NameVi = nameVi;
            input.NameEn = nameEn;

            await _roleProvider.UpdateAsync(id, input);

            return await GetByIdAsync(id);
        }

        // Xóa vai trò
        public async Task DeleteAsync(Guid id)
        {
            await _roleProvider.DeleteAsync(id);
        }

        #region Helper Methods

        // Gom nhóm logic xử lý tiêu đề và tích hợp cơ chế tự động dịch kèm Safe Fallback an toàn
        private async Task<(string nameVi, string nameEn)> ResolveAndTranslateNamesAsync(string name, string? inputNameVi, string? inputNameEn)
        {
            string nameVi = !string.IsNullOrEmpty(inputNameVi) ? inputNameVi : name;
            string nameEn = inputNameEn;

            if (string.IsNullOrEmpty(nameEn))
            {
                try
                {
                    // Thư viện tự động nhận diện ngôn ngữ nguồn và dịch sang tiếng Anh ("en")
                    var translationResult = await _translator.TranslateAsync(nameVi, "en");
                    if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                    {
                        nameEn = translationResult.Translation;
                    }
                }
                catch
                {
                    // Safe Fallback: Đảm bảo ứng dụng không bị crash/gián đoạn nếu lỗi mạng hoặc lỗi API dịch
                    nameEn = nameVi;
                }
            }

            return (nameVi, nameEn ?? nameVi);
        }

        private string ResolveDisplayName(RoleDto role, string currentCulture)
        {
            bool isVietnamese = currentCulture.IndexOf("vi", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isVietnamese)
            {
                return !string.IsNullOrEmpty(role.NameVi) ? role.NameVi : role.Name;
            }
            else
            {
                return !string.IsNullOrEmpty(role.NameEn) ? role.NameEn : role.Name;
            }
        }

        private string GetCurrentCulture()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "vi";

            string currentCulture = context.Request.Headers["Abp-Culture"].ToString();
            if (string.IsNullOrEmpty(currentCulture))
                currentCulture = context.Request.Headers["Accept-Language"].ToString();

            if (string.IsNullOrEmpty(currentCulture) && context.Request.Cookies.ContainsKey(".AspNetCore.Culture"))
                currentCulture = context.Request.Cookies[".AspNetCore.Culture"];

            if (string.IsNullOrEmpty(currentCulture)) return "vi";

            if (currentCulture.Contains(",")) currentCulture = currentCulture.Split(',')[0];
            if (currentCulture.Contains(";")) currentCulture = currentCulture.Split(';')[0];

            return currentCulture.Trim();
        }
        #endregion
    }
}