
```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px,color:#000000;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#000000;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#000000;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện UI <br/> task-list.component.* <br/> Toast & UI State]
        Service[Quản lý SignalR <br/> notification.service.ts]
        
        UI -->|1. Subscribe Observable & Trigger UI Refresh| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        Host[API Host & SignalR Hub <br/> TaskManagement.HttpApi.Host <br/> NotificationHub]
        App[Nghiệp vụ Application <br/> TaskManagement.Application <br/> TaskAppService]
        DomainEF[Dữ liệu & EF Core <br/> TaskManagement.Domain <br/> TaskManagement.EntityFrameworkCore]

        Host -->|3. Establish WebSocket Connection & JWT Auth| App
        App -->|4. Push Real-time Event on Task Create/Update| Host
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Task Tables & Notifications)]
    end

    Service -->|2. Connect WebSocket & Listen to Hub Events| Host
    DomainEF -->|5. Save State & Execute Transactions| DB

    class UI,Service frontend;
    class Host,App,DomainEF backend;
    class DB db;