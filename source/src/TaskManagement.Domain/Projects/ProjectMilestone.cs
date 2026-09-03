using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Projects
{

    public class ProjectMilestone : AuditedEntity<Guid>
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public Guid? AssigneeUserId { get; set; }
        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    }
}