# TÀI LIỆU PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG: SEQUENCE DIAGRAM CHI TIẾT
**Dự án:** Hệ thống Quản lý Công việc (Task Management – ABP .NET + Angular)  

---

## 1. SEQUENCE DIAGRAM – LUỒNG TẠO TASK MỚI (UC02 & UC01)
```mermaid
sequenceDiagram
    autonumber
    actor TL as Team Leader / PM / Admin
    participant UI as Angular UI (Client)
    participant Auth as ABP Auth Service
    participant AppSrv as TaskAppService (.NET)
    participant Domain as Task Domain Model
    participant DB as Database

    TL->>UI: Chọn "Tạo Task mới" & điền thông tin (UC02)
    UI->>Auth: Kiểm tra Token & Quyền Tasks.Create (UC01)
    Auth-->>UI: Xác thực thành công (Authorized)
    
    UI->>AppSrv: POST /api/app/tasks (CreateTaskDto)
    activate AppSrv
    
    AppSrv->>Domain: Khởi tạo Entity Task (Status = NEW, CreatorId)
    Domain->>Domain: Tự động sinh mã định danh (TASK-YYYY-XXXXX)
    
    AppSrv->>DB: Insert Task entity & ghi vết TaskHistory
    activate DB
    DB-->>AppSrv: Lưu thành công
    deactivate DB
    
    AppSrv-->>UI: Trả về TaskDto (201 Created)
    deactivate AppSrv
    
    UI-->>TL: Hiển thị thông báo thành công & cập nhật danh sách
```
## 2. SEQUENCE DIAGRAM – LUỒNG GÁN NGƯỜI THỰC HIỆN & THÔNG BÁO (UC07 & UC18)
```mermaid
sequenceDiagram
    autonumber
    actor TL as Team Leader / PM
    participant UI as Angular UI
    participant AppSrv as TaskAppService
    participant NotifSrv as Notification Manager
    participant DB as Database
    actor Emp as Assignee (Employee)

    TL->>UI: Chọn Task, chỉ định User & phân định vai trò (UC07)
    UI->>AppSrv: PUT /api/app/tasks/{id}/assign (AssignTaskDto)
    activate AppSrv
    
    AppSrv->>DB: Cập nhật TaskUser (ASSIGNEE/REVIEWER...) & ghi TaskHistory
    DB-->>AppSrv: Cập nhật thành công
    
    AppSrv->>NotifSrv: Kích hoạt sự kiện thông báo (UC18)
    NotifSrv->>DB: Lưu bản ghi AppNotifications
    NotifSrv-->>Emp: Gửi thông báo Real-time (SignalR)
    
    AppSrv-->>UI: Trả kết quả thành công (200 OK)
    deactivate AppSrv
    
    UI-->>TL: Cập nhật giao diện chi tiết Task
    Emp-->>Emp: Nhận thông báo thời gian thực trên hệ thống
```
## 3. SEQUENCE DIAGRAM – LUỒNG PHÊ DUYỆT WORKFLOW (UC15, UC16, UC17 & UC08)
```mermaid
sequenceDiagram
    autonumber
    actor Emp as Employee (Assignee)
    actor TL as Team Leader / Reviewer
    participant UI as Angular UI
    participant AppSrv as TaskAppService
    participant Domain as Task Domain Model
    participant DB as Database

    Emp->>UI: Chuyển trạng thái sang Review & Submit (UC15)
    UI->>AppSrv: PUT /api/app/tasks/{id}/submit-review
    activate AppSrv
    
    AppSrv->>Domain: Task.Status = REVIEW & tạo bản ghi TaskApproval (PENDING)
    AppSrv->>DB: Lưu thay đổi vào CSDL
    DB-->>AppSrv: Lưu thành công
    AppSrv-->>UI: Trả kết quả 200 OK
    deactivate AppSrv
    
    UI-->>Emp: Hiển thị trạng thái "Chờ duyệt"
    
    TL->>UI: Kiểm tra Task & chọn Phê duyệt hoặc Từ chối
    alt Phê duyệt (Approve - UC16)
        UI->>AppSrv: PUT /api/app/tasks/{id}/approve
        activate AppSrv
        AppSrv->>Domain: Task.Status = COMPLETED & TaskApproval.Status = APPROVED
    else Từ chối (Reject - UC17)
        UI->>AppSrv: PUT /api/app/tasks/{id}/reject (Kèm lý do)
        activate AppSrv
        AppSrv->>Domain: Task.Status = REJECTED & TaskApproval.Status = REJECTED
    end
    
    AppSrv->>DB: Cập nhật vòng đời Status (UC08) & ghi TaskHistory
    DB-->>AppSrv: Commit thành công
    AppSrv-->>UI: Trả kết quả thành công
    deactivate AppSrv
    
    UI-->>TL: Cập nhật giao diện hoàn tất
```
## 4. SEQUENCE DIAGRAM – LUỒNG QUẢN LÝ DỰ ÁN & THÀNH VIÊN (UC19, UC20, UC21)
```mermaid
sequenceDiagram
    autonumber
    actor PM as Project Manager
    participant UI as Angular UI
    participant AppSrv as ProjectAppService
    participant DB as Database

    PM->>UI: Nhập thông tin tạo Project & Milestones (UC19, UC20)
    UI->>AppSrv: POST /api/app/projects (CreateProjectDto)
    activate AppSrv
    AppSrv->>DB: Insert Project & Milestone entities
    DB-->>AppSrv: Thành công
    AppSrv-->>UI: Trả về ProjectDto (201 Created)
    deactivate AppSrv

    PM->>UI: Phân bổ thành viên vào dự án (UC21)
    UI->>AppSrv: POST /api/app/projects/{id}/members
    activate AppSrv
    AppSrv->>DB: Insert vào bảng ProjectMembers
    DB-->>AppSrv: Thành công
    AppSrv-->>UI: Trả kết quả 200 OK
    deactivate AppSrv
    
    UI-->>PM: Hiển thị sơ đồ quản lý dự án hoàn tất
```
## 5. SEQUENCE DIAGRAM – LUỒNG KANBAN KÉO THẢ TRẠNG THÁI (UC26 & UC08)
```mermaid
sequenceDiagram
    autonumber
    actor User as Employee / TeamLeader
    participant UI as Angular UI (Kanban View)
    participant AppSrv as TaskAppService
    participant DB as Database

    User->>UI: Kéo Task sang cột trạng thái mới (UC26)
    UI->>AppSrv: PUT /api/app/tasks/{id}/status (UpdateStatusDto)
    activate AppSrv
    
    AppSrv->>AppSrv: Kiểm tra tính hợp lệ vòng đời trạng thái (UC08)
    AppSrv->>DB: Cập nhật Task.Status mới & ghi vết TaskHistory
    DB-->>AppSrv: Lưu thành công
    
    AppSrv-->>UI: Trả dữ liệu Task đã cập nhật
    deactivate AppSrv
    
    UI-->>User: Cập nhật trực quan vị trí thẻ Task trên Kanban Board
```
## 6. SEQUENCE DIAGRAM – LUỒNG TỰ ĐỘNG HÓA BACKGROUND JOB (UC30 & UC31)
```mermaid
sequenceDiagram
    autonumber
    participant Job as Background Worker (System)
    participant AppSrv as Task Background Service
    participant DB as Database
    actor User as Assignee / Manager

    Note over Job, DB: Thực thi định kỳ tự động (Hangfire / ABP Background Job)
    
    Job->>AppSrv: Quét Task quá hạn (UC31)
    activate AppSrv
    AppSrv->>DB: SELECT * WHERE DueDate < Now AND Status != COMPLETED
    DB-->>AppSrv: Trả về danh sách Task quá hạn
    
    loop Xử lý từng Task quá hạn
        AppSrv->>DB: Cập nhật Status = OVERDUE & Tạo bản ghi Notification
    end
    AppSrv-->>Job: Hoàn thành quét quá hạn
    deactivate AppSrv
    
    Job->>AppSrv: Kiểm tra cấu hình lặp lại (UC30)
    activate AppSrv
    AppSrv->>DB: Tự động sinh Task mới theo chu kỳ (Recurring Task)
    DB-->>AppSrv: Lưu Task mới thành công
    deactivate AppSrv

    User-->>User: Nhận thông báo Task Overdue / Task mới tự động