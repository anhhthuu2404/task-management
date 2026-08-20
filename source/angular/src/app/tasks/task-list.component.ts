import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { RestService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="card">
      <div class="card-header d-flex justify-content-between align-items-center">
        <h4 class="card-title text-primary my-1">
          <i class="fas fa-tasks me-2"></i>Quản lý công việc
        </h4>
        <button class="btn btn-primary" (click)="onCreate()">
          <i class="fas fa-plus me-1"></i>Thêm công việc
        </button>
      </div>

      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-bordered table-hover align-middle">
            <thead class="table-light">
              <tr>
                <th>Tiêu đề</th>
                <th>Mức độ ưu tiên</th>
                <th>Hạn hoàn thành</th>
                <th class="text-center" style="width: 150px;">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of tasks">
                <td class="fw-bold text-dark">{{ item.title }}</td>
                <td>
                  <span class="badge" [ngClass]="{
                    'bg-secondary': item.priority === 0,
                    'bg-info': item.priority === 1,
                    'bg-warning': item.priority === 2,
                    'bg-danger': item.priority === 3
                  }">
                    {{ item.priority === 0 ? 'Thấp' : item.priority === 1 ? 'Trung bình' : item.priority === 2 ? 'Cao' : 'Khẩn cấp' }}
                  </span>
                </td>
                <td>{{ item.dueDate ? (item.dueDate | date:'dd/MM/yyyy') : 'Chưa có' }}</td>
                <td class="text-center">
                  <button class="btn btn-sm btn-outline-warning me-2" (click)="onEdit(item.id)" title="Chỉnh sửa">
                    <i class="fas fa-edit"></i>
                  </button>
                  <button class="btn btn-sm btn-outline-danger" (click)="onDelete(item.id)" title="Xóa">
                    <i class="fas fa-trash"></i>
                  </button>
                </td>
              </tr>
              <tr *ngIf="tasks.length === 0">
                <td colspan="4" class="text-center text-muted py-4">
                  Chưa có công việc nào. Vui lòng bấm "Thêm công việc" để tạo mới.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class TaskListComponent implements OnInit {
  private readonly rest = inject(RestService);
  private readonly router = inject(Router);
  private readonly toaster = inject(ToasterService);

  tasks: any[] = [];

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/task',
    }).subscribe({
      next: (res) => { this.tasks = res.items || []; },
      error: () => { this.toaster.error('Không thể tải danh sách công việc.'); }
    });
  }

  onCreate(): void {
    this.router.navigate(['/tasks/create']);
  }

  onEdit(id: string): void {
    this.router.navigate(['/tasks/edit', id]);
  }

  onDelete(id: string): void {
    if (confirm('Bạn có chắc chắn muốn xóa công việc này không?')) {
      this.rest.request<any, void>({
        method: 'DELETE',
        url: `/api/app/task/${id}`,
      }).subscribe({
        next: () => {
          this.toaster.success('Xóa công việc thành công!');
          this.loadTasks();
        },
        error: () => {
          this.toaster.error('Không thể xóa công việc này.');
        }
      });
    }
  }
}