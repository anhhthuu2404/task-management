namespace TaskManagement.Tasks.Dtos
{
    public class UpdateTaskStatusDto
    {
        public TaskItemStatus Status { get; set; }
        public int Position { get; set; } 
    }
}