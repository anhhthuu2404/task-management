import type { NotificationDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private restService = inject(RestService);
  apiName = 'Default';

  private hubConnection?: signalR.HubConnection;
  
  // Signal hoặc State lưu danh sách thông báo real-time nếu cần quản lý chung tại service
  public notifications = signal<NotificationDto[]>([]);

  constructor() {
    this.startSignalRConnection();
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

  private startSignalRConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/signalr-hubs/notification') // Đường dẫn Hub khớp với backend
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connected successfully for notifications.'))
      .catch(err => console.error('Error while starting SignalR connection: ', err));

    // Lắng nghe sự kiện quá hạn từ TaskOverdueEventHandler
    this.hubConnection.on('ReceiveTaskNotification', (payload: { title: string; message: string; taskId: string }) => {
      const newNotification: NotificationDto = {
        id: crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(),
        message: payload.message,
        taskId: payload.taskId,
        isRead: false,
        creationTime: new Date().toISOString()
      };

      // Cập nhật state thông báo ngay lập tức
      this.notifications.update(list => [newNotification, ...list]);
    });
  }
}