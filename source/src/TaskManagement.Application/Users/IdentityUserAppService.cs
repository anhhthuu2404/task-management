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
            // 1. Kiểm tra xem user hiện tại có quyền Admin (cập nhật user khác) hay không
            bool isAdmin = await AuthorizationService.IsGrantedAsync("AbpIdentity.Users.Update");

            // 2. Lấy ID của user đang đăng nhập
            Guid? currentUserId = CurrentUser.Id;

            // 3. Phân quyền: Chỉ cho phép thực hiện nếu LÀ ADMIN hoặc LÀ CHÍNH CHỦ (đổi mật khẩu của chính mình)
            if (!isAdmin && currentUserId != userId)
            {
                throw new Volo.Abp.UserFriendlyException("Bạn chỉ có quyền thay đổi mật khẩu cho tài khoản của chính mình!");
            }

            // 4. Tìm kiếm user trong hệ thống Identity
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Volo.Abp.UserFriendlyException("Không tìm thấy người dùng!");
            }

            // 5. Tiến hành tạo token và đổi mật khẩu an toàn theo chuẩn ABP/Identity
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