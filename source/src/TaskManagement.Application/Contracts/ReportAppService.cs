using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Projects;
using TaskManagement.Reports.Dtos;
using TaskManagement.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace TaskManagement.Reports
{
    [Route("api/app/report")]
    public class ReportAppService : ApplicationService, IApplicationService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IRepository<Project, Guid> _projectRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public ReportAppService(
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Project, Guid> projectRepository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        [HttpGet("get-task-report")]
        public async Task<List<TaskReportItemDto>> GetTaskReportAsync([FromQuery] TaskReportQueryDto input)
        {
            var tasks = await _taskRepository.GetListAsync();
            var projects = await _projectRepository.GetListAsync();
            var users = await _userRepository.GetListAsync();

            var projectDict = projects.ToDictionary(p => p.Id, p => p.Name);
            var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

            var query = tasks.AsQueryable();

            if (input.ProjectId.HasValue)
            {
                query = query.Where(x => x.ProjectId == input.ProjectId.Value);
            }

            if (input.EmployeeId.HasValue)
            {
                query = query.Where(x => x.AssigneeId == input.EmployeeId.Value);
            }

            if (input.FromDate.HasValue)
            {
                query = query.Where(x => x.DueDate >= input.FromDate.Value);
            }

            if (input.ToDate.HasValue)
            {
                query = query.Where(x => x.DueDate <= input.ToDate.Value);
            }

            // XỬ LÝ LỌC THEO PHÒNG BAN (Ưu tiên lọc theo danh sách ID phòng ban gồm cả nhánh con, nếu không có thì lọc phòng ban đơn lẻ)
            if (input.DepartmentIds != null && input.DepartmentIds.Any())
            {
                query = query.Where(x => x.DepartmentId.HasValue && input.DepartmentIds.Contains(x.DepartmentId.Value));
            }
            else if (input.DepartmentId.HasValue)
            {
                query = query.Where(x => x.DepartmentId == input.DepartmentId.Value);
            }

            var filteredTasks = query.ToList();

            var result = filteredTasks.Select(task =>
            {
                string projectName = string.Empty;
                if (task.ProjectId.HasValue && projectDict.TryGetValue(task.ProjectId.Value, out var projName))
                {
                    projectName = projName;
                }

                string assigneeName = task.AssigneeName ?? string.Empty;
                if (task.AssigneeId.HasValue && userDict.TryGetValue(task.AssigneeId.Value, out var uName))
                {
                    assigneeName = uName;
                }

                return new TaskReportItemDto
                {
                    Id = task.Id,
                    Title = task.Title ?? string.Empty,
                    ProjectId = task.ProjectId,
                    ProjectName = projectName,
                    AssignedUserId = task.AssigneeId,
                    AssigneeName = assigneeName,
                    DepartmentId = task.DepartmentId,
                    DepartmentName = string.Empty,
                    Status = task.Status.ToString(),
                    ProgressPercent = task.ProgressPercent,
                    DueDate = task.DueDate
                };
            }).ToList();

            return result;
        }
    }
}