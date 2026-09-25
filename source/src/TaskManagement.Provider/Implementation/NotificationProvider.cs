using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Response;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Provider.Implementation
{
    public class NotificationProvider : INotificationProvider, ITransientDependency
    {
        private readonly string _connectionString;

        public NotificationProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
        }

        public async Task<List<NotificationQueryResponse>> GetByUserIdAsync(Guid userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            var result = await connection.QueryAsync<NotificationQueryResponse>(
                "sp_Notification_GetByUserId",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task CreateAsync(NotificationQueryResponse input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", input.Id == Guid.Empty ? Guid.NewGuid() : input.Id);
            parameters.Add("@UserId", input.UserId);
            parameters.Add("@Message", input.Message);
            parameters.Add("@IsRead", input.IsRead);
            parameters.Add("@CreationTime", input.CreationTime == default ? DateTime.Now : input.CreationTime);
            parameters.Add("@CreatorId", input.CreatorId);
            parameters.Add("@TaskId", input.TaskId);

            await connection.ExecuteAsync(
                "sp_Notification_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_Notification_MarkAsRead",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}