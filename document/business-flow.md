##  QUY TRÌNH NGHIỆP VỤ ĐỒNG BỘ (BUSINESS FLOW)
### Luồng vận hành tổng thể của hệ thống được mô tả trực quan qua sơ đồ hoạt động (Activity Diagram), thể hiện toàn bộ vòng đời từ khâu khởi tạo dự án, phân công, thực hiện công việc cho đến quy trình phê duyệt khép kín và các tác vụ tự động hóa ngầm:
```mermaid
flowchart TD
    Start([Bắt đầu Dự án]) --> PM_Create[PM tạo Project, Milestone & Phân bổ Member<br/>UC19, UC20, UC21]
    PM_Create --> TL_Create[Team Leader/PM tạo Task mới<br/>UC02]
    TL_Create --> Assign[Assign Task & phân định vai trò Assignee/Reviewer<br/>UC07, UC18]
    Assign --> InProgress[Employee bắt đầu làm<br/>Status: IN_PROGRESS - UC08]

    subgraph Execution [Nhánh thực thi của Employee]
        direction TB
        InProgress --> CheckAction{Hành động?}

        CheckAction -->|Cập nhật Code/Document| NormalWork[Thực hiện công việc thông thường]
        CheckAction -->|Bổ sung Task con| CreateSub[Tạo/Cập nhật SubTask - UC10]
        CheckAction -->|Tạo Checklist| CreateCheck[Tick ChecklistItem - UC11]

        NormalWork --> ProgressCheck
        CreateSub --> UpdateProgress[Hệ thống tự tính lại % Tiến độ - UC09]
        CreateCheck --> UpdateProgress
        UpdateProgress --> ProgressCheck{Task đã sẵn sàng nộp?}

        ProgressCheck -->|Chưa xong| CheckAction
        ProgressCheck -->|Sẵn sàng| SubmitReview[Nhấn Submit Review<br/>Status: REVIEW - UC15]
    end

    SubmitReview --> ApprovalDualCheck{Dual Validation Rule<br/>Role=REVIEWER AND Rank>=TeamLeader?}

    ApprovalDualCheck -->|Fail| DenyAction[403 Forbidden - Task vẫn ở Status: REVIEW]
    DenyAction --> WaitRetry[Chờ Reviewer hợp lệ khác thao tác lại - thủ công]
    WaitRetry -.->|Reviewer hợp lệ thực hiện Approve/Reject| ApprovalDualCheck

    ApprovalDualCheck -->|Pass| ApprovalAction{Approve hay Reject?}

    ApprovalAction -->|Approve - UC16| Complete[Status: COMPLETED]
    ApprovalAction -->|Reject - UC17| RejectSet[Status: REJECTED kèm lý do]
    RejectSet --> NotifyEmp[Gửi Notification cho Employee - UC18]
    NotifyEmp --> ResumeWork[Employee bấm Resume<br/>Status: REJECTED -> IN_PROGRESS]
    ResumeWork --> CheckAction

    Complete --> NotifyDone[Gửi Notification hoàn tất - UC18]
    NotifyDone --> End([Kết thúc luồng chính])

    subgraph Background [System Background Jobs - chạy song song, độc lập với luồng chính]
        direction TB
        Job[Background Worker chạy định kỳ - Hangfire] --> Overdue{Task quá hạn?<br/>DueDate < Now AND Status != COMPLETED}
        Overdue -->|Đúng| Mark[IsOverdue = true & Gửi Notification<br/>UC31]
        Overdue -->|Chưa| Job

        Job --> Recurring{Có RecurringTaskConfig Active?}
        Recurring -->|Đúng và đến NextRunTime| CreateRecur[Sinh Task mới GeneratedFromTaskId<br/>UC30]
        Recurring -->|Chưa| Job
    end
```
