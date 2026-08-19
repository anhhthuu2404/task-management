import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { IdentityRoleService } from '@abp/ng.identity/proxy';

@Component({
  selector: 'app-role',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './role.component.html'
})
export class RoleComponent implements OnInit {
  roles: any[] = [];
  selectedRole: any = null;
  permissions: any[] = [];
  
  isRoleModalOpen = false;
  roleDisplayName = '';
  roleName = '';

  constructor(
    private roleService: IdentityRoleService,
    private httpClient: HttpClient,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void { 
    this.loadRoles(); 
  }

  loadRoles(): void {
    this.roleService.getList({ maxResultCount: 100 }).subscribe({
      next: (res: any) => {
        this.roles = res.items || [];
        if (this.roles.length > 0 && !this.selectedRole) {
          this.selectRole(this.roles[0]);
        } else if (this.roles.length === 0) {
          this.selectedRole = null;
          this.permissions = [];
        }
        this.cd.detectChanges();
      },
      error: (err) => console.error('Lỗi tải danh sách vai trò:', err)
    });
  }

  selectRole(role: any): void {
    this.selectedRole = role;
    if (role && role.name) {
      this.loadPermissions(role.name);
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
              allPerms = allPerms.concat(group.permissions);
            }
          });
        }
        
        this.permissions = allPerms;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.error('Không thể tải danh sách quyền:', err);
        this.permissions = [];
        this.cd.detectChanges();
      }
    });
  }

  savePermissions(): void {
    if (!this.selectedRole || !this.selectedRole.name) return;

    const encodedRoleName = encodeURIComponent(this.selectedRole.name);
    const url = `/api/permission-management/permissions?providerName=R&providerKey=${encodedRoleName}`;
    
    const payload = {
      permissions: this.permissions.map(p => ({
        name: p.name,
        isGranted: p.isGranted
      }))
    };

    this.httpClient.put(url, payload).subscribe({
      next: () => {
        alert('Đã lưu thay đổi phân quyền thành công!');
      },
      error: (err) => {
        console.error('Lỗi lưu quyền:', err);
        alert(err.error?.error?.message || 'Không thể lưu phân quyền!');
      }
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

  // Xử lý triệt để loại bỏ 100% chữ â, ê, ô, ơ, ư, đ...
  private removeVietnameseTones(str: string): string {
    if (!str) return '';
    return str
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '') // Xóa các dấu thanh
      .replace(/[âăÂĂ]/g, 'a')        // Chuyển â, ă thành a
      .replace(/[êÊ]/g, 'e')          // Chuyển ê thành e
      .replace(/[ôơÔƠ]/g, 'o')        // Chuyển ô, ơ thành o
      .replace(/[ưƯ]/g, 'u')          // Chuyển ư thành u
      .replace(/[đĐ]/g, 'd');         // Chuyển đ thành d
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

    this.roleService.create(payload as any).subscribe({
      next: () => {
        this.closeRoleModal();
        this.loadRoles();
      },
      error: (err) => {
        const errorMsg = err.error?.error?.message || err.error?.error?.details || 'Tên vai trò không hợp lệ hoặc đã tồn tại!';
        alert(errorMsg);
      }
    });
  }

  deleteRole(id: string): void {
    if (confirm('Xóa vai trò này khỏi hệ thống?')) {
      this.roleService.delete(id).subscribe({
        next: () => {
          this.selectedRole = null;
          this.permissions = [];
          this.loadRoles();
        },
        error: (err) => alert(err.error?.error?.message || 'Không thể xóa vai trò này!')
      });
    }
  }

  closeRoleModal(): void { 
    this.isRoleModalOpen = false; 
    this.cd.detectChanges(); 
  }
}