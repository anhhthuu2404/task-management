import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@abp/ng.core';

export interface NotificationItem {
  id?: string;
  message: string;
  time: Date;
  isRead?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private notificationSubject = new BehaviorSubject<NotificationItem[]>([]);
  public notifications$ = this.notificationSubject.asObservable();

  private hubConnection!: signalR.HubConnection;
  private readonly authService = inject(AuthService);

  constructor() {
    setTimeout(() => {
      this.startConnection();
    }, 500);
  }

  private startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:44399/signalr-hubs/notification', {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connected Successfully!'))
      .catch(err => console.log('Error while starting SignalR connection: ' + err));

    this.hubConnection.on('ReceiveNotification', (message: string) => {
      console.log('Nhận được thông báo từ Hub:', message);
      const currentList = this.notificationSubject.value;
      const newNotification: NotificationItem = {
        id: '_' + Math.random().toString(36).substr(2, 9),
        message: message,
        time: new Date(),
        isRead: false
      };
      this.notificationSubject.next([newNotification, ...currentList]);
    });
  }

  markAsRead(id?: string): void {
    const currentList = this.notificationSubject.value;
    if (id) {
      const updated = currentList.map(item => 
        item.id === id ? { ...item, isRead: true } : item
      );
      this.notificationSubject.next(updated);
    } else {
      const updated = currentList.map(item => ({ ...item, isRead: true }));
      this.notificationSubject.next(updated);
    }
  }

  clearAll(): void {
    this.notificationSubject.next([]);
  }
}