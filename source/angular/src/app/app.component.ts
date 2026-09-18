import { Component, OnInit, NgZone, inject } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent, ToasterService } from '@abp/ng.theme.shared';

import { NotificationService } from './shared/services/notification.service';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly toasterService = inject(ToasterService);
  private readonly zone = inject(NgZone);

  private lastProcessedCount = 0;

  ngOnInit() {
    // Thêm kiểu dữ liệu (notifications: any[]) để tường minh hóa kiểu, tránh lỗi unknown
    this.notificationService.notifications$.subscribe((notifications: any[]) => {
      const currentCount = notifications ? notifications.length : 0;
      
      if (currentCount > this.lastProcessedCount && currentCount > 0) {
        const latest = notifications[0];
        
        // Kết hợp setTimeout và zone.run để triệt để khắc phục lỗi NG0100 của Toast
        setTimeout(() => {
          this.zone.run(() => {
            this.toasterService.info(latest?.message || 'Bạn có thông báo mới', 'Thông báo mới');
          });
        });
      }
      
      this.lastProcessedCount = currentCount;
    });

    this.injectBellIntoNavbar();
  }

  private injectBellIntoNavbar() {
    setTimeout(() => {
      const navbarNav = document.querySelector('.navbar-nav.ms-auto') || document.querySelector('.navbar-nav');
      if (navbarNav && !document.querySelector('app-navbar-notifications')) {
        const bellElement = document.createElement('app-navbar-notifications');
        navbarNav.prepend(bellElement);
      }
    }, 1000);
  }
}