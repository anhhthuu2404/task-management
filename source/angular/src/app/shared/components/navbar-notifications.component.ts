import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router'; // Thêm Router để điều hướng
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService, NotificationItem } from '../services/notification.service';

@Component({
  selector: 'app-navbar-notifications',
  standalone: true,
  imports: [CommonModule, NgbDropdownModule],
  template: `
    <div class="dropdown" ngbDropdown placement="bottom-end" display="dynamic">
      <button class="btn btn-primary position-relative rounded-circle p-2 d-flex align-items-center justify-content-center" 
              style="width: 40px; height: 40px;" 
              ngbDropdownToggle 
              type="button" 
              id="notificationDropdown"
              title="Thông báo trực tuyến">
        <i class="bi bi-bell-fill"></i>
        <span *ngIf="unreadCount > 0" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 0.65rem;">
          {{ unreadCount }}
        </span>
      </button>

      <div ngbDropdownMenu aria-labelledby="notificationDropdown" class="dropdown-menu p-0 shadow-lg border-0" style="width: 350px; max-height: 400px; overflow-y: auto; border-radius: 12px;">
        <div class="d-flex justify-content-between align-items-center p-3 bg-light border-bottom">
          <h6 class="mb-0 fw-bold text-dark" style="font-size: 0.9rem;">Thông báo trực tuyến</h6>
          <button *ngIf="notifications.length > 0" class="btn btn-sm btn-link text-decoration-none p-0 text-primary" style="font-size: 0.85rem;" (click)="clearAll()">Xóa tất cả</button>
        </div>

        <div *ngIf="notifications.length === 0" class="text-center text-muted py-4 small">
          <i class="bi bi-bell-slash fs-4 d-block mb-1 text-black-50"></i>
          Chưa có thông báo mới nào.
        </div>

        <div class="notification-list">
          <div *ngFor="let item of notifications" 
               class="p-3 border-bottom position-relative" 
               [ngClass]="{'bg-white': item.isRead, 'bg-light': !item.isRead}"
               style="cursor: pointer; transition: background-color 0.2s;"
               (click)="onNotificationClick(item)">
            
            <span *ngIf="!item.isRead" class="position-absolute top-50 start-0 translate-middle-y bg-primary rounded-circle ms-2" style="width: 8px; height: 8px;"></span>
            
            <div [ngClass]="{'ms-3': !item.isRead}">
              <div class="fw-semibold text-dark mb-1" style="font-size: 0.9rem; line-height: 1.4;">{{ item.message }}</div>
              <div class="text-muted" style="font-size: 0.75rem;">
                <i class="bi bi-clock me-1"></i>{{ item.time | date:'dd/MM/yyyy HH:mm:ss' }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class NavbarNotificationsComponent implements OnInit, OnDestroy {
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router); // Inject Router
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();
  
  notifications: NotificationItem[] = [];

  ngOnInit(): void {
    this.notificationService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(res => {
        this.notifications = res || [];
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  // Xử lý khi click vào thông báo: đánh dấu đã đọc và điều hướng đến task tương ứng
  onNotificationClick(item: NotificationItem): void {
    if (item.id) {
      this.notificationService.markAsRead(item.id);
    }

    if (item.taskId) {
      // Điều hướng về trang tasks và truyền taskId qua queryParams (bạn có thể thay đổi đường dẫn ['/tasks'] cho khớp route ứng dụng của bạn)
      this.router.navigate(['/tasks'], { queryParams: { taskId: item.taskId } });
    } else {
      this.router.navigate(['/tasks']);
    }
  }

  clearAll(): void {
    this.notificationService.clearAll();
  }
}