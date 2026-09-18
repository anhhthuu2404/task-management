using System;

namespace TaskManagement.Provider.Request
{
    public class ProjectGetListRequest
    {
        public string? Filter { get; set; }
        public string? Status { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? CategoryId { get; set; }
        public int SkipCount { get; set; } = 0;
        public int MaxResultCount { get; set; } = 10;
    }
}