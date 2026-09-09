import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { RestService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';

export interface TaskAttachmentDto {
  fileName?: string;
  fileContent?: string;
}

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './task-form.component.html'
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rest = inject(RestService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toaster = inject(ToasterService);
  private readonly cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  taskId: string | null = null;
  isEditMode = false;
  isSubmitting = false;
  isLoading = false;

  categories: any[] = [];
  projects: any[] = [];
  departments: any[] = [];
  milestones: any[] = [];
  users: any[] = [];
  filteredUsers: any[] = [];

  minDate: string = '';
  selectedFiles: File[] = [];
  uploadProgress = 0;

  ngOnInit(): void {
    this.initForm();
    this.setMinDate();
    this.loadLookups();

    this.taskId = this.route.snapshot.paramMap.get('id');
    if (this.taskId) {
      this.isEditMode = true;
      this.loadTaskData(this.taskId);
    } else {
      this.route.queryParams.subscribe(params => {
        if (params['dueDate']) {
          this.form.patchValue({ dueDate: params['dueDate'] });
        }
        if (params['projectId']) {
          this.form.patchValue({ projectId: params['projectId'] });
          this.onProjectChange(params['projectId']);
        }
      });
    }
  }

  initForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [''],
      categoryId: ['', Validators.required],
      projectId: [null, Validators.required],
      departmentId: [null],
      milestoneId: [null],
      assigneeId: [null, Validators.required],
      dueDate: ['', Validators.required],
      priority: [1, Validators.required],
      status: [0, Validators.required],
      isRecurring: [false],
      frequency: [0]
    });

    this.form.get('projectId')?.valueChanges.subscribe(projectId => {
      if (projectId) {
        this.onProjectChange(projectId);
      } else {
        this.filteredUsers = [...this.users];
        this.milestones = [];
      }
    });

    this.form.get('milestoneId')?.valueChanges.subscribe(milestoneId => {
      if (milestoneId) {
        const selectedMilestone = this.milestones.find(m => (m.id === milestoneId || m.Id === milestoneId));
        if (selectedMilestone && (selectedMilestone.dueDate || selectedMilestone.DueDate)) {
          const dateStr = (selectedMilestone.dueDate || selectedMilestone.DueDate).split('T')[0];
          this.form.patchValue({ dueDate: dateStr });
        }
      }
    });
  }

  setMinDate(): void {
    const today = new Date();
    this.minDate = today.toISOString().split('T')[0];
  }

  loadLookups(): void {
    // 1. Load danh mục từ endpoint chuẩn của module Categories
    this.rest.request<any, any>({ method: 'GET', url: '/api/task-management/categories', params: { maxResultCount: 100 } }).subscribe({
      next: (res) => { 
        this.categories = Array.isArray(res) ? res : (res?.items || []); 
        this.cdr.detectChanges();
      }
    });

    // 2. Load dự án
    this.rest.request<any, any>({ method: 'GET', url: '/api/app/project', params: { maxResultCount: 100 } }).subscribe({
      next: (res) => { 
        this.projects = Array.isArray(res) ? res : (res?.items || []); 
        this.cdr.detectChanges();
      }
    });

    // 3. Load phòng ban
    this.rest.request<any, any>({ method: 'GET', url: '/api/app/department' }).subscribe({
      next: (res) => { 
        this.departments = Array.isArray(res) ? res : (res?.items || []); 
        this.cdr.detectChanges();
      },
      error: () => { this.departments = []; }
    });

    // 4. Load người dùng hệ thống
    this.rest.request<any, any>({ method: 'GET', url: '/api/identity/users' }).subscribe({
      next: (res) => { 
        this.users = Array.isArray(res) ? res : (res?.items || []); 
        this.filteredUsers = [...this.users];
        this.cdr.detectChanges();
      }
    });
  }

  onProjectChange(projectId: string): void {
    this.rest.request<any, any>({ method: 'GET', url: `/api/app/project/milestones/${projectId}` }).subscribe({
      next: (res) => { 
        this.milestones = Array.isArray(res) ? res : (res?.items || []); 
        this.cdr.detectChanges();
      },
      error: () => { 
        this.milestones = []; 
        this.cdr.detectChanges();
      }
    });
  }

  loadTaskData(id: string): void {
    this.isLoading = true;
    this.rest.request<any, any>({ method: 'GET', url: `/api/app/task/${id}` }).subscribe({
      next: (task) => {
        if (task) {
          this.form.patchValue({
            title: task.title,
            description: task.description,
            categoryId: task.categoryId,
            projectId: task.projectId,
            departmentId: task.departmentId,
            milestoneId: task.milestoneId,
            assigneeId: task.assigneeId,
            dueDate: task.dueDate ? task.dueDate.split('T')[0] : '',
            priority: task.priority,
            status: task.status,
            isRecurring: task.isRecurring || false,
            frequency: task.frequency || 0
          });
          if (task.projectId) {
            this.onProjectChange(task.projectId);
          }
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Không thể tải thông tin công việc.');
        this.router.navigate(['/tasks']);
      }
    });
  }

  onFileSelect(event: any): void {
    const files: FileList = event.target.files;
    if (files) {
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (file.size > 10 * 1024 * 1024) {
          this.toaster.warn(`Tệp ${file.name} vượt quá dung lượng cho phép (10MB).`);
          continue;
        }
        this.selectedFiles.push(file);
      }
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

    this.isSubmitting = true;

    // Chuyển đổi các tệp đính kèm sang định dạng Base64
    const filePromises = this.selectedFiles.map(file => {
      return new Promise<TaskAttachmentDto>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
          resolve({
            fileName: file.name,
            fileContent: reader.result as string
          });
        };
        reader.onerror = error => reject(error);
        reader.readAsDataURL(file);
      });
    });

    Promise.all(filePromises).then(attachments => {
      const requestBody = {
        ...this.form.value,
        attachments: attachments
      };

      const requestMethod = this.isEditMode ? 'PUT' : 'POST';
      const requestUrl = this.isEditMode ? `/api/app/task/${this.taskId}` : '/api/app/task';

      this.rest.request<any, any>({
        method: requestMethod,
        url: requestUrl,
        body: requestBody
      }).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.toaster.success(this.isEditMode ? 'Cập nhật công việc thành công!' : 'Tạo mới công việc thành công!');
          this.router.navigate(['/tasks']);
        },
        error: (err) => {
          this.isSubmitting = false;
          this.toaster.error(err?.error?.error?.message || 'Đã có lỗi xảy ra khi lưu công việc.');
          this.cdr.detectChanges();
        }
      });
    }).catch(() => {
      this.isSubmitting = false;
      this.toaster.error('Lỗi khi xử lý tệp đính kèm.');
      this.cdr.detectChanges();
    });
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
  }
}