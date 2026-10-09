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

            // Sử dụng câu lệnh SQL trực tiếp thay vì gọi Store Procedure để loại bỏ triệt để việc lệch thứ tự cột tại SQL Server
            var query = @"
                INSERT INTO Notifications (Id, UserId, Message, MessageEn, IsRead, CreationTime, CreatorId, TaskId)
                VALUES (@Id, @UserId, @Message, @MessageEn, @IsRead, @CreationTime, @CreatorId, @TaskId)";

            var parameters = new
            {
                Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id,
                UserId = input.UserId,
                Message = input.Message,
                MessageEn = input.MessageEn,
                IsRead = input.IsRead,
                CreationTime = input.CreationTime == default ? DateTime.Now : input.CreationTime,
                CreatorId = input.CreatorId,
                TaskId = input.TaskId
            };

            await connection.ExecuteAsync(query, parameters);
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

        public async Task MarkAllAsReadByUserIdAsync(Guid userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            await connection.ExecuteAsync(
                "sp_Notification_MarkAllAsReadByUserId",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task MarkAsDeletedAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_Notification_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}