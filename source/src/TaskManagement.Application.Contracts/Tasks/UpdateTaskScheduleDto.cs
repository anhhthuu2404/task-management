using System;

namespace TaskManagement.Tasks.Dtos
{
    public class UpdateTaskScheduleDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}