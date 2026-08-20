import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './task-form.component.html'
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toaster = inject(ToasterService);
  private readonly rest = inject(RestService);

  form!: FormGroup;
  categories: any[] = [];
  users: any[] = [];
  selectedFiles: File[] = [];
  isSubmitting = false;
  taskId: string | null = null;
  isEditMode = false;

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.taskId;

    this.buildForm();
    this.loadCategories();
    this.loadUsers();

    if (this.isEditMode) {
      this.loadTaskDetail();
    }
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

  loadTaskDetail(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: `/api/app/task/${this.taskId}`,
    }).subscribe({
      next: (res) => {
        const formattedDueDate = res.dueDate ? new Date(res.dueDate).toISOString().split('T')[0] : null;
        this.form.patchValue({
          title: res.title,
          description: res.description,
          priority: res.priority,
          dueDate: formattedDueDate,
          categoryId: res.categoryId,
          assigneeId: res.assigneeId,
        });
      },
      error: () => {
        this.toaster.error('Không tìm thấy thông tin công việc.');
        this.router.navigate(['/tasks']);
      }
    });
  }

  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/category',
    }).subscribe({
      next: (res) => { this.categories = res.items || []; },
    });
  }

  loadUsers(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/identity/users',
    }).subscribe({
      next: (res) => { this.users = res.items || []; },
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
      this.toaster.warn('Vui lòng điền đầy đủ thông tin.');
      return;
    }

    this.isSubmitting = true;
    const val = this.form.value;

    if (this.isEditMode) {
      const updatePayload = {
        title: val.title.trim(),
        description: val.description ? val.description.trim() : null,
        categoryId: val.categoryId,
        priority: Number(val.priority),
        dueDate: val.dueDate ? new Date(val.dueDate).toISOString() : null,
        assigneeId: val.assigneeId || null
      };

      this.rest.request<any, any>({
        method: 'PUT',
        url: `/api/app/task/${this.taskId}`,
        body: updatePayload,
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.toaster.success('Cập nhật công việc thành công!');
          this.router.navigate(['/tasks']);
        },
        error: () => {
          this.isSubmitting = false;
          this.toaster.error('Cập nhật thất bại.');
        }
      });
    } else {
      const formData = new FormData();
      formData.append('Title', val.title.trim());
      formData.append('CategoryId', val.categoryId);
      formData.append('Priority', Number(val.priority).toString());

      if (val.description) formData.append('Description', val.description.trim());
      if (val.dueDate) formData.append('DueDate', new Date(val.dueDate).toISOString());
      if (val.assigneeId) formData.append('AssigneeId', val.assigneeId);

      this.selectedFiles.forEach((file) => formData.append('Files', file, file.name));

      this.rest.request<any, any>({
        method: 'POST',
        url: '/api/app/task',
        body: formData,
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.toaster.success('Tạo mới công việc thành công!');
          this.router.navigate(['/tasks']);
        },
        error: () => {
          this.isSubmitting = false;
          this.toaster.error('Tạo mới thất bại.');
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
  }
}