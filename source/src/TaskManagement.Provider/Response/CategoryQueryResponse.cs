using System;

namespace TaskManagement.Provider.Response;

public class CategoryQueryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
    public long TotalCount { get; set; }
}