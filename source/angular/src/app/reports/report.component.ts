import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report.component.html'
})
export class ReportComponent implements OnInit {
  filter = { employeeId: null, departmentId: null, projectId: null, fromDate: '', toDate: '' };
  
  allEmployees: any[] = [];
  employees: any[] = [];
  departments: any[] = [];
  projects: any[] = [];
  reportData: any[] = [];
  isLoading = false;

  private _rawDepartments: any[] = [];

  constructor(private httpClient: HttpClient) {}

  ngOnInit(): void {
    this.loadDropdowns();
  }

  loadDropdowns(): void {
    forkJoin({
      users: this.httpClient.get<any>('/api/identity/users').pipe(catchError(() => of({ items: [] }))),
      departments: this.httpClient.get<any>('/api/app/department').pipe(catchError(() => of({ items: [] }))),
      projects: this.httpClient.get<any>('/api/app/project').pipe(catchError(() => of({ items: [] })))
    }).subscribe(res => {
      this.allEmployees = res.users.items || res.users || [];
      this.employees = [...this.allEmployees];
      
      const rawDepts = res.departments.items || res.departments || [];
      this._rawDepartments = Array.isArray(rawDepts) ? rawDepts : [];
      
      // Chỉ giữ lại các phòng ban gốc (nhánh chính: không có parentId hoặc parentId là null/rỗng)
      this.departments = this._rawDepartments.filter((d: any) => !d.parentId && !d.parentDepartmentId);
      
      const rawProjects = res.projects.items || res.projects || [];
      this.projects = Array.isArray(rawProjects) ? rawProjects : [];
    });
  }

  onDepartmentChange(): void {
    this.filter.employeeId = null; // Reset lại nhân sự khi đổi phòng ban
    if (!this.filter.departmentId) {
      this.employees = [...this.allEmployees];
      return;
    }

    const rawDepts = this._rawDepartments || [];
    
    // Tìm tất cả ID phòng ban thuộc nhánh này (gồm phòng ban gốc và các phòng ban con/cháu bên trong)
    const targetDeptIds = new Set<string>();
    const findSubDepartments = (parentId: string) => {
      targetDeptIds.add(parentId);
      const children = rawDepts.filter((d: any) => (d.parentId === parentId || d.parentDepartmentId === parentId));
      children.forEach((child: any) => {
        const childId = child.id || child.Id;
        if (childId) findSubDepartments(childId);
      });
    };

    findSubDepartments(this.filter.departmentId);

    // Gom tất cả nhân sự thuộc phòng ban chính và các nhánh phụ của nó
    const memberUserIds = new Set<string>();
    rawDepts.forEach((d: any) => {
      const dId = d.id || d.Id;
      if (targetDeptIds.has(dId) && d.members) {
        d.members.forEach((m: any) => {
          const uId = m.userId || m.UserId;
          if (uId) memberUserIds.add(uId);
        });
      }
    });

    // Lọc danh sách nhân sự hiển thị trên dropdown
    this.employees = this.allEmployees.filter(e => memberUserIds.has(e.id || e.Id));
  }

  getStatusText(status: any): string {
    switch (status) {
      case 1:
      case 'ToDo':
      case 'Chưa thực hiện':
        return 'Chưa thực hiện';
      case 2:
      case 'InProgress':
      case 'Đang làm':
        return 'Đang làm';
      case 3:
      case 'Completed':
      case 'Hoàn thành':
        return 'Hoàn thành';
      case 4:
      case 'Pending':
      case 'Tạm dừng':
        return 'Tạm dừng';
      case 5:
      case 'Overdue':
      case 'Quá hạn':
        return 'Quá hạn';
      default:
        return status || '---';
    }
  }

  getStatusBadgeClass(status: any): string {
    const text = this.getStatusText(status);
    switch (text) {
      case 'Hoàn thành': return 'bg-success';
      case 'Đang làm': return 'bg-primary';
      case 'Quá hạn': return 'bg-danger';
      case 'Tạm dừng': return 'bg-warning text-dark';
      default: return 'bg-secondary';
    }
  }

onSearchReport(): void {
  this.isLoading = true;
  
  const cleanParams: Record<string, any> = {};
  
  // Xử lý gom nhóm phòng ban con nếu có chọn phòng ban
  if (this.filter.departmentId) {
    const rawDepts = this._rawDepartments || [];
    const targetDeptIds = new Set<string>();
    
    const findSubDepartments = (parentId: string) => {
      targetDeptIds.add(parentId);
      const children = rawDepts.filter((d: any) => (d.parentId === parentId || d.parentDepartmentId === parentId));
      children.forEach((child: any) => {
        const childId = child.id || child.Id;
        if (childId) findSubDepartments(childId);
      });
    };

    findSubDepartments(this.filter.departmentId);
    
    // Truyền danh sách departmentIds thay vì một departmentId đơn lẻ
    cleanParams['departmentIds'] = Array.from(targetDeptIds);
  }

  // Đưa các bộ lọc còn lại vào params
  Object.keys(this.filter).forEach(key => {
    if (key === 'departmentId') return; // Bỏ qua departmentId đơn lẻ vì đã đổi thành departmentIds
    const val = (this.filter as any)[key];
    if (val !== null && val !== undefined && val !== '') {
      cleanParams[key] = val;
    }
  });

  this.httpClient.get<any>('/api/app/report/get-task-report', { params: cleanParams }).pipe(
    catchError(() => of({ items: [] }))
  ).subscribe((res: any) => {
    this.reportData = Array.isArray(res) ? res : (res.items || res.result || []);
    this.isLoading = false;
  });
}

  exportToExcel(): void {
    if (this.reportData.length === 0) return;
    let csv = "\ufeffSTT,Tên Công Việc,Dự Án,Người Thực Hiện,Trạng Thái,Tiến Độ,Hạn Hoàn Thành\n";
    this.reportData.forEach((item, index) => {
      const statusText = this.getStatusText(item.status);
      csv += `${index + 1},"${item.title}","${item.projectName || ''}","${item.assigneeName || ''}","${statusText}",${item.progressPercent}%,${item.dueDate ? item.dueDate.slice(0,10) : ''}\n`;
    });
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `Task_Report_${new Date().toISOString().slice(0,10)}.csv`;
    link.click();
  }
}