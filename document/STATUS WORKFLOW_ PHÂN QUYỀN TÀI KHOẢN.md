# SƠ ĐỒ KIẾN TRÚC LUỒNG XỬ LÝ: STATUS WORKFLOW & PHÂN QUYỀN TÀI KHOẢN

```mermaid
graph TD
    classDef frontend fill:#eef2ff,stroke:#4f46e5,stroke-width:2px,color:#000000;
    classDef backend fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#000000;
    classDef db fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#000000;

    subgraph Frontend [1. Frontend Layer - Angular Application]
        UI[Giao diện Task Detail & Modals <br/> task-detail.component.html <br/> - Dynamic Status Badges & Action Buttons <br/> - Modal Submit Review & Modal Reject <br/> - State Handling: InProgress, InReview, Completed]
        Service[TaskService & Shared Services <br/> - RestService / ToasterService <br/> - FileReader Base64 Encoding <br/> - ChangeDetectorRef UI Updates]
        
        UI -->|1. Sự kiện: Submit Review / Approve / Reject / Dropdown Status| Service
    end

    subgraph Backend [2. Backend Layer - TaskManagement Solutions]
        Host[API Host <br/> TaskManagement.HttpApi.Host <br/> /api/app/task/*]
        App[TaskAppService <br/> - SubmitForReviewAsync <br/> - ApproveTaskAsync <br/> - RejectTaskAsync <br/> - Role-based Status Workflow Engine]
        DomainEF[Repositories & Unit of Work <br/> - TaskItem State Machine <br/> - Physical File Storage Manager <br/> - TaskActivityLog Generator]

        Host -->|3. Route Request, Authentication & Permission Check| App
        App -->|4. Kiểm tra Role Assignee/Manager, Đổi Status & Ghi Log Activity| DomainEF
    end

    subgraph Database [3. Database Layer]
        DB[(SQL Server Database <br/> Tasks, TaskFiles, <br/> TaskActivityLogs, AbpUsers)]
    end

    Service -->|2. HTTP POST/PUT Requests: /submit-for-review, /approve, /reject| Host
    DomainEF -->|5. Thực thi LINQ / Cập nhật Task State, File Path & Save Audit Logs| DB

    class UI,Service frontend;
    class Host,App,DomainEF backend;
    class DB db;