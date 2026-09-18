using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Users // Hoặc namespace Contracts tương đương của bạn
{
    public class CreateUpdateUserDto
    {
        [Required]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Name { get; set; }

        [StringLength(64)]
        public string? Surname { get; set; }

        public bool IsActive { get; set; } = true;
    }
}