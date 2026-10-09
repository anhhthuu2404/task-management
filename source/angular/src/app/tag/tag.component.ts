import { Component, OnInit, inject, ViewEncapsulation, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { CoreModule, ListService, PagedResultDto, PermissionService, LocalizationService } from '@abp/ng.core';
import {
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
  `],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CoreModule,
    NgxDatatableModule,
    NgbDropdownModule,
    PageModule,
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
  private readonly localizationService = inject(LocalizationService);
  private readonly cdr = inject(ChangeDetectorRef);
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

  get canCreate(): boolean {
    return true; 
  }

  get canEdit(): boolean {
    return true; 
  }

  get canDelete(): boolean {
    return true; 
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
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      );
    };

    this.list.hookToQuery(streamCreator).subscribe({
      next: (res) => {
        this.items = res || { items: [], totalCount: 0 };
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Lỗi tải danh sách thẻ:', err);
        this.cdr.detectChanges();
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
    this.loading = true;
    this.service.get(id).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (item) => {
        this.selected = item;
        this.buildForm();
        this.isModalOpen = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        const isEn = this.localizationService.currentLang?.startsWith('en');
        this.noti.error(
          err?.error?.error?.message || (isEn ? 'Could not load tag information' : 'Không thể tải thông tin thẻ'), 
          isEn ? 'Error' : 'Lỗi'
        );
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

  getTagsByCategory(catId: string | null) {
    if (!this.items || !this.items.items) return [];
    
    return this.items.items.filter(x => {
      const rowCatId = (x as any).categoryId || (x as any).CategoryId;
      if (catId === null) {
        return rowCatId === null || rowCatId === undefined || rowCatId === '';
      }
      return String(rowCatId).toLowerCase() === String(catId).toLowerCase();
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

    const isEn = this.localizationService.currentLang?.startsWith('en');

    request
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.isModalOpen = false;
          this.list.get();
          this.noti.success(
            isEn ? 'Tag saved successfully' : 'Lưu thông tin thẻ thành công', 
            isEn ? 'Notification' : 'Thông báo'
          );
        },
        error: (err) => {
          this.noti.error(
            err?.error?.error?.message || (isEn ? 'An error occurred' : 'Có lỗi xảy ra'), 
            isEn ? 'Failed' : 'Thất bại'
          );
        }
      });
  }

  delete(id: string) {
    if (!this.canDelete) return;

    const isEn = this.localizationService.currentLang?.startsWith('en');

    this.confirmation
      .warn(
        isEn ? 'Are you sure you want to delete this tag?' : 'Bạn có chắc chắn muốn xóa thẻ này?', 
        isEn ? 'Delete confirmation' : 'Xác nhận xóa'
      )
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.loading = true;
          this.service.delete(id)
            .pipe(
              finalize(() => {
                this.loading = false;
                this.cdr.detectChanges();
              })
            )
            .subscribe({
              next: () => {
                this.list.get();
                this.noti.success(
                  isEn ? 'Tag deleted successfully' : 'Xóa thẻ thành công', 
                  isEn ? 'Notification' : 'Thông báo'
                );
              },
              error: (err) => {
                const defaultMsg = isEn 
                  ? 'Unable to delete this tag as it is currently in use or has data constraints!' 
                  : 'Không thể xóa thẻ này do đang được sử dụng hoặc có ràng buộc dữ liệu!';
                const errorMsg = err?.error?.error?.message || defaultMsg;
                this.noti.error(errorMsg, isEn ? 'Failed' : 'Thất bại');
              }
            });
        }
      });
  }
}