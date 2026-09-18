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
using TaskManagement.Users;

namespace TaskManagement.Provider.Implementation
{
    public class UserProvider(IRepository<IdentityUser, Guid> userRepository) : IUserProvider
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository = userRepository;

        public async Task<PagedResultDto<UserDto>> GetListAsync(string filter, int skipCount, int maxResultCount, string sorting)
        {
            var dbContext = await _userRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            var items = new List<UserDto>();
            int totalCount = 0;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_User_GetList] @Filter, @SkipCount, @MaxResultCount, @Sorting";
                command.Parameters.Add(new SqlParameter("@Filter", (object)filter ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@SkipCount", skipCount));
                command.Parameters.Add(new SqlParameter("@MaxResultCount", maxResultCount));
                command.Parameters.Add(new SqlParameter("@Sorting", (object)sorting ?? "CreationTime DESC"));

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dto = new UserDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
                        Surname = reader.IsDBNull(reader.GetOrdinal("Surname")) ? null : reader.GetString(reader.GetOrdinal("Surname")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                    };

                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    items.Add(dto);
                }
            }

            return new PagedResultDto<UserDto>(totalCount, items);
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var dbContext = await _userRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            UserDto? dto = null;
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_User_GetById] @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    dto = new UserDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
                        Surname = reader.IsDBNull(reader.GetOrdinal("Surname")) ? null : reader.GetString(reader.GetOrdinal("Surname")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                    };
                }
            }

            return dto;
        }

        public async Task CreateAsync(CreateUpdateUserDto input)
        {
            var dbContext = await _userRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_User_Create] @Id, @UserName, @Email, @Name, @Surname, @IsActive";
            command.Parameters.Add(new SqlParameter("@Id", Guid.NewGuid()));
            command.Parameters.Add(new SqlParameter("@UserName", input.UserName));
            command.Parameters.Add(new SqlParameter("@Email", input.Email));
            command.Parameters.Add(new SqlParameter("@Name", (object?)input.Name ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Surname", (object?)input.Surname ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@IsActive", input.IsActive));

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Guid id, CreateUpdateUserDto input)
        {
            var dbContext = await _userRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_User_Update] @Id, @UserName, @Email, @Name, @Surname, @IsActive";
            command.Parameters.Add(new SqlParameter("@Id", id));
            command.Parameters.Add(new SqlParameter("@UserName", input.UserName));
            command.Parameters.Add(new SqlParameter("@Email", input.Email));
            command.Parameters.Add(new SqlParameter("@Name", (object?)input.Name ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Surname", (object?)input.Surname ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@IsActive", input.IsActive));

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var dbContext = await _userRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_User_Delete] @Id";
            command.Parameters.Add(new SqlParameter("@Id", id));

            await command.ExecuteNonQueryAsync();
        }
    }
}