using System;
using System.Collections.Generic;
using TaskManagement.Categories;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Projects
{
    public class Project : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;       // Tên dự án (Tiếng Việt)
        public string? NameEn { get; set; }                    // Tên dự án (Tiếng Anh tự động dịch)
        public Guid? DepartmentId { get; set; }
        public string? Description { get; set; }               // Mô tả (Tiếng Việt)
        public string? DescriptionEn { get; set; }             // Mô tả (Tiếng Anh tự động dịch)
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
        public string Status { get; set; } = "Active";
        public Guid? AssigneeUserId { get; set; }

        public virtual ICollection<ProjectMilestone> Milestones { get; set; } = [];
        public virtual ICollection<ProjectMember> Members { get; set; } = [];
    }
}