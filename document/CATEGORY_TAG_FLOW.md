graph TD
    subgraph Frontend [Frontend - Angular Application]
        UI[Template UI: category.component.html / tag.component.html]
        Comp[Component Logic: category.component.ts / tag.component.ts]
        Proxy[Proxy API: category.service.ts / tag.service.ts]
    end

    subgraph Backend [Backend - TaskManagement.sln Solutions]
        Host[TaskManagement.HttpApi.Host / HttpApi]
        Contracts[TaskManagement.Application.Contracts]
        App[TaskManagement.Application]
        Domain[TaskManagement.Domain]
        EF[TaskManagement.EntityFrameworkCore]
    end

    subgraph Database [Database System]
        DB[(Bảng Categories & Tags)]
    end

    %% Client Interactions
    UI -->|1. User click Icon Sửa/Xóa/Thêm| Comp
    Comp -->|2. Validate FormGroup & gọi Proxy| Proxy
    Proxy -->|3. HTTP Request JSON + Bearer Token| Host

    %% Backend Processing
    Host -->|4. Routing Request & Authentication| Contracts
    Contracts -->|5. Kiếm tra DTO / Interfaces| App
    App -->|6. Xử lý AppService CRUD Logic| Domain
    Domain -->|7. Áp dụng Business Rule Entity| EF
    EF -->|8. Truy vấn DbContext LINQ| DB

    %% Database Response & UI Render
    DB -->|9. Trả về Record Dữ liệu| EF
    EF -->|10. Map Entity Data| Domain
    Domain -->|11. AutoMapper Convert Entity -> DTO| App
    App -->|12. Trả DTO cho Host Controller| Host
    Host -->|13. HTTP Response 200 / 201 / 204| Proxy
    Proxy -->|14. RxJS Observable Stream| Comp
    Comp -->|15. Update State & Render ngx-datatable| UI