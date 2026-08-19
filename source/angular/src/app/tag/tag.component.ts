import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ListService, PagedResultDto } from '@abp/ng.core';
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

@Component({
  selector: 'app-tag',
  standalone: true,
  templateUrl: './tag.component.html',
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
  public readonly list = inject(ListService);

  items: PagedResultDto<any> = { items: [], totalCount: 0 };
  categories: any[] = [];
  selected = {} as any;
  form!: FormGroup;
  searchForm!: FormGroup;
  
  isModalOpen = false;
  loading = false;
  isSaving = false;

  modalOptions: NgbModalOptions = { size: 'md', centered: true };

  ngOnInit() {
    this.buildSearchForm();
    this.loadCategoriesAndTags();
  }

  loadCategoriesAndTags() {
    this.loading = true;
    this.categoryService.getList({ skipCount: 0, maxResultCount: 1000 } as any).subscribe({
      next: (res: any) => {
        this.categories = res?.items || res?.result?.items || res?.result || (Array.isArray(res) ? res : []);
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
      }).pipe(
        finalize(() => (this.loading = false))
      );
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res;
    });
  }

  // Hàm tra cứu tên danh mục an toàn 100%, ép kiểu string để khớp ID
  getCategoryName(row: any): string {
    // 1. Ưu tiên đọc trực tiếp từ Backend C# gửi sang (nếu có)
    if (row?.categoryName) return row.categoryName;
    if (row?.CategoryName) return row.CategoryName;

    // 2. Lấy ID từ dòng hiện tại
    const catId = row?.categoryId || row?.CategoryId;
    if (!catId) return '';

    // 3. Nếu danh mục đã load xong, tìm kiếm bằng cách ép sang chuỗilowercase để khớp hoàn toàn
    if (this.categories && this.categories.length > 0) {
      const matchCat = this.categories.find((c: any) => {
        const cId = c.id || c.Id;
        return cId && String(cId).toLowerCase() === String(catId).toLowerCase();
      });
      if (matchCat) {
        return matchCat.name || matchCat.Name;
      }
    }

    return '';
  }

  buildSearchForm() {
    this.searchForm = this.fb.group({
      keyword: [''],
      categoryId: [''],
    });
  }

  search() {
    this.list.page = 0;
    this.list.get();
  }

  reset() {
    this.searchForm.reset({ keyword: '', categoryId: '' });
    this.list.page = 0;
    this.list.get();
  }

  create() {
    this.selected = {} as TagDto;
    this.buildForm();
    this.isModalOpen = true;
  }

  edit(id: string) {
    this.service.get(id).subscribe(item => {
      this.selected = item;
      this.buildForm();
      this.form.patchValue(item);
      this.isModalOpen = true;
    });
  }

  buildForm() {
    this.form = this.fb.group({
      id: [this.selected.id || null],
      name: [this.selected.name || '', [Validators.required, Validators.maxLength(100)]],
      categoryId: [this.selected.categoryId || this.selected.CategoryId || null],
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
      ...formVal,
      name: formVal.name?.trim(),
      categoryId: formVal.categoryId ? formVal.categoryId : null
    };

    const request = this.selected.id
      ? this.service.update(this.selected.id, dto)
      : this.service.create(dto);

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
    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa thẻ này?', 'Xác nhận xóa')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.service.delete(id).subscribe(() => {
            this.list.get();
            this.noti.success('Xóa thẻ thành công', 'Thông báo');
          });
        }
      });
  }
}