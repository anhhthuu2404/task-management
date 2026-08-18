import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-user',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user.component.html'
})
export class UserComponent implements OnInit {
  users: any[] = [];
  isModalOpen = false;
  isEditMode = false;
  selectedUserId = '';
  showPassword = false;
  
  formData = {
    userName: '',
    password: '',
    email: '',
    name: '',
    surname: '',
    isActive: true
  };

  constructor(private http: HttpClient, private cd: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.http.get<any>('/api/identity/users?maxResultCount=100').subscribe({
      next: (res) => {
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
        password: '',
        email: user.email,
        name: user.name || '',
        surname: user.surname || '',
        isActive: user.isActive
      };
    } else {
      this.isEditMode = false;
      this.selectedUserId = '';
      this.formData = {
        userName: '',
        password: '',
        email: '',
        name: '',
        surname: '',
        isActive: true
      };
    }
    this.isModalOpen = true;
    this.cd.detectChanges();
  }

  saveUser(): void {
    // Gán mặc định name và surname nếu trống để tránh lỗi 400 từ ABP API
    if (!this.formData.name) this.formData.name = this.formData.userName;
    if (!this.formData.surname) this.formData.surname = ' ';

    if (this.isEditMode) {
      this.http.put(`/api/identity/users/${this.selectedUserId}`, this.formData).subscribe({
        next: () => {
          this.closeModal();
          this.loadUsers();
        },
        error: (err) => {
          const errorMsg = err.error?.error?.message || err.error?.error?.details || 'Có lỗi khi cập nhật user!';
          alert(errorMsg);
        }
      });
    } else {
      this.http.post('/api/identity/users', this.formData).subscribe({
        next: () => {
          this.closeModal();
          this.loadUsers();
        },
        error: (err) => {
          const errorMsg = err.error?.error?.message || err.error?.error?.details || 'Có lỗi khi thêm mới user!';
          alert(errorMsg);
        }
      });
    }
  }

  deleteUser(id: string): void {
    if (confirm('Bạn có chắc chắn muốn xóa tài khoản này?')) {
      this.http.delete(`/api/identity/users/${id}`).subscribe({
        next: () => this.loadUsers(),
        error: (err) => alert(err.error?.error?.message || 'Không thể xóa user này!')
      });
    }
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.cd.detectChanges();
  }
}