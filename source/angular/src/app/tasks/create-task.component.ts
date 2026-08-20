import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';

@Component({
  selector: 'app-create-task',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-task.component.html',
})
export class CreateTaskComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toaster = inject(ToasterService);
  private readonly rest = inject(RestService);

  form!: FormGroup;
  categories: any[] = [];
  users: any[] = [];
  selectedFiles: File[] = [];
  isSubmitting = false;

  ngOnInit(): void {
    this.buildForm();
    this.loadCategories();
    this.loadUsers();
  }

  buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [''],
      priority: [1, [Validators.required]],
      dueDate: [null],
      categoryId: ['', [Validators.required]],
      assigneeId: [null],
    });
  }

  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/category',
    }).subscribe({
      next: (res) => { this.categories = res.items || []; },
      error: () => { this.toaster.error('Không thể tải danh sách danh mục.'); },
    });
  }

  loadUsers(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/identity/users',
    }).subscribe({
      next: (res) => { this.users = res.items || []; },
      error: () => { this.toaster.error('Không thể tải danh sách người dùng.'); },
    });
  }

  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFiles.push(...Array.from(input.files));
      input.value = '';
    }
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toaster.warn('Vui lòng điền đầy đủ các thông tin bắt buộc.');
      return;
    }

    const val = this.form.value;

    if (!val.categoryId || val.categoryId === '' || val.categoryId === 'null') {
      this.toaster.warn('Vui lòng chọn Danh mục.');
      return;
    }

    this.isSubmitting = true;
    const formData = new FormData();

    formData.append('title', val.title.trim());
    formData.append('categoryId', val.categoryId);
    formData.append('priority', Number(val.priority).toString());

    if (val.description && val.description.trim() !== '') {
      formData.append('description', val.description.trim());
    }

    if (val.dueDate) {
      const parsedDate = new Date(val.dueDate);
      if (!isNaN(parsedDate.getTime())) {
        formData.append('dueDate', parsedDate.toISOString());
      }
    }

    if (val.assigneeId && val.assigneeId !== '' && val.assigneeId !== 'null') {
      formData.append('assigneeId', val.assigneeId);
    }

    if (this.selectedFiles.length > 0) {
      this.selectedFiles.forEach((file) => {
        formData.append('files', file, file.name);
      });
    }

    this.rest.request<any, any>({
      method: 'POST',
      url: '/api/app/task',
      body: formData,
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toaster.success('Tạo mới công việc thành công!');
        this.form.reset({ priority: 1, categoryId: '', assigneeId: null });
        this.selectedFiles = [];
      },
      error: (err) => {
        this.isSubmitting = false;
        const serverError = err?.error?.error;
        if (serverError?.message) {
          this.toaster.error(serverError.message);
        } else {
          this.toaster.error('Đã xảy ra lỗi khi tạo công việc.');
        }
      },
    });
  }

  onCancel(): void {
    this.router.navigate(['/']);
  }
}