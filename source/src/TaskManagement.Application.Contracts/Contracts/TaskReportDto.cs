using System;
using System.Collections.Generic;

namespace TaskManagement.Reports.Dtos
{
    public class TaskReportQueryDto
    {
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class TaskReportItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public Guid? AssignedUserId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public DateTime? DueDate { get; set; }
    }
}