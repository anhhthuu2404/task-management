# TÀI LIỆU PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG: SƠ ĐỒ USE CASE & QUY TRÌNH NGHIỆP VỤ
**Dự án:** Hệ thống Quản lý Công việc (Task Management – ABP .NET + Angular)  
**Phạm vi:** Tài liệu chuyển đổi chi tiết từ yêu cầu hệ thống sang các sơ đồ phân tích kỹ thuật chuẩn Mermaid và quy trình nghiệp vụ.

---

## PHẦN 1: TỔNG QUAN VỀ TÁC NHÂN (ACTOR) VÀ PHÂN QUYỀN

Hệ thống định nghĩa 5 nhóm Actor chính tương ứng với các cấp độ phân quyền trong ABP Framework, cùng với một System Actor chuyên trách các tác vụ nền tự động:

1. **Employee (Nhân viên):** Actor cơ sở, có quyền xem Task được giao, cập nhật tiến độ, trạng thái, bình luận, upload tài liệu và submit review.
2. **Team Leader (Trưởng nhóm):** Kế thừa toàn bộ quyền của Employee, đồng thời có thể tạo/giao task, review task và theo dõi khối lượng công việc của cả team.
3. **Project Manager (Quản lý dự án):** Kế thừa quyền của Team Leader, chịu trách nhiệm quản lý toàn diện Project, Milestone, phân bổ nguồn lực, phê duyệt và xem báo cáo.
4. **Admin (Quản trị viên hệ thống):** Cấp độ cao nhất, kế thừa Project Manager và nắm giữ toàn quyền quản trị người dùng, phòng ban, danh mục, phân quyền hệ thống.
5. **Viewer (Người xem):** Actor độc lập với quyền hạn chế, chỉ có thể xem dữ liệu mà không được phép tác động hay chỉnh sửa.
6. **System (Hệ thống / Background Job):** Actor ngầm định thực thi tự động các tác vụ định kỳ như gửi thông báo, đánh dấu quá hạn, hoặc tạo Task lặp lại.

### Sơ đồ quan hệ phân cấp Actor
```mermaid
flowchart TD
    Employee["Employee"]
    TeamLeader["Team Leader"]
    PM["Project Manager"]
    Admin["Admin"]
    Viewer["Viewer"]

    Employee -.->|"kế thừa"| TeamLeader
    TeamLeader -.->|"kế thừa"| PM
    PM -.->|"kế thừa"| Admin
```
 ## PHẦN 2: CÁC SƠ ĐỒ USE CASE THEO MODULE (CHUẨN MERMAID)
### 2.1. Module Task Core (Lõi nghiệp vụ công việc)
flowchart TD
    Employee["Employee"]
    TeamLeader["Team Leader"]
    PM["Project Manager"]
    Viewer["Viewer"]

    subgraph SYS[" TASK CORE "]
        UC01(["UC01: Đăng nhập"])
        UC02(["UC02: Tạo Task"])
        UC03(["UC03: Xem DS/Chi tiết Task"])
        UC04(["UC04: Cập nhật Task"])
        UC05(["UC05: Xoá Task"])
        UC06(["UC06: Search/Filter/Pagination"])
        UC07(["UC07: Assign (Assignee/Reviewer/Collaborator/Watcher)"])
        UC08(["UC08: Cập nhật Status"])
        UC09(["UC09: Cập nhật Progress"])
        UC10(["UC10: Quản lý SubTask"])
        UC11(["UC11: Quản lý Checklist"])
    end

    Employee --> UC01
    Employee --> UC03
    Employee --> UC06
    Employee --> UC08
    Employee --> UC09
    Employee --> UC11

    TeamLeader --> UC02
    TeamLeader --> UC04
    TeamLeader --> UC07
    TeamLeader --> UC10

    PM --> UC05

    Viewer --> UC01
    Viewer --> UC03
    Viewer --> UC06

    UC02 -.->|include| UC01
    UC03 -.->|include| UC01
    UC04 -.->|extend| UC03
    UC05 -.->|extend| UC03
    UC07 -.->|extend| UC03
    UC08 -.->|extend| UC03
    UC09 -.->|extend| UC03
    UC10 -.->|extend| UC02
    UC11 -.->|extend| UC10

### 2.2. Module Cộng tác & Workflow (Quy trình phê duyệt)
flowchart TD
    Employee["Employee"]
    TeamLeader["Team Leader"]
    PM["Project Manager"]

    subgraph SYS[" COLLABORATION & WORKFLOW "]
        UC12(["UC12: Comment / Reply"])
        UC13(["UC13: Upload Attachment"])
        UC14(["UC14: Xem History"])
        UC15(["UC15: Submit Review"])
        UC16(["UC16: Approve Task"])
        UC17(["UC17: Reject Task"])
        UC18(["UC18: Nhận Notification"])
    end

    Employee --> UC12
    Employee --> UC13
    Employee --> UC14
    Employee --> UC15
    Employee --> UC18

    TeamLeader --> UC16
    TeamLeader --> UC17
    TeamLeader --> UC18

    PM --> UC16
    PM --> UC17

    UC15 -.->|extend| UC08
    UC16 -.->|"include"| UC15
    UC17 -.->|"include"| UC15
    UC18 -.->|extend| UC07
    UC18 -.->|extend| UC12
    UC18 -.->|extend| UC16
    UC18 -.->|extend| UC17
### 2.3. Module Project / Danh mục & Tổ chức
flowchart TD
    TeamLeader["Team Leader"]
    PM["Project Manager"]
    Admin["Admin"]

    subgraph SYS[" PROJECT & TỔ CHỨC "]
        UC19(["UC19: Quản lý Project"])
        UC20(["UC20: Quản lý Milestone"])
        UC21(["UC21: Quản lý ProjectMember"])
        UC22(["UC22: Quản lý Category"])
        UC23(["UC23: Quản lý Tag"])
        UC24(["UC24: Quản lý User & Department"])
        UC25(["UC25: Phân quyền hệ thống"])
    end

    PM --> UC19
    PM --> UC20
    PM --> UC21
    PM --> UC23

    Admin --> UC22
    Admin --> UC24
    Admin --> UC25

    UC20 -.->|extend| UC19
    UC21 -.->|extend| UC19
### 2.4. Module Hiển thị nâng cao & Báo cáo
flowchart TD
    Employee["Employee"]
    PM["Project Manager"]
    Admin["Admin"]

    subgraph SYS[" KANBAN / CALENDAR / DASHBOARD / REPORT "]
        UC26(["UC26: Xem Kanban (Drag & Drop)"])
        UC27(["UC27: Xem Calendar"])
        UC28(["UC28: Xem Dashboard"])
        UC29(["UC29: Xem & Xuất Report"])
    end

    Employee --> UC26
    Employee --> UC27
    Employee --> UC28

    PM --> UC28
    PM --> UC29
    Admin --> UC29

    UC26 -.->|extend| UC08
### 2.5. Module Tự động hoá (System Actor)
flowchart TD
    System["System (Background Job)"]

    subgraph SYS[" TỰ ĐỘNG HOÁ "]
        UC30(["UC30: Tạo Recurring Task"])
        UC31(["UC31: Đánh dấu Task Overdue"])
    end

    System --> UC30
    System --> UC31

    UC31 -.->|extend| UC08
    UC30 -.->|extend| UC02
---

## PHẦN 3: ĐẶC TẢ CHI TIẾT CÁC USE CASE TRỌNG YẾU

### 1. UC02 – Tạo Task
* **Actor:** Team Leader, Project Manager, Admin
* **Include:** UC01 (Đăng nhập)
* **Tiền điều kiện:** Người dùng đã xác thực hệ thống, sở hữu quyền `Tasks.Create`.
* **Luồng chính:**
  1. Người dùng chọn chức năng "Tạo Task mới" trên giao diện.
  2. Điền đầy đủ thông tin: Tiêu đề (`Title`), Mô tả (`Description`), Loại task (`TaskType`), Mức độ ưu tiên (`Priority`), Thời gian (`StartDate/DueDate`), Danh mục (`Category`), Dự án liên quan (`Project`), và Task cha nếu là công việc con (`ParentTaskId`).
  3. Hệ thống tự động sinh mã định danh dạng `TASK-YYYY-XXXXX`, khởi tạo trạng thái `Status = NEW`, và gán `CreatorId` tương ứng.
  4. Lưu trữ bản ghi vào cơ sở dữ liệu và ghi vết lịch sử vào `TaskHistory`.

### 2. UC07 – Gán người thực hiện (Assign)
* **Actor:** Team Leader, Project Manager, Admin
* **Extend:** UC03 (Xem chi tiết Task)
* **Luồng chính:**
  1. Tại màn hình chi tiết Task, người có quyền chọn chức năng "Assign".
  2. Lựa chọn tài khoản người dùng và phân định vai trò cụ thể trong bảng `TaskUser`: `ASSIGNEE`, `REVIEWER`, `COLLABORATOR`, hoặc `WATCHER`.
  3. Hệ thống tự động chuyển trạng thái Task thành `ASSIGNED` nếu đây là lần gán `Assignee` đầu tiên.
  4. Kích hoạt UC18 để gửi thông báo thời gian thực.

### 3. UC08 – Cập nhật trạng thái Task (UpdateStatus)
* **Actor:** Employee (Assignee), Team Leader, Project Manager, Admin
* **Vòng đời chuẩn:** `NEW -> ASSIGNED -> IN_PROGRESS -> REVIEW -> COMPLETED`
* **Ngoại lệ:** `REVIEW -> REJECTED -> IN_PROGRESS`

### 4. UC15, UC16, UC17 – Luồng Phê duyệt (Workflow)
* **Submit Review (UC15):** Assignee chuyển trạng thái sang `REVIEW`, tạo bản ghi `TaskApproval` ở trạng thái `PENDING`.
* **Approve Task (UC16):** Reviewer duyệt $\rightarrow$ `Task.Status = COMPLETED`.
* **Reject Task (UC17):** Reviewer từ chối kèm lý do $\rightarrow$ `Task.Status = REJECTED`.

---

## PHẦN 4: MA TRẬN QUAN HỆ ACTOR – USE CASE

| Use Case | Employee | Team Leader | Project Manager | Admin | Viewer |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **UC01 Đăng nhập / Đăng xuất** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC02 Tạo Task** | | ✔ | ✔ | ✔ | |
| **UC03 Xem DS / Chi tiết Task** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC04 Cập nhật Task** | | ✔ | ✔ | ✔ | |
| **UC05 Xoá Task** | | | ✔ | ✔ | |
| **UC06 Search / Filter / Pagination** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC07 Gán người thực hiện (Assign)** | | ✔ | ✔ | ✔ | |
| **UC08 Cập nhật trạng thái (Status)** | ✔ | ✔ | ✔ | ✔ | |
| **UC09 Cập nhật tiến độ (Progress)** | ✔ | ✔ | ✔ | ✔ | |
| **UC10 Quản lý SubTask** | | ✔ | ✔ | ✔ | |
| **UC11 Quản lý Checklist** | ✔ | ✔ | ✔ | ✔ | |
| **UC12 Thêm / Trả lời Comment** | ✔ | ✔ | ✔ | ✔ | |
| **UC13 Upload / Quản lý Attachment** | ✔ | ✔ | ✔ | ✔ | |
| **UC14 Xem lịch sử thay đổi (History)** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC15 Submit Review** | ✔ | ✔ | ✔ | | |
| **UC16 Approve Task** | | ✔ | ✔ | ✔ | |
| **UC17 Reject Task** | | ✔ | ✔ | ✔ | |
| **UC18 Nhận Notification** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC19 Quản lý Project (CRUD)** | | | ✔ | ✔ | |
| **UC20 Quản lý Milestone** | | | ✔ | ✔ | |
| **UC21 Quản lý ProjectMember** | | | ✔ | ✔ | |
| **UC22 Quản lý Category** | | | | ✔ | |
| **UC23 Quản lý Tag** | | ✔ | ✔ | ✔ | |
| **UC24 Quản lý User & Department** | | | | ✔ | |
| **UC25 Phân quyền hệ thống (Permission)** | | | | ✔ | |
| **UC26 Xem Kanban Board** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC27 Xem Calendar** | ✔ | ✔ | ✔ | ✔ | ✔ |
| **UC28 Xem Dashboard** | ✔ | ✔ | ✔ | ✔ | |
| **UC29 Xem & Xuất Report** | | | ✔ | ✔ | |
| **UC30 Tạo Recurring Task** | *System* | *System* | *System* | *System* | |
| **UC31 Đánh dấu Overdue tự động** | *System* | *System* | *System* | *System* | |