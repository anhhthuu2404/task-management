using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Departments;
using TaskManagement.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

public class DepartmentProvider : IDepartmentProvider, ITransientDependency
{
    private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;

    public DepartmentProvider(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    private async Task<(TaskManagementDbContext DbContext, DbConnection Connection)> GetConnectionAndContextAsync()
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return (dbContext, connection);
    }

    private void AssignTransaction(DbCommand command, TaskManagementDbContext dbContext)
    {
        if (dbContext.Database.CurrentTransaction != null)
        {
            command.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
        }
    }

    public async Task<PagedResultDto<DepartmentDto>> GetListAsync(GetDepartmentListDto input)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        var list = new List<DepartmentDto>();
        long totalCount = 0;

        await using (var command = connection.CreateCommand())
        {
            AssignTransaction(command, dbContext);
            command.CommandText = "Sp_Department_GetList";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Filter", (object?)input.Filter ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@IsActive", (object?)input.IsActive ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@SkipCount", input.SkipCount));
            command.Parameters.Add(new SqlParameter("@MaxResultCount", input.MaxResultCount));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DepartmentDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentId")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                });

                if (totalCount == 0)
                {
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                }
            }
        }

        return new PagedResultDto<DepartmentDto>(totalCount, list);
    }

    public async Task<DepartmentDto> GetByIdAsync(Guid id)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        DepartmentDto? dto = null;
        await using (var command = connection.CreateCommand())
        {
            AssignTransaction(command, dbContext);
            command.CommandText = "Sp_Department_GetById";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Id", id));

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dto = new DepartmentDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentId")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }
        }
        return dto ?? throw new Volo.Abp.UserFriendlyException("Không tìm thấy phòng ban!");
    }

    public async Task CreateAsync(CreateUpdateDepartmentDto input, Guid id, Guid? creatorId)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using var command = connection.CreateCommand();
        AssignTransaction(command, dbContext);
        command.CommandText = "Sp_Department_Create";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Id", id));
        command.Parameters.Add(new SqlParameter("@Code", input.Code));
        command.Parameters.Add(new SqlParameter("@Name", input.Name));
        command.Parameters.Add(new SqlParameter("@Description", (object?)input.Description ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ParentId", (object?)input.ParentId ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@IsActive", input.IsActive));
        command.Parameters.Add(new SqlParameter("@CreatorId", (object?)creatorId ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ExtraProperties", "{}"));
        command.Parameters.Add(new SqlParameter("@ConcurrencyStamp", Guid.NewGuid().ToString("N")));
        command.Parameters.Add(new SqlParameter("@CreationTime", DateTime.Now));

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Guid id, CreateUpdateDepartmentDto input, Guid? modifierId)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using var command = connection.CreateCommand();
        AssignTransaction(command, dbContext);
        command.CommandText = "Sp_Department_Update";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Id", id));
        command.Parameters.Add(new SqlParameter("@Code", input.Code));
        command.Parameters.Add(new SqlParameter("@Name", input.Name));
        command.Parameters.Add(new SqlParameter("@Description", (object?)input.Description ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ParentId", (object?)input.ParentId ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@IsActive", input.IsActive));
        command.Parameters.Add(new SqlParameter("@LastModifierId", (object?)modifierId ?? DBNull.Value));

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid id, Guid? deleterId)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using var command = connection.CreateCommand();
        AssignTransaction(command, dbContext);
        command.CommandText = "Sp_Department_Delete";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Id", id));
        command.Parameters.Add(new SqlParameter("@DeleterId", (object?)deleterId ?? DBNull.Value));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        var list = new List<DepartmentTreeDto>();
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using (var command = connection.CreateCommand())
        {
            AssignTransaction(command, dbContext);
            command.CommandText = "Sp_Department_GetList";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@SkipCount", 0));
            command.Parameters.Add(new SqlParameter("@MaxResultCount", 1000));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DepartmentTreeDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentId")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    Children = []
                });
            }
        }

        var lookup = list.ToLookup(x => x.ParentId);
        List<DepartmentTreeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId].Select(node =>
            {
                node.Children = BuildTree(node.Id);
                return node;
            }).ToList();
        }

        return BuildTree(null);
    }

    public async Task<List<DepartmentMemberDto>> GetUsersByDepartmentIdAsync(Guid departmentId)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        var members = new List<DepartmentMemberDto>();
        await using (var command = connection.CreateCommand())
        {
            AssignTransaction(command, dbContext);
            command.CommandText = @"
                SELECT u.Id as UserId, u.UserName, u.Email, ud.IsManager 
                FROM UserDepartments ud
                INNER JOIN AbpUsers u ON ud.UserId = u.Id
                WHERE ud.DepartmentId = @DepartmentId";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(new SqlParameter("@DepartmentId", departmentId));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                members.Add(new DepartmentMemberDto
                {
                    UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                    IsManager = reader.GetBoolean(reader.GetOrdinal("IsManager"))
                });
            }
        }
        return members;
    }

    public async Task UpsertUserDepartmentAsync(Guid userId, Guid departmentId, bool isManager)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using var command = connection.CreateCommand();
        AssignTransaction(command, dbContext);
        command.CommandText = @"
            IF EXISTS (SELECT 1 FROM UserDepartments WHERE UserId = @UserId AND DepartmentId = @DepartmentId)
                UPDATE UserDepartments SET IsManager = @IsManager WHERE UserId = @UserId AND DepartmentId = @DepartmentId;
            ELSE
                INSERT INTO UserDepartments (UserId, DepartmentId, IsManager) VALUES (@UserId, @DepartmentId, @IsManager);";
        command.CommandType = CommandType.Text;

        command.Parameters.Add(new SqlParameter("@UserId", userId));
        command.Parameters.Add(new SqlParameter("@DepartmentId", departmentId));
        command.Parameters.Add(new SqlParameter("@IsManager", isManager));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteUserDepartmentAsync(Guid departmentId, Guid userId)
    {
        var (dbContext, connection) = await GetConnectionAndContextAsync();

        await using var command = connection.CreateCommand();
        AssignTransaction(command, dbContext);
        command.CommandText = "DELETE FROM UserDepartments WHERE DepartmentId = @DepartmentId AND UserId = @UserId";
        command.CommandType = CommandType.Text;

        command.Parameters.Add(new SqlParameter("@DepartmentId", departmentId));
        command.Parameters.Add(new SqlParameter("@UserId", userId));

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}