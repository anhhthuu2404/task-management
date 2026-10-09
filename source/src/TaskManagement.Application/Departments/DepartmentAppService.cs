using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Localization;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using GTranslate.Translators;

namespace TaskManagement.Departments
{
    [Authorize(TaskManagementPermissions.Departments.Default)]
    public class DepartmentAppService : ApplicationService, IDepartmentAppService
    {
        private readonly IDepartmentProvider _departmentProvider;
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITranslator _translator;

        public DepartmentAppService(
            IDepartmentProvider departmentProvider,
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            IHttpContextAccessor httpContextAccessor,
            ITranslator translator)
        {
            _departmentProvider = departmentProvider;
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
            _translator = translator;

            LocalizationResource = typeof(TaskManagementResource);
        }

        public async Task<DepartmentDto> GetAsync(Guid id)
        {
            var currentCulture = GetCurrentCulture();
            var dto = await _departmentProvider.GetByIdAsync(id, currentCulture);
            dto.Members = await GetUsersAsync(id);
            return dto;
        }

        public async Task<PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input)
        {
            var currentCulture = GetCurrentCulture();
            var pagedResult = await _departmentProvider.GetListAsync(input, currentCulture);

            foreach (var department in pagedResult.Items)
            {
                department.Members = await GetUsersAsync(department.Id);
            }

            return pagedResult;
        }

        [Authorize(TaskManagementPermissions.Departments.Create)]
        public async Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
        {
            string textToTranslate = input.Name;
            string translatedEn = textToTranslate;

            try
            {
                // Chỉ định rõ nguồn là "vi" sang "en"
                var translationResult = await _translator.TranslateAsync(textToTranslate, "en", "vi");
                if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                {
                    translatedEn = translationResult.Translation;
                }
            }
            catch
            {
                translatedEn = textToTranslate;
            }

            input.NameVi = textToTranslate;
            input.NameEn = translatedEn;

            var id = Guid.NewGuid();
            await _departmentProvider.CreateAsync(input, id, CurrentUser.Id);
            return await GetAsync(id);
        }

        [Authorize(TaskManagementPermissions.Departments.Edit)]
        public async Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
        {
            if (string.IsNullOrEmpty(input.Name))
            {
                input.Name = input.NameVi;
            }

            string translatedEn = input.NameEn;

            // Buộc dịch lại nếu NameEn đang trống hoặc lỡ bị lưu trùng với tiếng Việt
            if (string.IsNullOrEmpty(translatedEn) || translatedEn == input.Name)
            {
                try
                {
                    var translationResult = await _translator.TranslateAsync(input.Name, "en", "vi");
                    if (translationResult != null && !string.IsNullOrEmpty(translationResult.Translation))
                    {
                        translatedEn = translationResult.Translation;
                    }
                    else
                    {
                        translatedEn = input.Name;
                    }
                }
                catch
                {
                    translatedEn = input.Name;
                }
            }

            input.NameVi = input.Name;
            input.NameEn = translatedEn;

            await _departmentProvider.UpdateAsync(id, input, CurrentUser.Id);
            return await GetAsync(id);
        }

        [Authorize(TaskManagementPermissions.Departments.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _departmentProvider.DeleteAsync(id, CurrentUser.Id);
        }

        public async Task<List<DepartmentTreeDto>> GetTreeAsync()
        {
            var currentCulture = GetCurrentCulture();
            return await _departmentProvider.GetTreeAsync(currentCulture);
        }

        public async Task<List<DepartmentMemberDto>> GetUsersAsync(Guid id)
        {
            return await _departmentProvider.GetUsersByDepartmentIdAsync(id);
        }

        [UnitOfWork]
        public async Task AssignUserAsync(AssignUserToDepartmentDto input)
        {
            await _departmentProvider.UpsertUserDepartmentAsync(input.UserId, input.DepartmentId, input.IsManager);
            await SyncUserRoleAsync(input.UserId, input.DepartmentId);

            if (input.IsManager)
            {
                await SyncManagerToChildDepartmentsRecursiveAsync(input.DepartmentId, input.UserId);
            }
        }

        public async Task DeleteUserAsync(Guid departmentId, Guid userId)
        {
            var deleted = await _departmentProvider.DeleteUserDepartmentAsync(departmentId, userId);
            if (deleted)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                var currentCulture = GetCurrentCulture();
                var departmentDto = await _departmentProvider.GetByIdAsync(departmentId, currentCulture);

                if (user != null && departmentDto != null && !string.IsNullOrWhiteSpace(departmentDto.Code))
                {
                    string roleName = departmentDto.Code.Trim();
                    if (await _userManager.IsInRoleAsync(user, roleName))
                    {
                        await _userManager.RemoveFromRoleAsync(user, roleName);
                    }
                }
            }
        }

        #region Helper Methods

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

            return currentCulture;
        }

        private async Task SyncUserRoleAsync(Guid userId, Guid departmentId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var currentCulture = GetCurrentCulture();
            var departmentDto = await _departmentProvider.GetByIdAsync(departmentId, currentCulture);

            if (user != null && departmentDto != null && !string.IsNullOrWhiteSpace(departmentDto.Code))
            {
                string roleName = departmentDto.Code.Trim();
                var roleExists = await _roleManager.RoleExistsAsync(roleName);

                if (roleExists && !await _userManager.IsInRoleAsync(user, roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
        }

        private async Task SyncManagerToChildDepartmentsRecursiveAsync(Guid parentId, Guid userId)
        {
            var currentCulture = GetCurrentCulture();
            var allTree = await _departmentProvider.GetTreeAsync(currentCulture);
            var parentNode = FindDepartmentInTree(allTree, parentId);

            if (parentNode?.Children != null)
            {
                foreach (var child in parentNode.Children)
                {
                    await _departmentProvider.UpsertUserDepartmentAsync(userId, child.Id, true);
                    await SyncUserRoleAsync(userId, child.Id);
                    await SyncManagerToChildDepartmentsRecursiveAsync(child.Id, userId);
                }
            }
        }

        private static DepartmentTreeDto? FindDepartmentInTree(List<DepartmentTreeDto> list, Guid id)
        {
            foreach (var node in list)
            {
                if (node.Id == id) return node;
                if (node.Children != null && node.Children.Count > 0)
                {
                    var found = FindDepartmentInTree(node.Children, id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        #endregion
    }
}