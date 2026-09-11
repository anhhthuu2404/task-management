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
      this.departments = res.departments.items || res.departments || [];
      this.projects = res.projects.items || res.projects || [];
    });
  }

  // Tự động lọc danh sách nhân sự khi người dùng thay đổi Phòng ban
  onDepartmentChange(): void {
    this.filter.employeeId = null; // Reset lại nhân sự khi đổi phòng ban
    if (!this.filter.departmentId) {
      this.employees = [...this.allEmployees];
      return;
    }
    const selectedDept = this.departments.find(d => d.id === this.filter.departmentId);
    if (selectedDept && selectedDept.members) {
      const memberUserIds = selectedDept.members.map((m: any) => m.userId);
      this.employees = this.allEmployees.filter(e => memberUserIds.includes(e.id));
    } else {
      this.employees = [];
    }
  }

 onSearchReport(): void {
    this.isLoading = true;
    
    const cleanParams: Record<string, any> = {};
    Object.keys(this.filter).forEach(key => {
      const val = (this.filter as any)[key];
      if (val !== null && val !== undefined && val !== '') {
        cleanParams[key] = val;
      }
    });

    this.httpClient.get<any[]>('/api/app/report/get-task-report', { params: cleanParams }).pipe(
      catchError(() => of([]))
    ).subscribe(data => {
      this.reportData = Array.isArray(data) ? data : [];
      this.isLoading = false;
    });
  }
  exportToExcel(): void {
    if (this.reportData.length === 0) return;
    let csv = "\ufeffSTT,Tên Công Việc,Dự Án,Người Thực Hiện,Trạng Thái,Tiến Độ,Hạn Hoàn Thành\n";
    this.reportData.forEach((item, index) => {
      csv += `${index + 1},"${item.title}","${item.projectName || ''}","${item.assigneeName || ''}",${item.status},${item.progressPercent}%,${item.dueDate ? item.dueDate.slice(0,10) : ''}\n`;
    });
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `Task_Report_${new Date().toISOString().slice(0,10)}.csv`;
    link.click();
  }
}