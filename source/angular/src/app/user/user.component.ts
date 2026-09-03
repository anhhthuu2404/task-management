import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-user',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    CoreModule
  ],
  templateUrl: './user.component.html'
})
export class UserComponent implements OnInit {

  users: any[] = [];
  isModalOpen = false;
  isEditMode = false;
  selectedUserId: string | null = null;
  showPassword = false;

  formData: any = {
    userName: '',
    email: '',
    name: '',
    surname: '',
    password: '',
    isActive: true,
    lockoutEnabled: true,
    roleNames: [],
    extraProperties: {}
  };

  constructor(
    private httpClient: HttpClient,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.httpClient.get<any>('/api/identity/users?maxResultCount=100').subscribe({
      next: (res: any) => {
        this.users = res.items || [];
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách user:', err)
    });
  }

  openModal(user?: any): void {
    this.showPassword = false;
    
    if (user) {
      this.isEditMode = true;
      this.selectedUserId = user.id;
      this.formData = {
        userName: user.userName,
        email: user.email,
        name: user.name || '',
        surname: user.surname || '',
        password: '', // Để trống khi sửa để không bắt buộc đổi mật khẩu
        isActive: user.isActive ?? true,
        lockoutEnabled: user.lockoutEnabled ?? true,
        roleNames: user.roleNames || [],
        extraProperties: user.extraProperties || {}
      };
    } else {
      this.isEditMode = false;
      this.selectedUserId = null;
      this.formData = {
        userName: '',
        email: '',
        name: '',
        surname: '',
        password: '',
        isActive: true,
        lockoutEnabled: true,
        roleNames: [],
        extraProperties: {}
      };
    }
    this.isModalOpen = true; 
    this.cd.detectChanges();
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.cd.detectChanges();
  }

  saveUser(): void {
    const payload = { ...this.formData };

    if (this.isEditMode) {
      if (!payload.password || payload.password.trim() === '') {
        delete payload.password;
      }
    }

    if (this.isEditMode && this.selectedUserId) {
      this.httpClient.put(`/api/identity/users/${this.selectedUserId}`, payload).subscribe({
        next: () => {
          alert('Cập nhật người dùng thành công!');
          this.closeModal();
          this.loadUsers();
        },
        error: (err: any) => this.handleError(err, 'cập nhật')
      });
    } else {
      this.httpClient.post('/api/identity/users', payload).subscribe({
        next: () => {
          alert('Thêm người dùng thành công!');
          this.closeModal();
          this.loadUsers();
        },
        error: (err: any) => this.handleError(err, 'thêm mới')
      });
    }
  }

  deleteUser(id: string): void {
    if (confirm('Bạn có chắc chắn muốn xóa người dùng này không?')) {
      this.httpClient.delete(`/api/identity/users/${id}`).subscribe({
        next: () => {
          alert('Xóa người dùng thành công!');
          this.loadUsers();
        },
        error: (err: any) => {
          let errorMsg = err.error?.error?.message || 'Không thể xóa người dùng!';
          alert(errorMsg);
        }
      });
    }
  }

  private handleError(err: any, actionName: string): void {
    let errorMsg = `Có lỗi khi ${actionName} user!`;
    if (err.error?.error) {
      const abpError = err.error.error;
      errorMsg = abpError.details || abpError.message || errorMsg;
    }
    alert(`Không thể ${actionName} người dùng:\n` + errorMsg);
  }
}