using System;

namespace TaskManagement.Provider.Response;

public class DepartmentQueryResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
    public long TotalCount { get; set; }
}