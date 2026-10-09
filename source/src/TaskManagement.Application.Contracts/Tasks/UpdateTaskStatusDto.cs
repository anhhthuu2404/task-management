using Volo.Abp.Domain.Entities;

namespace TaskManagement.Tasks.Dtos
{
    public class UpdateTaskStatusDto : IHasConcurrencyStamp
    {
        public TaskItemStatus Status { get; set; }
        public int Position { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}