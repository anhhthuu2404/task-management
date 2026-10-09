using Dapper;
using GTranslate.Translators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using Volo.Abp.Localization;

namespace TaskManagement.Reports
{
    [Route("api/app/report")]
    public class ReportAppService : ApplicationService, IApplicationService
    {
        private readonly IDbContextProvider<TaskManagementDbContext> _dbContextProvider;
        private readonly ITranslator _translator;

        public ReportAppService(
            IDbContextProvider<TaskManagementDbContext> dbContextProvider,
            ITranslator translator)
        {
            _dbContextProvider = dbContextProvider;
            _translator = translator;
        }

        [HttpGet("get-task-report")]
        public async Task<List<TaskReportItemDto>> GetTaskReportAsync([FromQuery] TaskReportQueryDto input)
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();

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

            var reportList = result.AsList();

            // Lấy mã ngôn ngữ hiện tại của hệ thống (ví dụ: "vi" hoặc "en")
            var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            // Chỉ tiến hành dịch khi ngôn ngữ giao diện đang là tiếng Anh ("en")
            if (currentCulture.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in reportList)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item.Title))
                        {
                            var translationResult = await _translator.TranslateAsync(item.Title, "en");
                            item.Title = translationResult.Translation;
                        }

                        if (!string.IsNullOrEmpty(item.Status))
                        {
                            var translationResult = await _translator.TranslateAsync(item.Status, "en");
                            item.Status = translationResult.Translation;
                        }

                        if (!string.IsNullOrEmpty(item.ProjectName))
                        {
                            var translationResult = await _translator.TranslateAsync(item.ProjectName, "en");
                            item.ProjectName = translationResult.Translation;
                        }
                    }
                    catch
                    {
                        // Fallback an toàn nếu dịch vụ dịch gặp sự cố
                    }
                }
            }

            return reportList;
        }
    }
}