using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Projects
{
    public class ProjectMilestone : AuditedEntity<Guid>
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;       // Tiêu đề cột mốc (Tiếng Việt)
        public string? TitleEn { get; set; }                   // Tiêu đề cột mốc (Tiếng Anh tự động dịch)
        public string? Description { get; set; }               // Mô tả (Tiếng Việt)
        public string? DescriptionEn { get; set; }             // Mô tả (Tiếng Anh tự động dịch)
        public DateTime DueDate { get; set; }
        public Guid? AssigneeUserId { get; set; }
        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    }
}