using System;

namespace TaskManagement.Provider.Response
{
    public class ProjectQueryResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int TotalCount { get; set; }
    }

    public class ProjectMilestoneResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public int Status { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public Guid? LastModifierId { get; set; }
        public Guid? AssigneeUserId { get; set; }
    }

    public class ProjectMemberResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Email { get; set; }
    }
}