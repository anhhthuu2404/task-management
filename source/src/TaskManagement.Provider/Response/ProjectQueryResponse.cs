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
}