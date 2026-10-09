import { Injectable, inject, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService, RestService, LocalizationService } from '@abp/ng.core';

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
  private readonly localizationService = inject(LocalizationService);
  private readonly zone = inject(NgZone);
  private readonly router = inject(Router);

  private readonly STORAGE_KEY = 'task_management_notifications_v3';
  private readonly EXPIRATION_TIME_MS = 24 * 60 * 60 * 1000; // 24 giờ
  private isStarting = false;

  constructor() {
    this.loadFromStorage();
    
    setTimeout(() => {
      this.fetchNotificationsFromDatabase();
    }, 500);

    setTimeout(() => {
      this.startConnection();
    }, 1000);

    setInterval(() => {
      this.clearExpiredNotifications();
    }, 60000);
  }

  public refreshNotifications(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    this.fetchNotificationsFromDatabase();
  }

  private deduplicateNotifications(items: NotificationItem[]): NotificationItem[] {
    const uniqueMap = new Map<string, NotificationItem>();
    
    items.forEach(item => {
      const cleanMessage = (item.message || '').trim();
      const key = `${cleanMessage}_${item.taskId || ''}`;
      
      if (!uniqueMap.has(key)) {
        uniqueMap.set(key, item);
      } else {
        const existing = uniqueMap.get(key)!;
        if (existing.id?.startsWith('_') && item.id && !item.id.startsWith('_')) {
          uniqueMap.set(key, item);
        }
      }
    });

    return Array.from(uniqueMap.values());
  }

  private sortNotifications(items: NotificationItem[]): NotificationItem[] {
    const deduplicated = this.deduplicateNotifications(items);
    return deduplicated.sort((a, b) => {
      const timeA = new Date(a.time).getTime();
      const timeB = new Date(b.time).getTime();
      return timeB - timeA;
    });
  }

  private loadFromStorage(): void {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved) {
      try {
        const now = new Date().getTime();
        const parsed: NotificationItem[] = JSON.parse(saved)
          .map((item: any) => {
            const timeVal = item.time ? new Date(item.time) : new Date();
            const expiresVal = item.expiresAt 
              ? new Date(item.expiresAt) 
              : new Date(timeVal.getTime() + this.EXPIRATION_TIME_MS);

            return {
              ...item,
              time: timeVal,
              expiresAt: expiresVal
            };
          })
          .filter((item: NotificationItem) => {
            if (!item.expiresAt || isNaN(new Date(item.expiresAt).getTime())) return true;
            return new Date(item.expiresAt).getTime() > now;
          });

        const sorted = this.sortNotifications(parsed);
        this.notificationSubject.next(sorted);
        this.saveToStorage(sorted);
      } catch (e: unknown) {
        this.notificationSubject.next([]);
      }
    }
  }

  private fetchNotificationsFromDatabase(): void {
    const token = this.authService.getAccessToken();
    if (!token) return;

    const currentCulture = this.localizationService.currentLang || 
                           navigator.language.substring(0, 2);

    this.restService.request<void, any[]>({
      method: 'GET',
      url: '/api/app/notification/user-notifications',
      headers: {
        'Abp-Culture': currentCulture,
        'Accept-Language': currentCulture
      }
    }).subscribe({
      next: (res) => {
        if (res && Array.isArray(res)) {
          this.zone.run(() => {
            const newMappedItems: NotificationItem[] = res.map((dbItem: any) => {
              const dbId = dbItem.id || dbItem.Id;
              const dbMessage = dbItem.message || dbItem.Message;
              const dbTaskId = dbItem.taskId || dbItem.TaskId || dbItem.taskID || dbItem.TaskID || dbItem.entityId || dbItem.EntityId;              const dbTime = dbItem.creationTime || dbItem.CreationTime || dbItem.time || new Date();
              const isRead = dbItem.isRead || dbItem.IsRead || false;
              const timeVal = new Date(dbTime);

              return {
                id: dbId || ('_' + Math.random().toString(36).substr(2, 9)),
                taskId: dbTaskId,
                message: dbMessage,
                time: timeVal,
                expiresAt: new Date(timeVal.getTime() + this.EXPIRATION_TIME_MS),
                isRead: isRead,
                read: isRead
              };
            });

            const sorted = this.sortNotifications(newMappedItems);
            this.notificationSubject.next([...sorted]);
            this.saveToStorage(sorted);
          });
        }
      },
      error: (err) => {
        console.warn('Không thể tải lịch sử thông báo từ DB:', err);
      }
    });
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
      const sorted = this.sortNotifications(validList);
      this.notificationSubject.next(sorted);
      this.saveToStorage(sorted);
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

    if (this.hubConnection) {
      try {
        this.hubConnection.off('ReceiveNotification');
        await this.hubConnection.stop();
      } catch (e) {}
      this.hubConnection = null;
    }

    const baseUrl = (this.restService as any).apiURL || 'https://localhost:44399';
    const hubUrl = `${baseUrl.replace(/\/$/, '')}/signalr-hubs/notification`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (arg1: any, arg2?: string) => {
      let message = '';
      let taskId: string | undefined = undefined;
      let notificationId = '';
      let timeVal = new Date();

      if (typeof arg1 === 'object' && arg1 !== null) {
        message = arg1.message || arg1.Message || '';
        taskId = arg1.taskId || arg1.TaskId || arg1.taskID || arg1.TaskID || arg1.entityId || arg1.EntityId;
        notificationId = arg1.id || arg1.Id || ('_' + Math.random().toString(36).substr(2, 9));
        const rawTime = arg1.creationTime || arg1.CreationTime || arg1.time || arg1.Time;
        timeVal = rawTime ? new Date(rawTime) : new Date();
      } 
      else if (typeof arg1 === 'string') {
        message = arg1;
        taskId = arg2;
        notificationId = '_' + Math.random().toString(36).substr(2, 9);
      }

      if (!message) return;

      this.zone.run(() => {
        const currentList = this.notificationSubject.value;

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
        const sorted = this.sortNotifications(updatedList);
        
        this.notificationSubject.next(sorted);
        this.saveToStorage(sorted);
      });
    });

    try {
      await this.hubConnection.start();
      this.isStarting = false;
    } catch (err: unknown) {
      this.isStarting = false;
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  public async onNotificationClick(notification: NotificationItem): Promise<void> {
    console.log("Dữ liệu thông báo khi click:", notification);
    
    // Đánh dấu đã đọc cục bộ
    this.markAsRead(notification.id);

    // Gửi request lên backend đánh dấu đã đọc
    if (notification.id && !notification.id.startsWith('_')) {
      try {
        await this.restService.request<void, void>({
          method: 'POST',
          url: `/api/app/notification/${notification.id}/mark-as-read`,
        }).toPromise();
      } catch (err) {
        console.warn('Không thể gửi request mark-as-read lên server:', err);
      }
    }

    // Lấy Task ID an toàn từ mọi biến thể (taskId, TaskId, taskID, entityId...)
    const targetTaskId = notification.taskId || 
                         (notification as any).TaskId || 
                         (notification as any).taskID || 
                         (notification as any).entityId || 
                         (notification as any).EntityId;

    // Chuyển hướng router bên trong NgZone
    this.zone.run(() => {
      if (targetTaskId) {
        this.router.navigate(['/tasks/detail', targetTaskId]);
      } else {
        this.router.navigate(['/tasks']);
      }
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
    const sorted = this.sortNotifications(updated);
    this.notificationSubject.next(sorted);
    this.saveToStorage(sorted);
  }

  markAllAsRead(): void {
    this.markAsRead();
  }

  public async removeNotification(idOrMessage: string): Promise<void> {
    const currentList = this.notificationSubject.value;
    const itemToRemove = currentList.find(item => item.id === idOrMessage || item.message === idOrMessage);

    if (itemToRemove && itemToRemove.id && !itemToRemove.id.startsWith('_')) {
      try {
        await this.restService.request<void, void>({
          method: 'DELETE',
          url: `/api/app/notification/${itemToRemove.id}`,
        }).toPromise();
      } catch (err) {}
    }

    const updated = currentList.filter(item => 
      item.id !== idOrMessage && 
      (item as any).key !== idOrMessage && 
      item.message !== idOrMessage
    );

    const sorted = this.sortNotifications(updated);
    this.notificationSubject.next(sorted);
    this.saveToStorage(sorted);
  }

  deleteNotification(idOrMessage: string): void {
    this.removeNotification(idOrMessage);
  }

  public async clearAll(): Promise<void> {
    const currentList = this.notificationSubject.value;

    for (const item of currentList) {
      if (item.id && !item.id.startsWith('_')) {
        try {
          await this.restService.request<void, void>({
            method: 'DELETE',
            url: `/api/app/notification/${item.id}`,
          }).toPromise();
        } catch (e) {}
      }
    }

    this.notificationSubject.next([]);
    localStorage.removeItem(this.STORAGE_KEY);
  }

  clearNotifications(): void {
    this.clearAll();
  }
}