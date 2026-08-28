using System;

namespace TaskManagement.Tasks;

public class TaskLookupDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}