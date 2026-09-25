using Dapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using TaskManagement.EntityFrameworkCore;
using TaskManagement.Reports.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Reports
{
    [Route("api/app/report")]
    public class ReportAppService : ApplicationService, IApplicationService
    {
        private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;

        public ReportAppService(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        [HttpGet("get-task-report")]
        public async Task<List<TaskReportItemDto>> GetTaskReportAsync([FromQuery] TaskReportQueryDto input)
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();

            // Sửa lại cách lấy connection bằng RelationalDatabaseFacadeExtensions
            var connection = RelationalDatabaseFacadeExtensions.GetDbConnection(dbContext.Database);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var parameters = new DynamicParameters();
            parameters.Add("@ProjectId", input.ProjectId);
            parameters.Add("@EmployeeId", input.EmployeeId);
            parameters.Add("@FromDate", input.FromDate);
            parameters.Add("@ToDate", input.ToDate);

            Guid? departmentIdToQuery = input.DepartmentId;
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