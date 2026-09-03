import { Component, OnInit, inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ListService, PagedResultDto, PermissionService } from '@abp/ng.core';
import {
  NgxDatatableListDirective,
  ModalComponent,
  ModalCloseDirective,
  ConfirmationService,
  Confirmation,
  ToasterService,
  ThemeSharedModule,
} from '@abp/ng.theme.shared';
import { PageModule } from '@abp/ng.components/page';
import { NgxDatatableModule, ColumnChangesService } from '@swimlane/ngx-datatable';
import { NgbDropdownModule, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { finalize } from 'rxjs/operators';

import { TagService } from '../proxy/tags/tag.service';
import { TagDto } from '../proxy/tags/models';
import { CategoryService } from '../proxy/categories/category.service';
import { CategoryDto } from '../proxy/categories/models';

@Component({
  selector: 'app-tag',
  standalone: true,
  templateUrl: './tag.component.html',
  encapsulation: ViewEncapsulation.None,
  styles: [`
    app-tag .ngx-datatable {
      width: 100% !important;
    }
    
    app-tag .ngx-datatable .datatable-header-inner,
    app-tag .ngx-datatable .datatable-body-row {
      width: 100% !important;
      display: flex !important;
    }

    app-tag .ngx-datatable .datatable-header-cell,
    app-tag .ngx-datatable .datatable-body-cell {
      display: flex !important;
      align-items: center;
      margin: 0 !important;
    }
  `],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgxDatatableModule,
    NgbDropdownModule,
    ModalComponent,
    ModalCloseDirective,
    PageModule,
    NgxDatatableListDirective,
    ThemeSharedModule,
  ],
  providers: [
    ListService,
    ColumnChangesService,
  ],
})
export class TagComponent implements OnInit {
  private readonly service = inject(TagService);
  private readonly categoryService = inject(CategoryService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmation = inject(ConfirmationService);
  private readonly noti = inject(ToasterService);
  private readonly permissionService = inject(PermissionService);
  public readonly list = inject(ListService);

  items: PagedResultDto<TagDto> = { items: [], totalCount: 0 };
  categories: CategoryDto[] = [];
  selected = {} as TagDto;
  form!: FormGroup;
  searchForm!: FormGroup;

  isModalOpen = false;
  loading = false;
  isSaving = false;

  modalOptions: NgbModalOptions = { size: 'md', centered: true };

  // Kiểm tra quyền hạn theo Policy (hỗ trợ linh hoạt các naming conventions phổ biến)
  get canCreate(): boolean {
    return this.permissionService.getGrantedPolicy('TaskManagement.Tags.Create') || 
           this.permissionService.getGrantedPolicy('MyProject.Tag.Create');
  }

  get canEdit(): boolean {
    return this.permissionService.getGrantedPolicy('TaskManagement.Tags.Update') || 
           this.permissionService.getGrantedPolicy('MyProject.Tag.Update');
  }

  get canDelete(): boolean {
    return this.permissionService.getGrantedPolicy('TaskManagement.Tags.Delete') || 
           this.permissionService.getGrantedPolicy('MyProject.Tag.Delete');
  }

  ngOnInit() {
    this.buildSearchForm();
    this.loadCategoriesThenInitList();
  }

  loadCategoriesThenInitList() {
    this.loading = true;
    this.categoryService.getList({ skipCount: 0, maxResultCount: 1000 } as any).subscribe({
      next: (res) => {
        this.categories = res?.items || [];
        this.initTagListStream();
      },
      error: (err) => {
        console.error('Lỗi khi tải danh mục:', err);
        this.categories = [];
        this.initTagListStream();
      }
    });
  }

  private initTagListStream() {
    const streamCreator = (query: any) => {
      this.loading = true;
      const searchVal = this.searchForm?.value;
      return this.service.getList({
        ...query,
        filter: searchVal?.keyword || '',
        categoryId: searchVal?.categoryId || null
      } as any).pipe(
        finalize(() => (this.loading = false))
      );
    };

    this.list.hookToQuery(streamCreator).subscribe({
      next: (res) => {
        this.items = res || { items: [], totalCount: 0 };
      },
      error: (err) => {
        console.error('Lỗi tải danh sách thẻ:', err);
      }
    });
  }

  getCategoryName(row: TagDto | any): string {
    if (!row) return '';
    const directName = row.categoryName || row.CategoryName;
    if (directName) return directName;

    const catId = row.categoryId || row.CategoryId;
    if (!catId) return '';

    const matchCat = this.categories.find(
      (c) => String(c.id).toLowerCase() === String(catId).toLowerCase()
    );
    return matchCat?.name || '';
  }

  buildSearchForm() {
    this.searchForm = this.fb.group({
      keyword: [''],
      categoryId: [null],
    });
  }

  search() {
    this.list.page = 0;
    this.list.get();
  }

  reset() {
    this.searchForm.reset({ keyword: '', categoryId: null });
    this.list.page = 0;
    this.list.get();
  }

  create() {
    if (!this.canCreate) return;
    this.selected = {} as TagDto;
    this.buildForm();
    this.isModalOpen = true;
  }

  edit(id: string) {
    if (!this.canEdit) return;
    this.service.get(id).subscribe({
      next: (item) => {
        this.selected = item;
        this.buildForm();
        this.isModalOpen = true;
      },
      error: (err) => {
        this.noti.error(err?.error?.error?.message || 'Không thể tải thông tin thẻ', 'Lỗi');
      }
    });
  }

  buildForm() {
    const rawCatId = (this.selected as any).categoryId || (this.selected as any).CategoryId;
    this.form = this.fb.group({
      id: [this.selected.id || null],
      name: [this.selected.name || '', [Validators.required, Validators.maxLength(64)]],
      colorCode: [(this.selected as any).colorCode || '#0d6efd'],
      categoryId: [rawCatId ? String(rawCatId) : null],
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const formVal = this.form.value;
    const dto = {
      name: formVal.name?.trim(),
      colorCode: formVal.colorCode || '',
      categoryId: formVal.categoryId ? formVal.categoryId : null
    };

    const targetId = this.selected.id;
    const request = targetId
      ? this.service.update(targetId, dto as any)
      : this.service.create(dto as any);

    request
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: () => {
          this.isModalOpen = false;
          this.list.get();
          this.noti.success('Lưu thông tin thẻ thành công', 'Thông báo');
        },
        error: (err) => {
          this.noti.error(err?.error?.error?.message || 'Có lỗi xảy ra', 'Thất bại');
        }
      });
  }

  delete(id: string) {
    if (!this.canDelete) return;

    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa thẻ này?', 'Xác nhận xóa')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.loading = true;
          this.service.delete(id)
            .pipe(finalize(() => (this.loading = false)))
            .subscribe({
              next: () => {
                this.list.get();
                this.noti.success('Xóa thẻ thành công', 'Thông báo');
              },
              error: (err) => {
                const errorMsg = err?.error?.error?.message || 'Không thể xóa thẻ này do đang được sử dụng hoặc có ràng buộc dữ liệu!';
                this.noti.error(errorMsg, 'Thất bại');
              }
            });
        }
      });
  }
}