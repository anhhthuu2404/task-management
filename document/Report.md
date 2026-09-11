```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện Báo cáo <br/> report.component.* <br/> Bộ lọc theo Employee, Department, Project & Nút Export] 
        Service[Quản lý API & Xử lý File <br/> report.service.ts]
        
        UI -->|1. Chọn bộ lọc & Nhấn Xuất/Xem| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        App[Nghiệp vụ Application <br/> ReportAppService]
        DomainEF[Tổng hợp dữ liệu & Tạo File <br/> LINQ Aggregation & Thư viện Export Excel/PDF]
        
        App -->|2. Nhận tham số & Truy vấn dữ liệu| DomainEF
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Employees, Departments, Projects, Tasks)]
        
        DomainEF -->|3. Query & Join dữ liệu báo cáo| DB
    end

    Service -->|Truyền params filter| App
    DomainEF -->|Trả về dữ liệu JSON / File Stream| Service
    Service -->|Hiển thị bảng dữ liệu / Tải file về máy| UI

    class UI,Service frontend;
    class App,DomainEF backend;
    class DB db;