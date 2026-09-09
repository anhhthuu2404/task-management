import type { NotificationDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject, ApplicationRef } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private restService = inject(RestService);
  apiName = 'Default';

  private hubConnection!: signalR.HubConnection;
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    // Khởi tạo kết nối tới endpoint /signalr-notification hoặc hub tùy cấu hình của bạn
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/signalr-hubs/notification') // Thay đổi đường dẫn này nếu cấu hình hub của bạn khác
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connected successfully.'))
      .catch(err => console.log('Error while starting SignalR connection: ', err));

    // Lắng nghe sự kiện đẩy về từ backend kèm theo 3 tham số: message, taskId, creationTime
    this.hubConnection.on('ReceiveNotification', (message: string, taskId: string, creationTime: string) => {
      const currentList = this.notificationsSubject.value;
      
      const newNotification: NotificationDto = {
        id: '', // Hoặc gen tạm id nếu cần
        message: message,
        taskId: taskId,
        creationTime: creationTime, // Nhận chính xác mốc thời gian từ backend đẩy xuống
        isRead: false,
      };

      // Đưa thông báo mới lên đầu danh sách để hiển thị ngay lập tức
      this.notificationsSubject.next([newNotification, ...currentList]);
    });
  }

  getUserNotifications = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationDto[]>({
      method: 'GET',
      url: '/api/app/notification/user-notifications',
    },
    { apiName: this.apiName, ...config });

  markAsRead = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/notification/${id}/mark-as-read`,
    },
    { apiName: this.apiName, ...config });
}