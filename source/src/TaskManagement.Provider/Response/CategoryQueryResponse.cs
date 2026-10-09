using System;

namespace TaskManagement.Provider.Response;

public class CategoryQueryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string NameVi { get; set; } = string.Empty; // Bổ sung
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionVi { get; set; } // Bổ sung
    public string? DescriptionEn { get; set; }
    public string? ColorCode { get; set; }
    public DateTime CreationTime { get; set; }
    public long TotalCount { get; set; }
}