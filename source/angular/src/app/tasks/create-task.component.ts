import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';

interface FileAttachment {
  fileName: string;
  fileContent: string;
}

@Component({
  selector: 'app-create-task',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './create-task.component.html'
})
export class CreateTaskComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rest = inject(RestService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toaster = inject(ToasterService);

  readonly MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
  readonly ALLOWED_EXTENSIONS = ['pdf', 'docx', 'doc', 'png', 'jpg', 'jpeg', 'xlsx'];

  form!: FormGroup;
  selectedFiles: File[] = [];
  attachments: FileAttachment[] = [];
  uploadProgress = 0;
  isSubmitting = false;
  minDate: string = new Date().toISOString().split('T')[0];

  categories: any[] = [];
  users: any[] = [];
  projects: any[] = []; // Thêm danh sách dự án

  ngOnInit(): void {
    this.buildForm();
    this.loadCategories();
    this.loadUsers();
    this.loadProjects(); // Gọi load danh sách dự án

    // Đọc projectId từ queryParams nếu bấm từ trang Quản lý dự án sang
    this.route.queryParams.subscribe(params => {
      if (params['projectId']) {
        this.form.patchValue({
          projectId: params['projectId']
        });
      }
    });
  }

  buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      categoryId: ['', [Validators.required]],
      projectId: [null], // Bổ sung trường projectId vào form
      assigneeId: [null],
      priority: [1, [Validators.required]],
      status: [0, [Validators.required]],
      dueDate: [null],
      isRecurring: [false], 
      frequency: [0],      
    });
  }

  loadCategories(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/category',
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

  loadProjects(): void {
    this.rest.request<any, any>({
      method: 'GET',
      url: '/api/app/project',
      params: { maxResultCount: 100 }
    }).subscribe({
      next: (res: any) => (this.projects = Array.isArray(res) ? res : (res?.items || res?.result || [])),
      error: () => (this.projects = [])
    });
  }

  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const newFiles: File[] = [];

    Array.from(input.files).forEach(file => {
      const ext = file.name.split('.').pop()?.toLowerCase() || '';

      if (!this.ALLOWED_EXTENSIONS.includes(ext)) {
        this.toaster.warn(`File "${file.name}" không đúng định dạng cho phép.`);
        return;
      }

      if (file.size > this.MAX_FILE_SIZE) {
        this.toaster.warn(`File "${file.name}" vượt quá dung lượng 10MB.`);
        return;
      }

      newFiles.push(file);
    });

    this.selectedFiles = [...this.selectedFiles, ...newFiles];
    this.processFilesToBase64();
    input.value = '';
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.processFilesToBase64();
  }

  processFilesToBase64(): void {
    this.attachments = [];
    if (this.selectedFiles.length === 0) {
      this.uploadProgress = 0;
      return;
    }

    let processedCount = 0;
    this.uploadProgress = 10;

    this.selectedFiles.forEach(file => {
      const reader = new FileReader();

      reader.onload = () => {
        const result = reader.result as string;
        const base64 = result.split(',')[1] || result;
        this.attachments.push({
          fileName: file.name,
          fileContent: base64
        });

        processedCount++;
        this.uploadProgress = Math.round((processedCount / this.selectedFiles.length) * 100);
      };

      reader.readAsDataURL(file);
    });
  }

  onCancel(): void {
    this.router.navigate(['/tasks']);
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
      frequency: Number(this.form.value.frequency),
      attachments: this.attachments
    };

    this.rest.request<any, any>({
      method: 'POST',
      url: '/api/app/task',
      body: dto,
    }).subscribe({
      next: () => {
        this.toaster.success('Tạo mới công việc thành công!');
        this.router.navigate(['/tasks']);
      },
      error: (err: any) => {
        this.isSubmitting = false;
        this.toaster.error(err?.error?.error?.message || 'Đã có lỗi xảy ra khi tạo mới.');
      }
    });
  }
}