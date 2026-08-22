using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class GetTaskListInputDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public TaskPriority? Priority { get; set; }
    public TaskItemStatus? Status { get; set; }
    public bool OnlyMyTasks { get; set; } = false;
}