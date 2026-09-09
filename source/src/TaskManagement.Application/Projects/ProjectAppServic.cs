using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Notifications; // Thêm namespace chứa Notification
using TaskManagement.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;

namespace TaskManagement.Projects
{
    public class ProjectAppService : CrudAppService<
        Project,
        ProjectDto,
        Guid,
        ProjectListFilterDto,
        CreateUpdateProjectDto,
        CreateUpdateProjectDto>,
        IProjectAppService
    {
        private readonly IRepository<ProjectMilestone, Guid> _milestoneRepository;
        private readonly IRepository<ProjectMember, Guid> _memberRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IRepository<Notification, Guid> _notificationRepository;
        private readonly IDistributedEventBus _distributedEventBus;


        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<ProjectMilestone, Guid> milestoneRepository,
            IRepository<ProjectMember, Guid> memberRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Notification, Guid> notificationRepository,
            IDistributedEventBus distributedEventBus) : base(repository)
        {
            _milestoneRepository = milestoneRepository;
            _memberRepository = memberRepository;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _notificationRepository = notificationRepository;
            _distributedEventBus = distributedEventBus;
        }

        protected override async Task<IQueryable<Project>> CreateFilteredQueryAsync(ProjectListFilterDto input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (input == null)
            {
                return query;
            }

            return query
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!) || (x.Description != null && x.Description.Contains(input.Filter!)))
                .WhereIf(!string.IsNullOrWhiteSpace(input.Status), x => x.Status == input.Status)
                .WhereIf(input.DepartmentId.HasValue, x => x.DepartmentId == input.DepartmentId);
        }

        [HttpGet("/api/app/project/milestones/{projectId}")]
        public async Task<List<MilestoneDto>> GetMilestonesByProjectAsync(Guid projectId)
        {
            var milestones = await _milestoneRepository.GetListAsync(x => x.ProjectId == projectId);
            return ObjectMapper.Map<List<ProjectMilestone>, List<MilestoneDto>>(milestones);
        }

        public async Task<MilestoneDto> CreateMilestoneAsync(Guid projectId, CreateUpdateMilestoneDto input)
        {
            var milestone = new ProjectMilestone
            {
                ProjectId = projectId,
                Title = input.Title,
                Description = input.Description,
                DueDate = input.DueDate,
                Status = input.Status,
                AssigneeUserId = input.AssigneeUserId
            };

            await _milestoneRepository.InsertAsync(milestone);

            // Đồng bộ sang TaskItem
            var newTask = new TaskItem
            {
                ProjectId = projectId,
                Title = input.Title,
                Description = input.Description,
                DueDate = input.DueDate,
                Status = (TaskItemStatus)(int)input.Status,
                AssigneeId = input.AssigneeUserId
            };

            await _taskRepository.InsertAsync(newTask);

            if (input.AssigneeUserId.HasValue)
            {
                var notification = new Notification(
                    GuidGenerator.Create(),
                    input.AssigneeUserId.Value,
                    $"Bạn vừa được phân công công việc mới: {input.Title}"
                )
                {
                    TaskId = newTask.Id,
                    IsRead = false
                };

                await _notificationRepository.InsertAsync(notification);

                // Phát sự kiện realtime kèm đầy đủ nội dung và mốc thời gian tạo
                await _distributedEventBus.PublishAsync(new TaskNotificationEto
                {
                    UserId = input.AssigneeUserId.Value,
                    Message = notification.Message,
                    TaskId = newTask.Id,
                    CreationTime = notification.CreationTime
                });
            }

            // BỔ SUNG LẠI DÒNG RETURN NÀY ĐỂ TRÁNH LỖI BIÊN DỊCH
            return ObjectMapper.Map<ProjectMilestone, MilestoneDto>(milestone);
        }
        public async Task DeleteMilestoneAsync(Guid milestoneId)
        {
            var milestone = await _milestoneRepository.GetAsync(milestoneId);

            if (milestone != null)
            {
                var tasks = await _taskRepository.GetListAsync(t =>
                    t.ProjectId == milestone.ProjectId &&
                    t.Title == milestone.Title &&
                    t.DueDate == milestone.DueDate
                );

                foreach (var task in tasks)
                {
                    await _taskRepository.DeleteAsync(task.Id);
                }

                await _milestoneRepository.DeleteAsync(milestoneId);
            }
        }

        [HttpGet("/api/app/project/by-project/{projectId}/members")]
        public async Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            var members = await _memberRepository.GetListAsync(x => x.ProjectId == projectId);
            var userIds = members.Select(x => x.UserId).ToList();

            var userQuery = await _userRepository.GetQueryableAsync();
            var users = await userQuery.Where(x => userIds.Contains(x.Id)).ToListAsync();

            var resultList = members.Select(m =>
            {
                var user = users.FirstOrDefault(u => u.Id == m.UserId);
                return new ProjectMemberDto
                {
                    Id = m.Id,
                    ProjectId = m.ProjectId,
                    UserId = m.UserId,
                    Role = m.Role,
                    UserName = user?.UserName,
                    Name = user?.Name,
                    Surname = user?.Surname,
                    Email = user?.Email
                };
            }).ToList();

            return new ListResultDto<ProjectMemberDto>(resultList);
        }

        public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto input)
        {
            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = input.UserId,
                Role = input.Role
            };

            await _memberRepository.InsertAsync(member);
            return ObjectMapper.Map<ProjectMember, ProjectMemberDto>(member);
        }

        public async Task RemoveMemberAsync(Guid memberId)
        {
            await _memberRepository.DeleteAsync(memberId);
        }
    }
}