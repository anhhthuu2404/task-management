using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.Reports.Dtos;
using TaskManagement.Reports.Interface;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Reports.Implementation
{
    public class ReportProvider : IReportProvider, ITransientDependency
    {
        private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;

        public ReportProvider(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<List<TaskReportItemDto>> GetTaskReportAsync(TaskReportQueryDto input)
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var parameters = new DynamicParameters();
            parameters.Add("@ProjectId", input.ProjectId);
            parameters.Add("@EmployeeId", input.EmployeeId);
            parameters.Add("@FromDate", input.FromDate);
            parameters.Add("@ToDate", input.ToDate);

            // Xử lý lấy DepartmentId ưu tiên lọc đơn lẻ hoặc phần tử đầu tiên trong danh sách
            System.Guid? departmentIdToQuery = input.DepartmentId;
            if (!departmentIdToQuery.HasValue && input.DepartmentIds != null && input.DepartmentIds.Count > 0)
            {
                departmentIdToQuery = input.DepartmentIds[0];
            }
            parameters.Add("@DepartmentId", departmentIdToQuery);

            var result = await connection.QueryAsync<TaskReportItemDto>(
                "sp_GetTaskReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }
    }
}