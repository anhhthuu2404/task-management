using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Reports.Dtos;

namespace TaskManagement.Reports.Interface
{
    public interface IReportProvider
    {
        Task<List<TaskReportItemDto>> GetTaskReportAsync(TaskReportQueryDto input);
    }
}