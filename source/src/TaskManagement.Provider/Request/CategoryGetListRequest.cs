using System;

namespace TaskManagement.Provider.Request;

public class CategoryGetListRequest
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
    public int SkipCount { get; set; } = 0;
    public string? Sorting { get; set; }
    public int MaxResultCount { get; set; } = 10;
}