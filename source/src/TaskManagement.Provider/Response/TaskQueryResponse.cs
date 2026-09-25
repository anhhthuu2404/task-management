using System;

namespace TaskManagement.Tasks.Response
{
    public class TaskQueryResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid? AssigneeId { get; set; }

        public string? AssigneeName { get; set; }    
        public string? AssigneeUserName { get; set; } 

        public Guid ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public Guid DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? MilestoneId { get; set; }
        public string? MilestoneName { get; set; }
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsRecurring { get; set; }
        public int? Frequency { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
        public int TotalCount { get; set; }
    }
}