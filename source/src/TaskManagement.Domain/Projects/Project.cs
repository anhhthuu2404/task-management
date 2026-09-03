using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Projects
{
    public class Project : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public Guid? AssigneeUserId { get; set; }

        public virtual ICollection<ProjectMilestone> Milestones { get; set; } = [];
        public virtual ICollection<ProjectMember> Members { get; set; } = [];
    }
}