```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px,color:#000000;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#000000;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#000000;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện UI <br/> dashboard.component.* <br/> KPI Cards & Chart.js Config]
        Service[Quản lý API & Hiệu năng <br/> dashboard.service.ts & RxJS forkJoin]
        
        UI -->|1. Subscribe Observable & Trigger UI Refresh| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        App[Nghiệp vụ Application <br/> TaskManagement.Application <br/> DashboardAppService]
        DomainEF[Dữ liệu & EF Core <br/> TaskManagement.Domain & Repositories <br/> TaskItem, Project, Department, Category, Tag, User]

        App -->|3. Query Statistics & Aggregate Data| DomainEF
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Tasks, Users, Departments, Roles Tables)]
    end

    Service -->|2. Send GET /api/app/dashboard/statistics| App
    DomainEF -->|4. Fetch Aggregated Metrics & Execute Queries| DB

    class UI,Service frontend;
    class App,DomainEF backend;
    class DB db;