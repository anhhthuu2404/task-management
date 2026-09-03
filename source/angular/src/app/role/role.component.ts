import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-role',
  standalone: true,
  imports: [CommonModule, FormsModule, CoreModule],
  templateUrl: './role.component.html'
})
export class RoleComponent implements OnInit {
  roles: any[] = [];
  selectedRole: any = null;
  permissions: any[] = [];
  
  isRoleModalOpen = false;
  roleDisplayName = '';
  roleName = '';

  roleUsers: any[] = [];
  allUsers: any[] = [];
  selectedUserIdToAdd: string = '';

  constructor(
    private httpClient: HttpClient,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void { 
    this.loadRoles(); 
    this.loadAllUsers();
  }

  loadRoles(): void {
    this.httpClient.get<any>('/api/identity/roles?maxResultCount=100').subscribe({
      next: (res: any) => {
        this.roles = res.items || [];
        if (this.roles.length > 0 && !this.selectedRole) {
          this.selectRole(this.roles[0]);
        }
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách vai trò:', err)
    });
  }

  loadAllUsers(): void {
    this.httpClient.get<any>('/api/identity/users?maxResultCount=1000').subscribe({
      next: (res: any) => {
        const list = res.items || res.result?.items || (Array.isArray(res) ? res : []);
        
        const userObservables = list.map((u: any) => {
          const userId = u.id || u.Id;
          return new Promise((resolve) => {
            this.httpClient.get<any>(`/api/identity/users/${userId}/roles`).subscribe({
              next: (roleRes: any) => {
                const rolesData = roleRes.items || roleRes.result?.items || (Array.isArray(roleRes) ? roleRes : []);
                const rolesList = rolesData.map((r: any) => typeof r === 'string' ? r : (r.name || r.Name));

                resolve({
                  id: userId,
                  userName: u.userName || u.UserName || u.name || u.Name || 'User',
                  email: u.email || u.Email || '',
                  roleNames: rolesList
                });
              },
              error: () => {
                let fallbackRoles = u.roleNames || u.RoleNames || u.roles || u.Roles || [];
                if (Array.isArray(fallbackRoles)) {
                  fallbackRoles = fallbackRoles.map((r: any) => typeof r === 'string' ? r : (r.name || r.Name));
                }
                resolve({
                  id: userId,
                  userName: u.userName || u.UserName || u.name || u.Name || 'User',
                  email: u.email || u.Email || '',
                  roleNames: fallbackRoles
                });
              }
            });
          });
        });

        Promise.all(userObservables).then((updatedUsers: any[]) => {
          this.allUsers = updatedUsers;
          if (this.selectedRole) {
            this.filterUsersBySelectedRole();
          }
          this.cd.markForCheck();
          this.cd.detectChanges();
        });
      },
      error: (err) => console.error('Lỗi tải danh sách người dùng:', err)
    });
  }

  selectRole(role: any): void {
    this.selectedRole = role;
    this.selectedUserIdToAdd = '';
    if (role && role.name) {
      this.loadPermissions(role.name);
      this.filterUsersBySelectedRole();
    }
  }

  filterUsersBySelectedRole(): void {
    if (!this.selectedRole || !this.allUsers) {
      this.roleUsers = [];
      return;
    }
    this.roleUsers = this.allUsers.filter((user: any) => 
      user.roleNames && user.roleNames.includes(this.selectedRole.name)
    );
    this.cd.detectChanges();
  }

  addUserToRole(): void {
    if (!this.selectedUserIdToAdd || !this.selectedRole) {
      alert('Vui lòng chọn người dùng cần thêm!');
      return;
    }

    const targetUserId = this.selectedUserIdToAdd;

    this.httpClient.get<any>(`/api/identity/users/${targetUserId}/roles`).subscribe({
      next: (roleRes: any) => {
        const rolesData = roleRes.items || roleRes.result?.items || (Array.isArray(roleRes) ? roleRes : []);
        let currentRoles = rolesData.map((r: any) => typeof r === 'string' ? r : (r.name || r.Name));

        if (!currentRoles.includes(this.selectedRole.name)) {
          currentRoles.push(this.selectedRole.name);
        } else {
          alert('Người dùng này đã thuộc vai trò từ trước!');
          return;
        }

        const payload = { roleNames: currentRoles };

        this.httpClient.put(`/api/identity/users/${targetUserId}/roles`, payload).subscribe({
          next: () => {
            alert('Thêm người dùng vào vai trò thành công!');
            const targetUser = this.allUsers.find(u => u.id === targetUserId);
            if (targetUser) {
              targetUser.roleNames = [...currentRoles];
            }
            this.selectedUserIdToAdd = '';
            this.filterUsersBySelectedRole();
            this.cd.detectChanges();
          },
          error: (err) => {
            alert(err.error?.error?.message || 'Không thể thêm người dùng vào vai trò!');
          }
        });
      }
    });
  }

  removeUserFromRole(userId: string): void {
    if (confirm('Bạn có chắc chắn muốn gỡ người dùng này khỏi vai trò?')) {
      this.httpClient.get<any>(`/api/identity/users/${userId}/roles`).subscribe({
        next: (roleRes: any) => {
          const rolesData = roleRes.items || roleRes.result?.items || (Array.isArray(roleRes) ? roleRes : []);
          let currentRoles = rolesData.map((r: any) => typeof r === 'string' ? r : (r.name || r.Name));

          currentRoles = currentRoles.filter((r: string) => r !== this.selectedRole.name);
          const payload = { roleNames: currentRoles };

          this.httpClient.put(`/api/identity/users/${userId}/roles`, payload).subscribe({
            next: () => {
              alert('Đã gỡ người dùng khỏi vai trò!');
              const targetUser = this.allUsers.find(u => u.id === userId);
              if (targetUser) {
                targetUser.roleNames = [...currentRoles];
              }
              this.filterUsersBySelectedRole();
              this.cd.detectChanges();
            },
            error: (err) => alert(err.error?.error?.message || 'Không thể gỡ người dùng khỏi vai trò!')
          });
        }
      });
    }
  }

  loadPermissions(roleName: string): void {
    const encodedRoleName = encodeURIComponent(roleName);
    const url = `/api/permission-management/permissions?providerName=R&providerKey=${encodedRoleName}`;
    
    this.httpClient.get<any>(url).subscribe({
      next: (res) => {
        let allPerms: any[] = [];
        const groups = res?.groups || res?.result?.groups || [];
        if (Array.isArray(groups)) {
          groups.forEach((group: any) => {
            if (group.permissions && Array.isArray(group.permissions)) {
              group.permissions.forEach((p: any) => {
                allPerms.push({
                  name: p.name,
                  displayName: p.displayName || p.name,
                  isGranted: p.isGranted || false
                });
                if (p.children && Array.isArray(p.children)) {
                  p.children.forEach((child: any) => {
                    allPerms.push({
                      name: child.name,
                      displayName: child.displayName || child.name,
                      isGranted: child.isGranted || false
                    });
                  });
                }
              });
            }
          });
        }
        this.permissions = allPerms;
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách phân quyền:', err)
    });
  }

  savePermissions(): void {
    if (!this.selectedRole || !this.selectedRole.name) return;
    const encodedRoleName = encodeURIComponent(this.selectedRole.name);
    const url = `/api/permission-management/permissions?providerName=R&providerKey=${encodedRoleName}`;
    
    const payload = {
      permissions: this.permissions.map(p => ({ name: p.name, isGranted: p.isGranted }))
    };

    this.httpClient.put(url, payload).subscribe({
      next: () => alert('Đã lưu thay đổi phân quyền thành công!'),
      error: (err) => alert(err.error?.error?.message || 'Không thể lưu phân quyền!')
    });
  }

  openRoleModal(): void { 
    this.roleDisplayName = '';
    this.roleName = ''; 
    this.isRoleModalOpen = true; 
    this.cd.detectChanges(); 
  }

  onDisplayNameChange(value: string): void {
    this.roleDisplayName = value;
    this.roleName = this.removeVietnameseTones(value)
      .toLowerCase()
      .trim()
      .replace(/\s+/g, '_')
      .replace(/[^a-z0-9_]/g, '');
  }

  private removeVietnameseTones(str: string): string {
    if (!str) return '';
    return str
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[âăÂĂ]/g, 'a') 
      .replace(/[êÊ]/g, 'e') 
      .replace(/[ôơÔƠ]/g, 'o') 
      .replace(/[ưƯ]/g, 'u') 
      .replace(/[đĐ]/g, 'd'); 
  }

  saveRole(): void {
    if (!this.roleDisplayName || !this.roleDisplayName.trim()) {
      alert('Vui lòng nhập tên vai trò!');
      return;
    }
    
    const payload = {
      name: this.roleName || this.removeVietnameseTones(this.roleDisplayName).toLowerCase().replace(/\s+/g, '_'),
      displayName: this.roleDisplayName.trim(),
      isDefault: false,
      isPublic: true
    };

    this.httpClient.post('/api/identity/roles', payload).subscribe({
      next: () => {
        this.closeRoleModal();
        this.loadRoles();
      },
      error: (err) => {
        alert(err.error?.error?.message || 'Tên vai trò không hợp lệ hoặc đã tồn tại!');
      }
    });
  }

  deleteRole(id: string): void {
    if (confirm('Xóa vai trò này khỏi hệ thống?')) {
      this.httpClient.delete(`/api/identity/roles/${id}`).subscribe({
        next: () => {
          this.selectedRole = null;
          this.permissions = [];
          this.roleUsers = [];
          this.loadRoles();
        }
      });
    }
  }

  closeRoleModal(): void { 
    this.isRoleModalOpen = false; 
    this.cd.detectChanges(); 
  }
}