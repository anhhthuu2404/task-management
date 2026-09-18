import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CoreModule, LocalizationService } from '@abp/ng.core';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';

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
  private readonly httpClient = inject(HttpClient);
  private readonly cd = inject(ChangeDetectorRef);
  private readonly toaster = inject(ToasterService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly localizationService = inject(LocalizationService);

  users: any[] = [];
  totalCount = 0;           // Biến lưu tổng số lượng user
  pageSize = 10;            // Số lượng bản ghi trên 1 trang
  page = 0;                 // Trang hiện tại (ABP bắt đầu từ 0 hoặc tính theo skipCount)

  isModalOpen = false;
  isEditMode = false;
  selectedUserId: string | null = null;
  showPassword = false;
  changePassword = false; 

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

  ngOnInit(): void {
    this.loadUsers();
  }

  // Hàm tải dữ liệu có phân trang
  loadUsers(pageOffset: number = 0): void {
    this.page = pageOffset;
    const skipCount = this.page * this.pageSize;
    
    // Gọi API Identity User của ABP kèm theo phân trang skipCount và maxResultCount
    this.httpClient.get<any>(`/api/identity/users?skipCount=${skipCount}&maxResultCount=${this.pageSize}`).subscribe({
      next: (res: any) => {
        this.users = res.items || [];
        this.totalCount = res.totalCount || 0; // Lấy tổng số lượng từ API trả về
        this.cd.detectChanges();
      },
      error: (err) => console.error('Error loading users:', err)
    });
  }

  // Hàm lắng nghe sự kiện đổi trang từ giao diện <abp-paginator>
  getData(pageInfo: any): void {
    // ABP paginator thường truyền về offset hoặc số trang, ta tính toán lại page
    const pageIndex = pageInfo.offset ? pageInfo.offset : (pageInfo - 1 >= 0 ? pageInfo - 1 : 0);
    this.loadUsers(pageIndex);
  }

  openModal(user?: any): void {
    this.showPassword = false;
    this.changePassword = false; 
    
    if (user) {
      this.isEditMode = true;
      this.selectedUserId = user.id;
      this.formData = {
        userName: user.userName,
        email: user.email,
        name: user.name || '',
        surname: user.surname || '',
        password: '', 
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

  onChangePasswordToggle(): void {
    if (!this.changePassword) {
      this.formData.password = '';
    }
  }

  saveUser(): void {
    const payload = { ...this.formData };

    if (this.isEditMode) {
      if (!this.changePassword || !payload.password || payload.password.trim() === '') {
        delete payload.password;
      }
    }

    if (this.isEditMode && this.selectedUserId) {
      this.httpClient.put(`/api/identity/users/${this.selectedUserId}`, payload).subscribe({
        next: () => {
          this.toaster.success('User updated successfully.', 'Success');
          this.closeModal();
          this.loadUsers(this.page); // Load lại trang hiện tại
        },
        error: (err: any) => this.handleError(err, 'update')
      });
    } else {
      this.httpClient.post('/api/identity/users', payload).subscribe({
        next: () => {
          this.toaster.success('User created successfully.', 'Success');
          this.closeModal();
          this.loadUsers(0); // Tạo mới xong quay về trang đầu tiên
        },
        error: (err: any) => this.handleError(err, 'create')
      });
    }
  }

  deleteUser(id: string): void {
    this.confirmation
      .warn('Are you sure you want to delete this user?', 'AreYouSure')
      .subscribe((status) => {
        if (status === Confirmation.Status.confirm) {
          this.httpClient.delete(`/api/identity/users/${id}`).subscribe({
            next: () => {
              this.toaster.success('User deleted successfully.', 'Success');
              this.loadUsers(this.page); // Load lại trang hiện tại
            },
            error: (err: any) => {
              const errorMsg = err.error?.error?.message || 'Could not delete the user!';
              this.toaster.error(errorMsg, 'Error');
            }
          });
        }
      });
  }

  private handleError(err: any, actionName: string): void {
    let errorMsg = `An error occurred while trying to ${actionName} user!`;
    if (err.error?.error) {
      const abpError = err.error.error;
      errorMsg = abpError.details || abpError.message || errorMsg;
    }
    this.toaster.error(errorMsg, 'Error');
  }
}