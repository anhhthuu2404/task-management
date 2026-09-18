using System;

namespace TaskManagement.Provider.Response;

public class RoleQueryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ConcurrencyStamp { get; set; }
    public bool IsStatic { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPublic { get; set; }
    public long TotalCount { get; set; }
}