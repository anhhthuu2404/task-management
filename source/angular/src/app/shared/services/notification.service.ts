import { Injectable, inject, NgZone } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService, RestService } from '@abp/ng.core';

export interface NotificationItem {
  id?: string;
  taskId?: string;
  message: string;
  time: Date;
  expiresAt?: Date; // Bổ sung thời gian hết hạn tùy chọn
  isRead?: boolean;
  read?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private notificationSubject = new BehaviorSubject<NotificationItem[]>([]);
  public notifications$ = this.notificationSubject.asObservable();

  private hubConnection!: signalR.HubConnection;
  private readonly authService = inject(AuthService);
  private readonly restService = inject(RestService);
  private readonly zone = inject(NgZone);

  private readonly STORAGE_KEY = 'task_management_notifications_v2';
  private readonly EXPIRATION_TIME_MS = 24 * 60 * 60 * 1000; // Thời gian sống: 24 giờ
  private isStarting = false;

  constructor() {
    this.loadFromStorage();
    setTimeout(() => {
      this.startConnection();
    }, 1000);

    // Tự động kiểm tra và quét dọn các thông báo quá hạn mỗi 1 phút
    setInterval(() => {
      this.clearExpiredNotifications();
    }, 60000);
  }

  private loadFromStorage(): void {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved) {
      try {
        const now = new Date().getTime();
        const parsed: NotificationItem[] = JSON.parse(saved)
          .map((item: any) => ({
            ...item,
            time: new Date(item.time),
            expiresAt: item.expiresAt ? new Date(item.expiresAt) : new Date(new Date(item.time).getTime() + this.EXPIRATION_TIME_MS)
          }))
          // Chỉ giữ lại những thông báo chưa quá hạn so với hiện tại
          .filter((item: NotificationItem) => !item.expiresAt || new Date(item.expiresAt).getTime() > now);

        this.notificationSubject.next(parsed);
        this.saveToStorage(parsed); // Cập nhật lại kho lưu trữ sau khi đã lọc bỏ mục quá hạn
      } catch (e: unknown) {
        this.notificationSubject.next([]);
      }
    }
  }

  private saveToStorage(items: NotificationItem[]): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(items));
  }

  private clearExpiredNotifications(): void {
    const currentList = this.notificationSubject.value;
    const now = new Date().getTime();

    const validList = currentList.filter(item => {
      if (!item.expiresAt) return true;
      return new Date(item.expiresAt).getTime() > now;
    });

    if (validList.length !== currentList.length) {
      this.notificationSubject.next(validList);
      this.saveToStorage(validList);
    }
  }

  private startConnection() {
    if (this.isStarting) return;

    if (this.hubConnection && (
        this.hubConnection.state === signalR.HubConnectionState.Connected || 
        this.hubConnection.state === signalR.HubConnectionState.Connecting
    )) {
      return;
    }

    const token = this.authService.getAccessToken();
    if (!token) {
      setTimeout(() => this.startConnection(), 2000);
      return;
    }

    this.isStarting = true;

    if (this.hubConnection) {
      this.hubConnection.off('ReceiveNotification');
      this.hubConnection.stop();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:44399/signalr-hubs/notification', {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (message: string, taskId?: string) => {
      console.log('Nhận được thông báo từ Hub:', message, taskId);
      
      if (!message) return;

      this.zone.run(() => {
        const currentList = this.notificationSubject.value;

        // Chống trùng lặp thông báo trong khoảng 3 giây
        const isDuplicateRecent = currentList.some(
          item => item.message === message && (new Date().getTime() - new Date(item.time).getTime() < 3000)
        );

        if (isDuplicateRecent) {
          return;
        }

        const now = new Date();
        const newNotification: NotificationItem = {
          id: '_' + Math.random().toString(36).substr(2, 9),
          taskId: taskId,
          message: message,
          time: now,
          expiresAt: new Date(now.getTime() + this.EXPIRATION_TIME_MS), // Thiết lập hạn sử dụng 24h
          isRead: false,
          read: false
        };

        const updatedList = [newNotification, ...currentList];
        this.notificationSubject.next(updatedList);
        this.saveToStorage(updatedList);
      });
    });

    this.hubConnection
      .start()
      .then(() => {
        this.isStarting = false;
        console.log('SignalR Connected Successfully to NotificationHub!');
      })
      .catch((err: unknown) => {
        this.isStarting = false;
        console.log('Error while starting SignalR connection: ' + err);
        setTimeout(() => this.startConnection(), 5000);
      });
  }

  markAsRead(id?: string): void {
    const currentList = this.notificationSubject.value;
    let updated: NotificationItem[];
    if (id) {
      updated = currentList.map(item => 
        (item.id === id || (item as any).key === id) ? { ...item, isRead: true, read: true } : item
      );
    } else {
      updated = currentList.map(item => ({ ...item, isRead: true, read: true }));
    }
    this.notificationSubject.next(updated);
    this.saveToStorage(updated);
  }

  markAllAsRead(): void {
    this.markAsRead();
  }

  removeNotification(idOrMessage: string): void {
    const currentList = this.notificationSubject.value;
    const updated = currentList.filter(item => item.id !== idOrMessage && (item as any).key !== idOrMessage && item.message !== idOrMessage);
    this.notificationSubject.next(updated);
    this.saveToStorage(updated);
  }

  deleteNotification(idOrMessage: string): void {
    this.removeNotification(idOrMessage);
  }

  clearAll(): void {
    this.notificationSubject.next([]);
    localStorage.removeItem(this.STORAGE_KEY);
  }

  clearNotifications(): void {
    this.clearAll();
  }
}