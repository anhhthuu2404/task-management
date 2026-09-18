using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using TaskManagement.Provider.Interface;

namespace TaskManagement.Users
{
    public class CustomUserManagementAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IUserProvider _userProvider;

        public CustomUserManagementAppService(IdentityUserManager userManager, IUserProvider userProvider)
        {
            _userManager = userManager;
            _userProvider = userProvider;
        }

        [HttpGet]
        [Route("api/custom-user/list")]
        public async Task<PagedResultDto<UserDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting)
        {
            return await _userProvider.GetListAsync(filter, skipCount, maxResultCount, sorting);
        }

        [HttpGet]
        [Route("api/custom-user/{id}")]
        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            return await _userProvider.GetByIdAsync(id);
        }

        [HttpPost]
        [Route("api/custom-user/change-password")]
        public async Task ChangePasswordAsync(Guid userId, string newPassword)
        {
            // Kiểm tra quyền quản trị hoặc quyền cập nhật user thông thường
            bool isAdmin = await AuthorizationService.IsGrantedAsync("AbpIdentity.Users.Update");

            if (!isAdmin)
            {
                throw new Volo.Abp.UserFriendlyException("Bạn không có quyền thực hiện thao tác này!");
            }

            var user = await _userManager.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Volo.Abp.UserFriendlyException("Không tìm thấy người dùng!");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Volo.Abp.UserFriendlyException($"Không thể đổi mật khẩu: {errors}");
            }
        }
    }
}