using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Categories;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Request;
using Volo.Abp.DependencyInjection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
        // Bổ sung dòng khởi tạo connection này nhé:
        using var connection = CreateConnection();
        await connection.OpenAsync(); // Nhớ mở kết nối nếu chưa mở tự động

        using var multi = await connection.QueryMultipleAsync(
            "sp_Categories_GetPagedList",
            new
            {
                Keyword = request.Filter,
                request.SkipCount,
                request.MaxResultCount,
                request.Sorting
            },
            commandType: CommandType.StoredProcedure
        );

        var items = (await multi.ReadAsync<CategoryDto>()).ToList();
        var totalCount = await multi.ReadFirstAsync<int>();

        return (items, totalCount);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CategoryDto>(
            "sp_Categories_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );
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
                input.Description,
                IsActive = true,
                CreationTime = DateTime.Now,
                CreatorId = (Guid?)null
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
                input.Description,
                IsActive = true,
                LastModificationTime = DateTime.Now,
                LastModifierId = (Guid?)null
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