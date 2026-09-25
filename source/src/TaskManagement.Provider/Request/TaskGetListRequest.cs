using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks.Request
{
    public class TaskGetListRequest : PagedAndSortedResultRequestDto
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? AssigneeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public List<Guid>? DepartmentIds { get; set; }
        public int? Priority { get; set; }
        public int? Status { get; set; }
        public bool? OnlyMyTasks { get; set; }
    }
}