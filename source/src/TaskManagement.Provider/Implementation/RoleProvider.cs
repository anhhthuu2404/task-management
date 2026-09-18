using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using TaskManagement.Provider.Interface;
using TaskManagement.Roles; // Đã đổi sang namespace chứa RoleDto chuẩn

namespace TaskManagement.Provider.Implementation
{
    public class RoleProvider : IRoleProvider
    {
        private readonly IRepository<IdentityRole, Guid> _roleRepository;

        public RoleProvider(IRepository<IdentityRole, Guid> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<PagedResultDto<RoleDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting)
        {
            var dbContext = await _roleRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            var items = new List<RoleDto>();
            int totalCount = 0;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Role_GetList] @Filter, @SkipCount, @MaxResultCount, @Sorting";
                command.Parameters.Add(new SqlParameter("@Filter", (object)filter ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@SkipCount", skipCount));
                command.Parameters.Add(new SqlParameter("@MaxResultCount", maxResultCount));
                command.Parameters.Add(new SqlParameter("@Sorting", (object)sorting ?? "CreationTime DESC"));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dto = new RoleDto
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("TenantId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            NormalizedName = reader.GetString(reader.GetOrdinal("NormalizedName")),
                            IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                            IsStatic = reader.GetBoolean(reader.GetOrdinal("IsStatic")),
                            IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                            EntityVersion = reader.GetInt32(reader.GetOrdinal("EntityVersion")),
                            CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                        };

                        totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                        items.Add(dto);
                    }
                }
            }

            return new PagedResultDto<RoleDto>(totalCount, items);
        }

        public async Task<RoleDto?> GetByIdAsync(Guid id)
        {
            var dbContext = await _roleRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            RoleDto? dto = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Role_GetById] @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        dto = new RoleDto
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("TenantId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            NormalizedName = reader.GetString(reader.GetOrdinal("NormalizedName")),
                            IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                            IsStatic = reader.GetBoolean(reader.GetOrdinal("IsStatic")),
                            IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                            EntityVersion = reader.GetInt32(reader.GetOrdinal("EntityVersion")),
                            CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                        };
                    }
                }
            }

            return dto;
        }

        public async Task CreateAsync(RoleDto input)
        {
            var dbContext = await _roleRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Role_Create] @Id, @TenantId, @Name, @NormalizedName, @IsDefault, @IsStatic, @IsPublic";
                command.Parameters.Add(new SqlParameter("@Id", input.Id == Guid.Empty ? Guid.NewGuid() : input.Id));
                command.Parameters.Add(new SqlParameter("@TenantId", (object)input.TenantId ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Name", input.Name));
                command.Parameters.Add(new SqlParameter("@NormalizedName", input.Name.ToUpperInvariant()));
                command.Parameters.Add(new SqlParameter("@IsDefault", input.IsDefault));
                command.Parameters.Add(new SqlParameter("@IsStatic", input.IsStatic));
                command.Parameters.Add(new SqlParameter("@IsPublic", input.IsPublic));

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateAsync(Guid id, CreateUpdateRoleDto input)
        {
            var dbContext = await _roleRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Role_Update] @Id, @Name, @NormalizedName, @IsDefault, @IsPublic";
                command.Parameters.Add(new SqlParameter("@Id", id));
                command.Parameters.Add(new SqlParameter("@Name", input.Name));
                command.Parameters.Add(new SqlParameter("@NormalizedName", input.Name.ToUpperInvariant()));
                command.Parameters.Add(new SqlParameter("@IsDefault", input.IsDefault));
                command.Parameters.Add(new SqlParameter("@IsPublic", input.IsPublic));

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var dbContext = await _roleRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Role_Delete] @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}