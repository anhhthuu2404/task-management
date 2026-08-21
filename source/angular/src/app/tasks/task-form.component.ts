import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { RestService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './task-form.component.html'
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rest = inject(RestService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toaster = inject(ToasterService);

  // Đổi thành '/api/app/tasks' nếu Swagger backend của bạn dùng số nhiều
  private readonly taskApiUrl = '/api/app/task'; 
  private readonly categoryApiUrl = '/api/app/category';

  form!: FormGroup;
  taskId: string | null = null;
  isEditMode = false;
  selectedFile: File | null = null;
  fileBase64: string | null = null;
  currentFileName: string | null = null;
  isSubmitting = false;

  categories: any[] = [];
  users: any[] = [];

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.taskId;
    this.buildForm();
    this.loadCategories();
    this.loadUsers();

    if (this.isEditMode && this.taskId) {
      this.loadTaskDetail(this.taskId);
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      categoryId: ['', [Validators.required]],
      assigneeId: [null],
      priority: [1, [Validators.required]],
      status: [0, [Validators.required]],
      dueDate: [null],
    });
  }

  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: this.categoryApiUrl,
    }).subscribe({
      next: (res: any) => (this.categories = res.items || res || []),
      error: () => (this.categories = [])
    });
  }

  loadUsers(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/identity/users',
    }).subscribe({
      next: (res: any) => (this.users = res.items || res || []),
      error: () => (this.users = [])
    });
  }

  loadTaskDetail(id: string): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: `${this.taskApiUrl}/${id}`,
    }).subscribe({
      next: (res: any) => {
        this.currentFileName = res.fileName || null;
        this.form.patchValue({
          title: res.title,
          description: res.description,
          categoryId: res.categoryId,
          assigneeId: res.assigneeId,
          priority: res.priority ?? 1,
          status: res.status ?? 0,
          dueDate: res.dueDate ? res.dueDate.split('T')[0] : null,
        });
      },
      error: (err: any) => {
        if (err?.status === 404) {
          this.toaster.error('Công việc không tồn tại hoặc đã bị xóa.');
        } else {
          this.toaster.error('Không thể tải thông tin công việc.');
        }
        this.router.navigate(['/tasks']);
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        this.fileBase64 = result.split(',')[1] || result;
      };
      reader.readAsDataURL(this.selectedFile);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    const dto = {
      ...this.form.value,
      priority: Number(this.form.value.priority),
      status: Number(this.form.value.status),
      fileName: this.selectedFile ? this.selectedFile.name : this.currentFileName,
      fileContent: this.fileBase64 || null
    };

    const request$ = this.isEditMode
      ? this.rest.request<any, any>({
          method: 'PUT',
          url: `${this.taskApiUrl}/${this.taskId}`,
          body: dto,
        })
      : this.rest.request<any, any>({
          method: 'POST',
          url: this.taskApiUrl,
          body: dto,
        });

    request$.subscribe({
      next: () => {
        this.toaster.success(
          this.isEditMode ? 'Cập nhật công việc thành công!' : 'Tạo mới công việc thành công!'
        );
        this.router.navigate(['/tasks']);
      },
      error: (err: any) => {
        this.isSubmitting = false;
        this.toaster.error(err?.error?.error?.message || 'Đã có lỗi xảy ra khi lưu.');
      }
    });
  }
}