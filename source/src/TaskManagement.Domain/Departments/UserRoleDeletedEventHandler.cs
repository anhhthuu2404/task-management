using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;

namespace TaskManagement.Departments
{
    public class UserRoleDeletedEventHandler : ILocalEventHandler<EntityDeletedEventData<IdentityUserRole>>, ITransientDependency
    {
        // Sửa ở đây: Bỏ kiểu khóa chính Guid nếu UserDepartment dùng khóa phức hợp hoặc định nghĩa đặc biệt
        private readonly IRepository<UserDepartment> _userDepartmentRepository;
        private readonly IRepository<Department, Guid> _departmentRepository;
        private readonly IRepository<IdentityRole, Guid> _roleRepository;

        public UserRoleDeletedEventHandler(
            IRepository<UserDepartment> userDepartmentRepository,
            IRepository<Department, Guid> departmentRepository,
            IRepository<IdentityRole, Guid> roleRepository)
        {
            _userDepartmentRepository = userDepartmentRepository;
            _departmentRepository = departmentRepository;
            _roleRepository = roleRepository;
        }

        public async Task HandleEventAsync(EntityDeletedEventData<IdentityUserRole> eventData)
        {
            var role = await _roleRepository.GetAsync(eventData.Entity.RoleId);
            if (role == null || string.IsNullOrWhiteSpace(role.Name))
            {
                return;
            }

            string roleName = role.Name.Trim();

            var department = await _departmentRepository.FirstOrDefaultAsync(x => x.Code == roleName);
            if (department == null)
            {
                return;
            }

            var userDept = await _userDepartmentRepository.FirstOrDefaultAsync(
                x => x.DepartmentId == department.Id && x.UserId == eventData.Entity.UserId);

            if (userDept != null)
            {
                await _userDepartmentRepository.DeleteAsync(userDept);
            }
        }
    }
}