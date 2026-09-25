using System;
using System.Collections.Generic;

namespace TaskManagement.Provider.Request
{
    public class TaskReportGetListRequest
    {
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime? FromDate { get; set; }
        public List<Guid>? DepartmentIds { get; set; }
        public DateTime? ToDate { get; set; }
    }
}