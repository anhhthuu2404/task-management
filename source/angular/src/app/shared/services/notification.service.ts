import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@abp/ng.core';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private notificationSubject = new BehaviorSubject<any[]>([]);
  public notifications$ = this.notificationSubject.asObservable();

  private hubConnection!: signalR.HubConnection;
  private readonly authService = inject(AuthService);

  constructor() {
    // Đợi một nhịp hoặc check token rồi mới start kết nối
    setTimeout(() => {
      this.startConnection();
    }, 500);
  }

  private startConnection() {
    const accessToken = this.authService.getAccessToken();

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
      const newNotification = {
        message: message,
        time: new Date()
      };
      this.notificationSubject.next([newNotification, ...currentList]);
    });
  }
}