using System;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Tasks.Dtos
{
    public class UpdateTaskScheduleDto : IHasConcurrencyStamp
    {
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}