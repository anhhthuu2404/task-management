import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CoreModule, ConfigStateService, LocalizationService } from '@abp/ng.core';
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
  private readonly configState = inject(ConfigStateService);
  private readonly localizationService = inject(LocalizationService);

  users: any[] = [];
  totalCount = 0;          
  pageSize = 10;            
  page = 0;                 

  isModalOpen = false;
  isEditMode = false;
  selectedUserId: string | null = null;
  showPassword = false;
  changePassword = false; 

  isCurrentLoggedInUser = false;
  isAdminUser = false; // Biến phân quyền Admin

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
    this.checkAdminRole();
    this.loadUsers();
  }

  // Kiểm tra quyền Admin dựa trên danh sách roles trong state
  checkAdminRole(): void {
    const roles: string[] = this.configState.getDeep('currentUser.roles') || [];
    this.isAdminUser = roles.some(role => role.toLowerCase() === 'admin');
  }

  loadUsers(pageOffset: number = 0): void {
    this.page = pageOffset;
    const skipCount = this.page * this.pageSize;
    
    this.httpClient.get<any>(`/api/identity/users?skipCount=${skipCount}&maxResultCount=${this.pageSize}`).subscribe({
      next: (res: any) => {
        this.users = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.cd.detectChanges();
      },
      error: (err) => {
        const isEn = this.localizationService.currentLang?.startsWith('en');
        console.error(isEn ? 'Error loading users:' : 'Lỗi tải danh sách người dùng:', err);
      }
    });
  }

  getData(pageInfo: any): void {
    const pageIndex = pageInfo.offset ? pageInfo.offset : (pageInfo - 1 >= 0 ? pageInfo - 1 : 0);
    this.loadUsers(pageIndex);
  }

  openModal(user?: any): void {
    this.showPassword = false;
    this.changePassword = false; 
    
    if (user) {
      this.isEditMode = true;
      this.selectedUserId = user.id;

      const currentUserId = this.configState.getDeep('currentUser.id');
      this.isCurrentLoggedInUser = (currentUserId === user.id);

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
      this.isCurrentLoggedInUser = false; 
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

    const isEn = this.localizationService.currentLang?.startsWith('en');

    if (this.isEditMode && this.selectedUserId) {
      this.httpClient.put(`/api/identity/users/${this.selectedUserId}`, payload).subscribe({
        next: () => {
          this.toaster.success(
            isEn ? 'User updated successfully.' : 'Cập nhật người dùng thành công.', 
            isEn ? 'Success' : 'Thành công'
          );
          this.closeModal();
          this.loadUsers(this.page);
        },
        error: (err: any) => this.handleError(err, 'update')
      });
    } else {
      this.httpClient.post('/api/identity/users', payload).subscribe({
        next: () => {
          this.toaster.success(
            isEn ? 'User created successfully.' : 'Tạo người dùng thành công.', 
            isEn ? 'Success' : 'Thành công'
          );
          this.closeModal();
          this.loadUsers(0);
        },
        error: (err: any) => this.handleError(err, 'create')
      });
    }
  }

  deleteUser(id: string): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');

    this.confirmation
      .warn(
        isEn ? 'Are you sure you want to delete this user?' : 'Bạn có chắc chắn muốn xóa người dùng này?', 
        isEn ? 'Are you sure' : 'Xác nhận xóa'
      )
      .subscribe((status) => {
        if (status === Confirmation.Status.confirm) {
          this.httpClient.delete(`/api/identity/users/${id}`).subscribe({
            next: () => {
              this.toaster.success(
                isEn ? 'User deleted successfully.' : 'Xóa người dùng thành công.', 
                isEn ? 'Success' : 'Thành công'
              );
              this.loadUsers(this.page);
            },
            error: (err: any) => {
              const defaultMsg = isEn ? 'Could not delete the user!' : 'Không thể xóa người dùng này!';
              const errorMsg = err.error?.error?.message || defaultMsg;
              this.toaster.error(errorMsg, isEn ? 'Error' : 'Lỗi');
            }
          });
        }
      });
  }

  private handleError(err: any, actionName: string): void {
    const isEn = this.localizationService.currentLang?.startsWith('en');
    let errorMsg = isEn 
      ? `An error occurred while trying to ${actionName} user!` 
      : `Đã xảy ra lỗi khi ${actionName === 'create' ? 'tạo' : 'cập nhật'} người dùng!`;

    if (err.error?.error) {
      const abpError = err.error.error;
      errorMsg = abpError.details || abpError.message || errorMsg;
    }
    this.toaster.error(errorMsg, isEn ? 'Error' : 'Lỗi');
  }
}