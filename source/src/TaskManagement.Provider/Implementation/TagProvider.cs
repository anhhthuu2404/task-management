using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using TaskManagement.Tags;
using TaskManagement.Provider.Interface;
using Volo.Abp;

namespace TaskManagement.Provider.Implementation
{
    public class TagProvider(IRepository<Tag, Guid> tagRepository) : ITagProvider
    {
        private readonly IRepository<Tag, Guid> _tagRepository = tagRepository;

        public async Task<PagedResultDto<TagDto>> GetListAsync(GetTagListInput input)
        {
            var dbContext = await _tagRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            var items = new List<TagDto>();
            int totalCount = 0;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Tag_GetList] @Filter, @CategoryId, @SkipCount, @MaxResultCount, @Sorting";
                command.Parameters.Add(new SqlParameter("@Filter", (object?)input.Filter ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@CategoryId", input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty ? (object)input.CategoryId.Value : DBNull.Value));
                command.Parameters.Add(new SqlParameter("@SkipCount", input.SkipCount));
                command.Parameters.Add(new SqlParameter("@MaxResultCount", input.MaxResultCount));
                command.Parameters.Add(new SqlParameter("@Sorting", (object?)input.Sorting ?? "creationTime DESC"));

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dto = new TagDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        ColorCode = reader.IsDBNull(reader.GetOrdinal("ColorCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ColorCode")),
                        CategoryId = reader.IsDBNull(reader.GetOrdinal("CategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("CategoryId")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                        CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("CreatorId"))) dto.CreatorId = reader.GetGuid(reader.GetOrdinal("CreatorId"));
                    if (!reader.IsDBNull(reader.GetOrdinal("LastModificationTime"))) dto.LastModificationTime = reader.GetDateTime(reader.GetOrdinal("LastModificationTime"));
                    if (!reader.IsDBNull(reader.GetOrdinal("LastModifierId"))) dto.LastModifierId = reader.GetGuid(reader.GetOrdinal("LastModifierId"));

                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    items.Add(dto);
                }
            }

            return new PagedResultDto<TagDto>(totalCount, items);
        }

        public async Task<TagDto> GetByIdAsync(Guid id)
        {
            var dbContext = await _tagRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            TagDto? dto = null;
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC [dbo].[Sp_Tag_GetById] @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    dto = new TagDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        ColorCode = reader.IsDBNull(reader.GetOrdinal("ColorCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ColorCode")),
                        CategoryId = reader.IsDBNull(reader.GetOrdinal("CategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("CategoryId")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                        CreationTime = reader.GetDateTime(reader.GetOrdinal("CreationTime"))
                    };
                }
            }

            return dto ?? throw new UserFriendlyException("Không tìm thấy thẻ.");
        }

        public async Task CreateAsync(TagDto input, Guid? creatorId)
        {
            var dbContext = await _tagRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_Tag_Create] @Id, @Name, @ColorCode, @CategoryId, @CreationTime, @CreatorId";
            command.Parameters.Add(new SqlParameter("@Id", input.Id == Guid.Empty ? Guid.NewGuid() : input.Id));
            command.Parameters.Add(new SqlParameter("@Name", input.Name));
            command.Parameters.Add(new SqlParameter("@ColorCode", (object?)input.ColorCode ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@CategoryId", input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty ? (object)input.CategoryId.Value : DBNull.Value));
            command.Parameters.Add(new SqlParameter("@CreationTime", DateTime.UtcNow));
            command.Parameters.Add(new SqlParameter("@CreatorId", (object?)creatorId ?? DBNull.Value));

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Guid id, CreateUpdateTagDto input, Guid? modifierId)
        {
            var dbContext = await _tagRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_Tag_Update] @Id, @Name, @ColorCode, @CategoryId, @LastModificationTime, @LastModifierId";
            command.Parameters.Add(new SqlParameter("@Id", id));
            command.Parameters.Add(new SqlParameter("@Name", input.Name));
            command.Parameters.Add(new SqlParameter("@ColorCode", (object?)input.ColorCode ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@CategoryId", input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty ? (object)input.CategoryId.Value : DBNull.Value));
            command.Parameters.Add(new SqlParameter("@LastModificationTime", DateTime.UtcNow));
            command.Parameters.Add(new SqlParameter("@LastModifierId", (object?)modifierId ?? DBNull.Value));

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? deleterId)
        {
            var dbContext = await _tagRepository.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC [dbo].[Sp_Tag_Delete] @Id, @DeleterId, @DeletionTime";
            command.Parameters.Add(new SqlParameter("@Id", id));
            command.Parameters.Add(new SqlParameter("@DeleterId", (object?)deleterId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@DeletionTime", DateTime.UtcNow));

            await command.ExecuteNonQueryAsync();
        }
    }
}