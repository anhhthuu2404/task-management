using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects
{
    public class ProjectDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string DepartmentName { get; set; }

        public int MemberCount { get; set; }
        public int MilestoneCount { get; set; }
    }

    public class CreateUpdateProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class ProjectListFilterDto : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; }
        public string? Status { get; set; }
        public Guid? DepartmentId { get; set; }
    }

    public class MilestoneDto : EntityDto<Guid>
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public Guid? AssigneeUserId { get; set; }
    }

    public class CreateUpdateMilestoneDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public Guid? AssigneeUserId { get; set; }
    }

    public class ProjectMemberDto : EntityDto<Guid>
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Email { get; set; }
    }

    public class AddProjectMemberDto
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}