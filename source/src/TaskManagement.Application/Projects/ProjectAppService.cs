using GTranslate.Translators;
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
using TaskManagement.Provider.Response;
using TaskManagement.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.Localization;

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
        private readonly IProjectProvider _projectProvider;
        private readonly INotificationAppService _notificationAppService;
        private readonly INotificationNotifier _notificationNotifier;
        private readonly ITranslator _translator;

        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<ProjectMilestone, Guid> milestoneRepository,
            IRepository<ProjectMember, Guid> memberRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<Notification, Guid> notificationRepository,
            IDistributedEventBus distributedEventBus,
            IProjectProvider projectProvider,
            INotificationAppService notificationAppService,
            INotificationNotifier notificationNotifier,
            ITranslator translator) : base(repository)
        {
            _milestoneRepository = milestoneRepository;
            _memberRepository = memberRepository;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _notificationRepository = notificationRepository;
            _distributedEventBus = distributedEventBus;
            _projectProvider = projectProvider;
            _notificationAppService = notificationAppService;
            _notificationNotifier = notificationNotifier;
            _translator = translator;
        }

        // --- XỬ LÝ TỰ ĐỘNG DỊCH CHO PROJECT (CREATE) ---
        public override async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
        {
            if (string.IsNullOrWhiteSpace(input.NameEn) || input.NameEn == input.Name)
            {
                try
                {
                    var translation = await _translator.TranslateAsync(input.Name, "en", "vi");
                    input.NameEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Name;
                }
                catch
                {
                    input.NameEn = input.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(input.DescriptionEn) || input.DescriptionEn == input.Description)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(input.Description))
                    {
                        var translation = await _translator.TranslateAsync(input.Description, "en", "vi");
                        input.DescriptionEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Description;
                    }
                }
                catch
                {
                    input.DescriptionEn = input.Description;
                }
            }

            return await base.CreateAsync(input);
        }

        // --- XỬ LÝ TỰ ĐỘNG DỊCH CHO PROJECT (UPDATE) ---
        public override async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
        {
            if (string.IsNullOrWhiteSpace(input.NameEn) || input.NameEn == input.Name)
            {
                try
                {
                    var translation = await _translator.TranslateAsync(input.Name, "en", "vi");
                    input.NameEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Name;
                }
                catch
                {
                    input.NameEn = input.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(input.DescriptionEn) || input.DescriptionEn == input.Description)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(input.Description))
                    {
                        var translation = await _translator.TranslateAsync(input.Description, "en", "vi");
                        input.DescriptionEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Description;
                    }
                }
                catch
                {
                    input.DescriptionEn = input.Description;
                }
            }

            return await base.UpdateAsync(id, input);
        }

        // --- XỬ LÝ LẤY DANH SÁCH & TỰ ĐỘNG CHUYỂN NGÔN NGỮ HIỂN THỊ (PROJECT) ---
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

            var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < dtos.Count; i++)
                {
                    var originalItem = data.ElementAtOrDefault(i);
                    if (originalItem != null)
                    {
                        var nameEnVal = originalItem.GetType().GetProperty("NameEn")?.GetValue(originalItem) as string;
                        if (!string.IsNullOrWhiteSpace(nameEnVal))
                        {
                            dtos[i].Name = nameEnVal;
                        }

                        var descEnVal = originalItem.GetType().GetProperty("DescriptionEn")?.GetValue(originalItem) as string;
                        if (!string.IsNullOrWhiteSpace(descEnVal))
                        {
                            dtos[i].Description = descEnVal;
                        }
                    }
                }
            }

            return new PagedResultDto<ProjectDto>(totalCount, dtos);
        }

        // --- LẤY DANH SÁCH MILESTONE (HỖ TRỢ ĐỔI NGÔN NGỮ HIỂN THỊ) ---
        [HttpGet("/api/app/project/milestones/{projectId}")]
        public async Task<List<MilestoneDto>> GetMilestonesByProjectAsync(Guid projectId)
        {
            var data = await _projectProvider.GetMilestonesByProjectIdAsync(projectId);
            var dtos = ObjectMapper.Map<List<ProjectMilestoneResponse>, List<MilestoneDto>>(data);

            var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < dtos.Count; i++)
                {
                    var originalItem = data.ElementAtOrDefault(i);
                    if (originalItem != null)
                    {
                        var titleEnVal = originalItem.GetType().GetProperty("TitleEn")?.GetValue(originalItem) as string;
                        if (!string.IsNullOrWhiteSpace(titleEnVal))
                        {
                            dtos[i].Title = titleEnVal;
                        }

                        var descEnVal = originalItem.GetType().GetProperty("DescriptionEn")?.GetValue(originalItem) as string;
                        if (!string.IsNullOrWhiteSpace(descEnVal))
                        {
                            dtos[i].Description = descEnVal;
                        }
                    }
                }
            }

            return dtos;
        }

        // --- TẠO MỚI MILESTONE (TỰ ĐỘNG DỊCH TITLE & DESCRIPTION) ---
        [HttpPost("/api/app/project/milestone/{projectId}")]
        public async Task<MilestoneDto> CreateMilestoneAsync(Guid projectId, CreateUpdateMilestoneDto input)
        {
            var milestoneId = GuidGenerator.Create();

            // Tự động dịch Title -> TitleEn
            if (string.IsNullOrWhiteSpace(input.TitleEn) || input.TitleEn == input.Title)
            {
                try
                {
                    var translation = await _translator.TranslateAsync(input.Title, "en", "vi");
                    input.TitleEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Title;
                }
                catch
                {
                    input.TitleEn = input.Title;
                }
            }

            // Tự động dịch Description -> DescriptionEn
            if (string.IsNullOrWhiteSpace(input.DescriptionEn) || input.DescriptionEn == input.Description)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(input.Description))
                    {
                        var translation = await _translator.TranslateAsync(input.Description, "en", "vi");
                        input.DescriptionEn = !string.IsNullOrEmpty(translation?.Translation) ? translation.Translation : input.Description;
                    }
                }
                catch
                {
                    input.DescriptionEn = input.Description;
                }
            }

            var milestoneResponse = new ProjectMilestoneResponse
            {
                Id = milestoneId,
                ProjectId = projectId,
                Title = input.Title,
                TitleEn = input.TitleEn,
                Description = input.Description,
                DescriptionEn = input.DescriptionEn,
                DueDate = input.DueDate,
                Status = (int)input.Status,
                AssigneeUserId = input.AssigneeUserId,
                CreatorId = CurrentUser.Id
            };

            await _projectProvider.CreateMilestoneAsync(milestoneResponse);

            // Khởi tạo TaskItem với đầy đủ TitleEn và DescriptionEn được đồng bộ
            var newTask = new TaskItem
            {
                ProjectId = projectId,
                MilestoneId = milestoneId,
                Title = input.Title,
                TitleEn = !string.IsNullOrWhiteSpace(input.TitleEn) ? input.TitleEn : input.Title,
                Description = input.Description,
                DescriptionEn = !string.IsNullOrWhiteSpace(input.DescriptionEn) ? input.DescriptionEn : input.Description,
                DueDate = input.DueDate,
                Status = (TaskItemStatus)(int)input.Status,
                AssigneeId = input.AssigneeUserId
            };

            await _taskRepository.InsertAsync(newTask);

            var members = await _projectProvider.GetMembersByProjectIdAsync(projectId);
            var targetUserIds = members.Select(m => m.UserId).Distinct().ToList();

            if (input.AssigneeUserId.HasValue && !targetUserIds.Contains(input.AssigneeUserId.Value))
            {
                targetUserIds.Add(input.AssigneeUserId.Value);
            }

            if (CurrentUser.Id.HasValue && !targetUserIds.Contains(CurrentUser.Id.Value))
            {
                targetUserIds.Add(CurrentUser.Id.Value);
            }

            try
            {
                var adminUser = await _userRepository.FirstOrDefaultAsync(u => u.UserName == "admin");
                if (adminUser != null && !targetUserIds.Contains(adminUser.Id))
                {
                    targetUserIds.Add(adminUser.Id);
                }
            }
            catch (Exception) { }

            string message = $"Mốc tiến độ / công việc mới '{input.Title}' vừa được thêm vào dự án.";

            foreach (var userId in targetUserIds)
            {
                await _notificationAppService.CreateTaskNotificationAsync(
                    userId,
                    newTask.Id,
                    message
                );

                var notificationPayload = new
                {
                    id = Guid.NewGuid().ToString(),
                    message = message,
                    taskId = newTask.Id,
                    creationTime = DateTime.UtcNow
                };

                await _notificationNotifier.SendClientNotificationAsync(userId, notificationPayload);
            }

            return ObjectMapper.Map<ProjectMilestoneResponse, MilestoneDto>(milestoneResponse);
        }

        [HttpDelete("/api/app/project/milestone/{milestoneId}")]
        public async Task DeleteMilestoneAsync(Guid milestoneId)
        {
            var milestone = await _projectProvider.GetMilestoneByIdAsync(milestoneId);

            if (milestone != null)
            {
                var tasks = await _taskRepository.GetListAsync(t => t.MilestoneId == milestoneId);

                var members = await _projectProvider.GetMembersByProjectIdAsync(milestone.ProjectId);
                var targetUserIds = members.Select(m => m.UserId).Distinct().ToList();

                if (CurrentUser.Id.HasValue && !targetUserIds.Contains(CurrentUser.Id.Value))
                {
                    targetUserIds.Add(CurrentUser.Id.Value);
                }

                try
                {
                    var adminUser = await _userRepository.FirstOrDefaultAsync(u => u.UserName == "admin");
                    if (adminUser != null && !targetUserIds.Contains(adminUser.Id))
                    {
                        targetUserIds.Add(adminUser.Id);
                    }
                }
                catch (Exception) { }

                foreach (var task in tasks)
                {
                    string deleteMessage = $"Công việc '{task.Title}' đã bị xóa khỏi dự án.";

                    foreach (var userId in targetUserIds)
                    {
                        await _notificationAppService.CreateTaskNotificationAsync(
                            userId,
                            task.Id,
                            deleteMessage
                        );

                        var notificationPayload = new
                        {
                            id = Guid.NewGuid().ToString(),
                            message = deleteMessage,
                            taskId = task.Id,
                            creationTime = DateTime.UtcNow
                        };

                        await _notificationNotifier.SendClientNotificationAsync(userId, notificationPayload);
                    }

                    await _taskRepository.DeleteAsync(task.Id);
                }

                await _projectProvider.DeleteMilestoneAsync(milestoneId);
            }
        }

        [HttpGet("/api/app/project/by-project/{projectId}/members")]
        public async Task<ListResultDto<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            try
            {
                var members = await _projectProvider.GetMembersByProjectIdAsync(projectId);

                var resultList = members.Select(m => new ProjectMemberDto
                {
                    Id = m.Id,
                    ProjectId = m.ProjectId,
                    UserId = m.UserId,
                    Role = m.Role,
                    UserName = m.UserName,
                    Name = m.Name,
                    Surname = m.Surname,
                    Email = m.Email
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
            var memberResponse = new ProjectMemberResponse
            {
                Id = GuidGenerator.Create(),
                ProjectId = projectId,
                UserId = input.UserId,
                Role = input.Role
            };

            await _projectProvider.AddMemberAsync(memberResponse);
            return ObjectMapper.Map<ProjectMemberResponse, ProjectMemberDto>(memberResponse);
        }

        [HttpDelete("/api/app/project/member/{memberId}")]
        public async Task RemoveMemberAsync(Guid memberId)
        {
            await _projectProvider.RemoveMemberAsync(memberId);
        }

        [HttpGet("/api/app/project/{projectId}/tasks")]
        public async Task<List<TaskItem>> GetTasksByProjectAsync(Guid projectId)
        {
            var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == projectId);
            return tasks;
        }
    }
}