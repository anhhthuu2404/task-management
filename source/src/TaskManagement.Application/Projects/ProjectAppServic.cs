using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.Notifications;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Request;
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
        private readonly IProjectProvider _projectProvider; // Provider sử dụng Stored Procedure & Dapper

        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<ProjectMilestone, Guid> milestoneRepository,
            IRepository<ProjectMember, Guid> memberRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Notification, Guid> notificationRepository,
            IDistributedEventBus distributedEventBus,
            IProjectProvider projectProvider) : base(repository)
        {
            _milestoneRepository = milestoneRepository;
            _memberRepository = memberRepository;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _notificationRepository = notificationRepository;
            _distributedEventBus = distributedEventBus;
            _projectProvider = projectProvider;
        }

        /// <summary>
        /// Tối ưu hóa việc lấy danh sách Project bằng Stored Procedure thông qua Provider (Dapper)
        /// </summary>
        public override async Task<PagedResultDto<ProjectDto>> GetListAsync(ProjectListFilterDto input)
        {
            var request = new ProjectGetListRequest
            {
                Filter = input.Filter,
                Status = input.Status,
                DepartmentId = input.DepartmentId,
                CategoryId = input.CategoryId,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount
            };

            var data = await _projectProvider.GetListAsync(request);
            var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

            var dtos = ObjectMapper.Map<List<Provider.Response.ProjectQueryResponse>, List<ProjectDto>>(data);

            return new PagedResultDto<ProjectDto>(totalCount, dtos);
        }

        [HttpGet("/api/app/project/milestones/{projectId}")]
        public async Task<List<MilestoneDto>> GetMilestonesByProjectAsync(Guid projectId)
        {
            var milestones = await _milestoneRepository.GetListAsync(x => x.ProjectId == projectId);
            return ObjectMapper.Map<List<ProjectMilestone>, List<MilestoneDto>>(milestones);
        }

        [HttpPost("/api/app/project/milestone/{projectId}")]
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

            var newTask = new TaskItem
            {
                ProjectId = projectId,
                MilestoneId = milestone.Id,
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

                // Publish sự kiện realtime qua SignalR mà bạn đã cấu hình
                await _distributedEventBus.PublishAsync(new TaskNotificationEto
                {
                    UserId = input.AssigneeUserId.Value,
                    Message = notification.Message,
                    TaskId = newTask.Id,
                    CreationTime = notification.CreationTime
                });
            }

            return ObjectMapper.Map<ProjectMilestone, MilestoneDto>(milestone);
        }

        [HttpDelete("/api/app/project/milestone/{milestoneId}")]
        public async Task DeleteMilestoneAsync(Guid milestoneId)
        {
            var milestone = await _milestoneRepository.GetAsync(milestoneId);

            if (milestone != null)
            {
                var tasks = await _taskRepository.GetListAsync(t => t.MilestoneId == milestoneId);

                foreach (var task in tasks)
                {
                    await _taskRepository.DeleteAsync(task.Id);
                }

                await _milestoneRepository.DeleteAsync(milestoneId);
            }
        }

        [HttpGet("/api/app/project/by-project/{projectId}/members")]
        public async Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            try
            {
                var members = await _memberRepository.GetListAsync(x => x.ProjectId == projectId, cancellationToken: cancellationToken);
                var userIds = members.Select(x => x.UserId).ToList();

                var userQuery = await _userRepository.GetQueryableAsync();
                var users = await userQuery.Where(x => userIds.Contains(x.Id)).ToListAsync(cancellationToken);

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
            catch (OperationCanceledException)
            {
                return new ListResultDto<ProjectMemberDto>(new List<ProjectMemberDto>());
            }
        }

        [HttpPost("/api/app/project/member/{projectId}")]
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

        [HttpDelete("/api/app/project/member/{memberId}")]
        public async Task RemoveMemberAsync(Guid memberId)
        {
            await _memberRepository.DeleteAsync(memberId);
        }

        [HttpGet("/api/app/project/{projectId}/tasks")]
        public async Task<List<TaskItem>> GetTasksByProjectAsync(Guid projectId)
        {
            var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == projectId);
            return tasks;
        }
    }
}