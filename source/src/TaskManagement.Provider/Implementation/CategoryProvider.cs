using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using TaskManagement.Categories;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Request;
using TaskManagement.Provider.Response;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Provider.Implementation;

public class CategoryProvider : ICategoryProvider, ITransientDependency
{
    private readonly string _connectionString;

    public CategoryProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<(List<CategoryDto> Items, int TotalCount)> GetPagedListAsync(CategoryGetListRequest request)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var queryResult = await connection.QueryAsync<CategoryQueryResponse>(
            "sp_Categories_GetPagedList",
            new
            {
                Keyword = request.Filter,
                request.SkipCount,
                request.MaxResultCount,
                Sorting = request.Sorting
            },
            commandType: CommandType.StoredProcedure
        );

        var list = queryResult.ToList();
        int totalCount = list.FirstOrDefault() != null ? (int)list.First().TotalCount : 0;

        var items = list.Select(x => new CategoryDto
        {
            Id = x.Id,
            Name = x.Name,
            NameVi = x.NameVi,
            NameEn = x.NameEn,
            Description = x.Description,
            DescriptionVi = x.DescriptionVi,
            DescriptionEn = x.DescriptionEn,
            ColorCode = x.ColorCode
        }).ToList();

        return (items, totalCount);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        using var connection = CreateConnection();
        var response = await connection.QueryFirstOrDefaultAsync<CategoryQueryResponse>(
            "sp_Categories_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );

        if (response == null) return null;

        return new CategoryDto
        {
            Id = response.Id,
            Name = response.Name,
            NameVi = response.NameVi,
            NameEn = response.NameEn,
            Description = response.Description,
            DescriptionVi = response.DescriptionVi,
            DescriptionEn = response.DescriptionEn,
            ColorCode = response.ColorCode
        };
    }

    public async Task InsertAsync(CategoryDto input)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "sp_Categories_Insert",
            new
            {
                input.Id,
                input.Name,
                input.NameVi,
                input.NameEn,
                input.Description,
                input.DescriptionVi,
                input.DescriptionEn,
                input.ColorCode,
                IsActive = true,
                CreationTime = DateTime.Now,
                CreatorId = (Guid?)null,
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task UpdateAsync(Guid id, CategoryDto input)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "sp_Categories_Update",
            new
            {
                Id = id,
                input.Name,
                input.NameVi,
                input.NameEn,
                input.Description,
                input.DescriptionVi,
                input.DescriptionEn,
                input.ColorCode,
                IsActive = true,
                LastModificationTime = DateTime.Now,
                LastModifierId = (Guid?)null,
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task DeleteAsync(Guid id, Guid? deleterId = null)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "sp_Categories_Delete",
            new { Id = id, DeleterId = deleterId, DeletionTime = DateTime.Now },
            commandType: CommandType.StoredProcedure
        );
    }
}