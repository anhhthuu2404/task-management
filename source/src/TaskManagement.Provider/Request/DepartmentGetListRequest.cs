using System;

namespace TaskManagement.Provider.Request;

public class DepartmentGetListRequest
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}