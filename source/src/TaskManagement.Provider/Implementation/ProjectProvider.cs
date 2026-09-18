using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Provider.Interface;
using TaskManagement.Provider.Request;
using TaskManagement.Provider.Response;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Provider.Implementation
{
    public class ProjectProvider : IProjectProvider, ITransientDependency
    {
        private readonly string _connectionString;

        public ProjectProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
        }

        public async Task<List<ProjectQueryResponse>> GetListAsync(ProjectGetListRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Filter", request.Filter);
            parameters.Add("@Status", request.Status);
            parameters.Add("@DepartmentId", request.DepartmentId);
            parameters.Add("@CategoryId", request.CategoryId);
            parameters.Add("@SkipCount", request.SkipCount);
            parameters.Add("@MaxResultCount", request.MaxResultCount);

            var result = await connection.QueryAsync<ProjectQueryResponse>(
                "sp_Project_GetList",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<ProjectQueryResponse?> GetByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var result = await connection.QueryFirstOrDefaultAsync<ProjectQueryResponse>(
                "sp_Project_GetById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task CreateAsync(ProjectQueryResponse input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", input.Id == Guid.Empty ? Guid.NewGuid() : input.Id);
            parameters.Add("@Name", input.Name);
            parameters.Add("@Description", input.Description);
            parameters.Add("@StartDate", input.StartDate);
            parameters.Add("@EndDate", input.EndDate);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DepartmentId", input.DepartmentId);
            parameters.Add("@CategoryId", input.CategoryId);

            await connection.ExecuteAsync(
                "sp_Project_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateAsync(ProjectQueryResponse input)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", input.Id);
            parameters.Add("@Name", input.Name);
            parameters.Add("@Description", input.Description);
            parameters.Add("@StartDate", input.StartDate);
            parameters.Add("@EndDate", input.EndDate);
            parameters.Add("@Status", input.Status);
            parameters.Add("@DepartmentId", input.DepartmentId);
            parameters.Add("@CategoryId", input.CategoryId);

            await connection.ExecuteAsync(
                "sp_Project_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            await connection.ExecuteAsync(
                "sp_Project_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}