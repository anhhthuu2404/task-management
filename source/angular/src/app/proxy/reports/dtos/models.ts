
export interface TaskReportItemDto {
  id?: string;
  title?: string;
  projectId?: string;
  projectName?: string;
  assignedUserId?: string;
  assigneeName?: string;
  departmentId?: string;
  departmentName?: string;
  status?: string;
  progressPercent?: number;
  dueDate?: string;
}

export interface TaskReportQueryDto {
  employeeId?: string;
  departmentId?: string;
  projectId?: string;
  fromDate?: string;
  toDate?: string;
}
