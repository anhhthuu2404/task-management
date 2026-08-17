```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px,color:#000000;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#000000;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#000000;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện UI <br/> category.component.* <br/> tag.component.*]
        Service[Giao tiếp API <br/> category.service.ts <br/> tag.service.ts]
        
        UI -->|1. Event Handling & Form Validation| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        Host[API Host <br/> TaskManagement.HttpApi.Host]
        App[Nghiệp vụ Application <br/> TaskManagement.Application <br/> CategoryAppService & TagAppService]
        DomainEF[Dữ liệu & EF Core <br/> TaskManagement.Domain <br/> TaskManagement.EntityFrameworkCore]

        Host -->|3. Route Request, JWT Auth & Permission| App
        App -->|4. Process Business Logic & Mapping DTO| DomainEF
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Bảng Categories & Tags)]
    end

    Service -->|2. Send HTTP Request GET/POST/PUT/DELETE| Host
    DomainEF -->|5. Execute LINQ / SQL Queries & Unit of Work| DB

    class UI,Service frontend;
    class Host,App,DomainEF backend;
    class DB db;