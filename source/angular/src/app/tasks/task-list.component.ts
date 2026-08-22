import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { RestService, PermissionService } from '@abp/ng.core';
import { NgbPaginationModule, NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

export interface TaskDto {
  id: string;
  title: string;
  description?: string;
  assigneeId?: string;
  assigneeName?: string;
  priority: number;
  status: number;
  progressPercent: number;
  dueDate: string;
  fileUrl?: string;
  fileName?: string;
  attachments?: any[];
}

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NgbPaginationModule,
    NgbDropdownModule
  ],
  templateUrl: './task-list.component.html'
})
export class TaskListComponent implements OnInit {
  private readonly rest = inject(RestService);
  private readonly permission = inject(PermissionService);

  readonly backendUrl = 'https://localhost:44399';

  taskList: TaskDto[] = [];
  totalCount = 0;
  isLoading = false;

  filters = {
    filter: '', 
    categoryId: '',
    assigneeId: '',
    priority: null as number | null,
    status: null as number | null,
    onlyMyTasks: false,
    skipCount: 0,
    maxResultCount: 10,
    sorting: 'CreationTime DESC'
  };

  page = 1;
  categories: any[] = [];
  users: any[] = [];

  readonly canCreate = this.permission.getGrantedPolicy('TaskManagement.Tasks.Create');
  readonly canEdit = this.permission.getGrantedPolicy('TaskManagement.Tasks.Edit');
  readonly canDelete = this.permission.getGrantedPolicy('TaskManagement.Tasks.Delete');

  ngOnInit(): void {
    this.loadCategories();
    this.loadUsers();
    this.fetchTasks();
  }

  fetchTasks(): void {
    this.isLoading = true;
    this.filters.skipCount = (this.page - 1) * this.filters.maxResultCount;

    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/task',
      params: this.cleanParams(this.filters)
    }).subscribe({
      next: (res) => {
        this.taskList = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Lỗi khi tải danh sách công việc:', err);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.page = 1;
    this.fetchTasks();
  }

  toggleMyTasks(): void {
    this.filters.onlyMyTasks = !this.filters.onlyMyTasks;
    this.onSearch();
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
    this.fetchTasks();
  }

  updateTaskStatus(task: TaskDto, newStatus: number): void {
    const oldStatus = task.status;
    const oldProgress = task.progressPercent;

    task.status = newStatus;
    if (newStatus === 2) {
      task.progressPercent = 100;
    }

    this.rest.request<any, TaskDto>({
      method: 'POST',
      url: `/api/app/task/${task.id}/status`,
      params: { status: newStatus }
    }).subscribe({
      error: (err) => {
        task.status = oldStatus;
        task.progressPercent = oldProgress;
        console.error('Lỗi đổi trạng thái:', err);
      }
    });
  }

  updateTaskAssignee(task: TaskDto, assigneeId: string | null): void {
    const selectedUser = this.users.find(u => u.id === assigneeId);
    const oldAssigneeId = task.assigneeId;
    const oldAssigneeName = task.assigneeName;

    task.assigneeId = assigneeId || undefined;
    task.assigneeName = selectedUser ? (selectedUser.name || selectedUser.userName) : 'Chưa phân công';

    const params: any = {};
    if (assigneeId) {
      params.assigneeId = assigneeId;
    }

    this.rest.request<any, TaskDto>({
      method: 'POST',
      url: `/api/app/task/${task.id}/assignee`,
      params: params
    }).subscribe({
      next: (updatedTask) => {
        if (updatedTask && updatedTask.assigneeName) {
          task.assigneeName = updatedTask.assigneeName;
        }
      },
      error: (err) => {
        task.assigneeId = oldAssigneeId;
        task.assigneeName = oldAssigneeName;
        console.error('Lỗi đổi người thực hiện:', err);
      }
    });
  }

  deleteTask(id: string): void {
    if (!confirm('Bạn có chắc chắn muốn xóa công việc này?')) return;

    this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/task/${id}`
    }).subscribe({
      next: () => {
        this.fetchTasks();
      }
    });
  }

  getFileList(task: TaskDto): { name: string; url: string }[] {
    if (task.attachments && task.attachments.length > 0) {
      return task.attachments.map(a => ({
        name: a.fileName || 'Tệp đính kèm',
        url: a.fileUrl?.startsWith('http') ? a.fileUrl : `${this.backendUrl}${a.fileUrl}`
      }));
    }

    if (!task.fileUrl) return [];

    const urls = task.fileUrl.split(';').filter(u => !!u);
    const names = task.fileName ? task.fileName.split(';') : [];

    return urls.map((url, i) => ({
      name: names[i] || `File ${i + 1}`,
      url: url.startsWith('http') ? url : `${this.backendUrl}${url}`
    }));
  }

  getPriorityBadge(priority: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Thấp', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Trung bình', cssClass: 'bg-info-subtle text-info-emphasis border' },
      2: { text: 'Cao', cssClass: 'bg-warning-subtle text-warning-emphasis border' },
      3: { text: 'Khẩn cấp', cssClass: 'bg-danger-subtle text-danger border' }
    };
    return maps[priority] || { text: 'N/A', cssClass: 'bg-light text-dark' };
  }

  getStatusBadge(status: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Mới', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Đang làm', cssClass: 'bg-primary-subtle text-primary border' },
      2: { text: 'Hoàn thành', cssClass: 'bg-success-subtle text-success border' },
      3: { text: 'Đã hủy', cssClass: 'bg-danger-subtle text-danger border' }
    };
    return maps[status] || { text: 'N/A', cssClass: 'bg-light text-dark' };
  }

  private loadCategories(): void {
    this.rest.request<any, any>({ method: 'GET', url: '/api/app/category' })
      .subscribe(res => this.categories = res.items || res || []);
  }

  private loadUsers(): void {
    this.rest.request<any, any>({ method: 'GET', url: '/api/identity/users' })
      .subscribe(res => this.users = res.items || res || []);
  }

  private cleanParams(params: any): any {
    const cleaned = { ...params };
    Object.keys(cleaned).forEach(key => {
      if (cleaned[key] === '' || cleaned[key] === null || cleaned[key] === undefined) {
        delete cleaned[key];
      }
    });
    return cleaned;
  }
}