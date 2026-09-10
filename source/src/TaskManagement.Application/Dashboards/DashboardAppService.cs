using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Tasks; 
using TaskManagement.Projects; 
using TaskManagement.Departments; 
using TaskManagement.Categories; 
using TaskManagement.Tags; 
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace TaskManagement.Dashboards
{
    public class DashboardAppService : TaskManagementAppService, IDashboardAppService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IRepository<Project, Guid> _projectRepository;
        private readonly IRepository<Department, Guid> _departmentRepository;
        private readonly IRepository<Category, Guid> _categoryRepository;
        private readonly IRepository<Tag, Guid> _tagRepository;
        private readonly IIdentityUserRepository _userRepository;

        public DashboardAppService(
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Project, Guid> projectRepository,
            IRepository<Department, Guid> departmentRepository,
            IRepository<Category, Guid> categoryRepository,
            IRepository<Tag, Guid> tagRepository,
            IIdentityUserRepository userRepository)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _departmentRepository = departmentRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _userRepository = userRepository;
        }

        public async Task<DashboardStatisticsDto> GetStatisticsAsync()
        {
            var totalCategories = (int)await _categoryRepository.GetCountAsync();
            var totalTags = (int)await _tagRepository.GetCountAsync();
            var totalDepartments = (int)await _departmentRepository.GetCountAsync();
            var totalUsers = (int)await _userRepository.GetCountAsync();
            var totalProjects = (int)await _projectRepository.GetCountAsync();

            var tasks = await _taskRepository.GetListAsync();
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == TaskItemStatus.Completed);
            var pendingTasks = totalTasks - completedTasks;

            var tasksByStatus = tasks
                .GroupBy(t => t.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Thống kê nhân sự theo phòng ban (Ví dụ mẫu mapping cơ bản)
            var usersByDepartment = new Dictionary<string, int>();
            var departments = await _departmentRepository.GetListAsync();
            foreach (var dept in departments)
            {
                // Thay thế điều kiện đếm theo cấu trúc thực tế khóa ngoại giữa User và Department của bạn
                usersByDepartment[dept.Name] = 5;
            }

            return new DashboardStatisticsDto
            {
                TotalCategories = totalCategories,
                TotalTags = totalTags,
                TotalDepartments = totalDepartments,
                TotalUsers = totalUsers,
                TotalProjects = totalProjects,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                TasksByStatus = tasksByStatus,
                UsersByDepartment = usersByDepartment
            };
        }
    }
}