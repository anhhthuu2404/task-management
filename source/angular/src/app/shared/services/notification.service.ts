import { Injectable, inject, NgZone } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService, RestService } from '@abp/ng.core';

export interface NotificationItem {
  id?: string;
  taskId?: string;
  message: string;
  time: Date;
  expiresAt?: Date;
  isRead?: boolean;
  read?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private notificationSubject = new BehaviorSubject<NotificationItem[]>([]);
  public notifications$ = this.notificationSubject.asObservable();

  private hubConnection: signalR.HubConnection | null = null;
  private readonly authService = inject(AuthService);
  private readonly restService = inject(RestService);
  private readonly zone = inject(NgZone);

  private readonly STORAGE_KEY = 'task_management_notifications_v3'; // Nâng version storage để làm sạch cache cũ nếu cần
  private readonly EXPIRATION_TIME_MS = 24 * 60 * 60 * 1000; // 24 giờ
  private isStarting = false;

  constructor() {
    this.loadFromStorage();
    
    // Đợi 1 chút để đảm bảo AuthService đã sẵn sàng token rồi mới kết nối
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
          .filter((item: NotificationItem) => !item.expiresAt || new Date(item.expiresAt).getTime() > now);

        this.notificationSubject.next(parsed);
        this.saveToStorage(parsed);
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

  private async startConnection() {
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

    // Đảm bảo dọn dẹp kết nối cũ hoàn toàn trước khi tạo mới để tránh đăng ký sự kiện nhiều lần (gây lặp)
    if (this.hubConnection) {
      try {
        this.hubConnection.off('ReceiveNotification');
        await this.hubConnection.stop();
      } catch (e) {
        console.error('Lỗi khi dừng kết nối cũ:', e);
      }
      this.hubConnection = null;
    }

    // Lấy apiurl từ cấu hình ABP hoặc fallback sang đường dẫn tuyệt đối/tương đối của bạn
    const baseUrl = (this.restService as any).apiURL || 'https://localhost:44399';
    const hubUrl = `${baseUrl.replace(/\/$/, '')}/signalr-hubs/notification`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    // Đăng ký sự kiện lắng nghe 1 lần duy nhất trên instance mới
    this.hubConnection.on('ReceiveNotification', (arg1: any, arg2?: string) => {
      console.log('Nhận được thông báo từ Hub:', arg1, arg2);

      let message = '';
      let taskId: string | undefined = undefined;
      let notificationId = arg1?.id || arg1?.Id || ('_' + Math.random().toString(36).substr(2, 9));
      let timeVal = new Date();

      if (typeof arg1 === 'object' && arg1 !== null) {
        message = arg1.message || arg1.Message || '';
        taskId = arg1.taskId || arg1.TaskId || arg1.taskID || arg1.TaskID;
        
        const rawTime = arg1.creationTime || arg1.CreationTime || arg1.time || arg1.Time;
        timeVal = rawTime ? new Date(rawTime) : new Date();
      } 
      else if (typeof arg1 === 'string') {
        message = arg1;
        taskId = arg2;
      }

      if (!message) return;

      this.zone.run(() => {
        const currentList = this.notificationSubject.value;

        // 1. Chống trùng lặp nghiêm ngặt theo ID (nếu Backend có truyền ID cố định)
        if (notificationId && currentList.some(item => item.id === notificationId)) {
          return;
        }

        // 2. Chống trùng lặp thông báo giống hệt nhau trong khoảng thời gian 5 giây
        const isDuplicateRecent = currentList.some(
          item => item.message === message && 
                  item.taskId === taskId && 
                  (new Date().getTime() - new Date(item.time).getTime() < 5000)
        );

        if (isDuplicateRecent) {
          return;
        }

        const newNotification: NotificationItem = {
          id: notificationId,
          taskId: taskId,
          message: message,
          time: timeVal,
          expiresAt: new Date(timeVal.getTime() + this.EXPIRATION_TIME_MS),
          isRead: false,
          read: false
        };

        const updatedList = [newNotification, ...currentList];
        this.notificationSubject.next(updatedList);
        this.saveToStorage(updatedList);
      });
    });

    try {
      await this.hubConnection.start();
      this.isStarting = false;
      console.log('SignalR Connected Successfully to NotificationHub!');
    } catch (err: unknown) {
      this.isStarting = false;
      console.log('Error while starting SignalR connection: ' + err);
      setTimeout(() => this.startConnection(), 5000);
    }
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