using System;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Projects
{
    public class ProjectMember : Entity<Guid>
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; } // Liên kết với bảng User hệ thống ABP
        public string Role { get; set; } = "Member"; // ProjectManager, Developer, Viewer...
    }
}