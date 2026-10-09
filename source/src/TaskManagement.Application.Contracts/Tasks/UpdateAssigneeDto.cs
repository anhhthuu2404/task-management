using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Tasks.Dtos
{
    public class UpdateAssigneeDto : IHasConcurrencyStamp
    {
        [Required]
        public Guid TaskId { get; set; }

        public Guid? AssigneeId { get; set; }

        public string? AssigneeName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}