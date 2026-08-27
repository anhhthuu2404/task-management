```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px,color:#000000;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#000000;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#000000;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện UI <br/> task-list.component.*]
        Service[Giao tiếp API <br/> task.service.ts]
        
        UI -->|1. Event Handling & Auto-Refresh| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        Host[API Host <br/> TaskManagement.HttpApi.Host <br/> TaskOverdueBackgroundWorker]
        App[Nghiệp vụ Application <br/> TaskManagement.Application <br/> TaskAppService]
        DomainEF[Dữ liệu & EF Core <br/> TaskManagement.Domain <br/> TaskManagement.EntityFrameworkCore]

        Host -->|3. Route Request & Background Trigger| App
        App -->|4. Process Business Logic & Recurring Calculation| DomainEF
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Bảng Tasks)]
    end

    Service -->|2. Send HTTP Request GET| Host
    DomainEF -->|5. Execute LINQ / SQL Queries & Unit of Work| DB

    class UI,Service frontend;
    class Host,App,DomainEF backend;
    class DB db;