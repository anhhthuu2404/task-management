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

  ngOnInit() {

    // Lắng nghe qua Observable BehaviorSubject của service
    this.notificationService.notifications$.subscribe(notifications => {
      if (notifications.length > 0) {
        const latest = notifications[0];
        this.zone.run(() => {
          this.toasterService.info(latest.message, 'Thông báo mới');
        });
      }
    });
  }
}