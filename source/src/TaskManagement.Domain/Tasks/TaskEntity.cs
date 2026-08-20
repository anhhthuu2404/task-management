using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks;

public class TaskEntity : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }

    protected TaskEntity() { }

    public TaskEntity(Guid id, string title, Guid categoryId, int priority = 1) : base(id)
    {
        Title = title;
        CategoryId = categoryId;
        Priority = priority;
    }
}