# SƠ ĐỒ THỰC THỂ MỐI QUAN HỆ (ERD DIAGRAM)
### Sơ đồ ERD chuẩn hóa (3NF) hỗ trợ toàn bộ các thực thể xuất hiện trong luồng xử lý Task, Project, Workflow phê duyệt, Bình luận và Thông báo nền.

```mermaid
erDiagram
    Users ||--o{ ProjectMembers : participates
    Projects ||--o{ ProjectMembers : has
    Projects ||--o{ Milestones : contains
    Projects ||--o{ Tasks : includes
    Users ||--o{ Tasks : creates
    Tasks ||--o{ Tasks : sub_tasks
    Tasks ||--o{ Tasks : recurring_generated_tasks
    Tasks ||--o{ TaskUsers : assigned_to
    Users ||--o{ TaskUsers : performs
    Tasks ||--o{ TaskApprovals : requires
    Tasks ||--o{ TaskHistories : tracks
    Users ||--o{ TaskHistories : triggers
    Tasks ||--o{ Comments : has
    Comments ||--o{ Comments : replies
    Users ||--o{ Comments : writes
    Users ||--o{ AppNotifications : receives
    Tasks ||--o| AppNotifications : triggers
    Projects ||--o{ Categories : has
    Categories ||--o{ Tasks : categorizes
    Tasks ||--o{ Checklists : contains
    Checklists ||--o{ ChecklistItems : has
    Tasks ||--o{ Attachments : has
    Tasks ||--o{ TaskTags : tagged_with
    Tags ||--o{ TaskTags : applies_to
    Users ||--o{ Departments : belongs_to
    Tasks ||--o| RecurringTaskConfigs : configures

    Users {
        long Id PK
        long DepartmentId FK
        string UserName
        string Email
        string PasswordHash
        string FullName
    }

    Departments {
        long Id PK
        string Name
    }

    Projects {
        long Id PK
        string Name
        string Description
        string Status
        datetime StartDate
        datetime EndDate
        long CreatedBy FK
    }

    Milestones {
        long Id PK
        long ProjectId FK
        string Title
        datetime DueDate
    }

    Categories {
        long Id PK
        long? ProjectId FK "Null = Global, NotNull = Project-specific"
        string Name
    }

    Tags {
        long Id PK
        string Name
    }

    ProjectMembers {
        long ProjectId PK, FK
        long UserId PK, FK
        string AssignedRole
    }

    Tasks {
        long Id PK
        long ProjectId FK
        long? MilestoneId FK
        long? CategoryId FK
        long? ParentTaskId FK "SubTask (UC10)"
        long? GeneratedFromTaskId FK "Trace Task con từ Recurring Task"
        string TaskCode
        string Title
        string Description
        string Status
        string Priority
        boolean IsOverdue
        datetime DueDate
        long CreatedBy FK
    }

    TaskUsers {
        long TaskId PK, FK
        long UserId PK, FK
        string RoleType PK "ASSIGNEE, REVIEWER, COLLABORATOR, WATCHER"
    }

    TaskTags {
        long TaskId PK, FK
        long TagId PK, FK
    }

    Checklists {
        long Id PK
        long TaskId FK
        string Title
    }

    ChecklistItems {
        long Id PK
        long ChecklistId FK
        string Content
        boolean IsCompleted
    }

    Attachments {
        long Id PK
        long TaskId FK
        string FileName
        string FilePath
        long UploadedBy FK
    }

    TaskApprovals {
        long Id PK
        long TaskId FK
        long ReviewerId FK
        string ApprovalStatus
        string Reason
    }

    TaskHistories {
        long Id PK
        long TaskId FK
        long UserId FK
        string Action
        string Description
        datetime Timestamp
    }

    Comments {
        long Id PK
        long TaskId FK
        long? ParentCommentId FK "Reply Comment"
        long UserId FK
        string Content
        datetime CreationTime
    }

    AppNotifications {
        long Id PK
        long UserId FK
        long? TaskId FK "Liên kết nguồn thông báo tới Task cụ thể"
        string Title
        string Message
        bool IsRead
        datetime CreationTime
    }

    RecurringTaskConfigs {
        long Id PK
        long TaskId FK "Task gốc mẫu"
        string CronExpression
        datetime NextRunTime
        boolean IsActive
    }
```
