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

import { CategoryService } from '../proxy/categories/category.service';
import { CategoryDto } from '../proxy/categories/models';

@Component({
  selector: 'app-category',
  standalone: true,
  templateUrl: './category.component.html',
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
export class CategoryComponent implements OnInit {
  private readonly service = inject(CategoryService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmation = inject(ConfirmationService);
  private readonly noti = inject(ToasterService);
  public readonly list = inject(ListService);

  category: PagedResultDto<CategoryDto> = { items: [], totalCount: 0 };
  selectedCategory = {} as CategoryDto;
  form!: FormGroup;
  searchForm!: FormGroup;

  isModalOpen = false;
  loading = false;
  isSaving = false;

  modalOptions: NgbModalOptions = { size: 'md', centered: true };

  ngOnInit() {
    this.buildSearchForm();
    this.buildForm();
    this.initCategoryListStream();
  }

  private initCategoryListStream() {
    const streamCreator = (query: any) => {
      this.loading = true;
      const searchVal = this.searchForm?.value;
      return this.service.getList({
        ...query,
        filter: searchVal?.filter || '',
      } as any).pipe(
        finalize(() => (this.loading = false))
      );
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.category = res || { items: [], totalCount: 0 };
    });
  }

  buildSearchForm() {
    this.searchForm = this.fb.group({
      filter: [''],
    });
  }

  search() {
    this.list.page = 0;
    this.list.get();
  }

  reset() {
    this.searchForm.reset({ filter: '' });
    this.list.page = 0;
    this.list.get();
  }

  createCategory() {
    this.selectedCategory = {} as CategoryDto;
    this.buildForm();
    this.isModalOpen = true;
  }

  editCategory(id: string) {
    this.service.get(id).subscribe(item => {
      this.selectedCategory = item;
      this.buildForm();
      this.isModalOpen = true;
    });
  }

  buildForm() {
    this.form = this.fb.group({
      id: [this.selectedCategory?.id || null],
      name: [this.selectedCategory?.name || '', [Validators.required, Validators.maxLength(128)]],
      description: [(this.selectedCategory as any)?.description || ''],
      isActive: [(this.selectedCategory as any)?.isActive ?? true],
    });
  }

  saveCategory() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const formVal = this.form.value;
    const dto = {
      ...formVal,
      name: formVal.name?.trim(),
    };

    const targetId = this.selectedCategory?.id;
    const request = targetId
      ? this.service.update(targetId, dto as any)
      : this.service.create(dto as any);

    request
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: () => {
          this.isModalOpen = false;
          this.list.get();
          this.noti.success('Lưu thông tin danh mục thành công', 'Thông báo');
        },
        error: (err) => {
          this.noti.error(err?.error?.error?.message || 'Có lỗi xảy ra', 'Thất bại');
        }
      });
  }

  deleteCategory(id: string) {
    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa danh mục này?', 'Xác nhận xóa')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.service.delete(id).subscribe(() => {
            this.list.get();
            this.noti.success('Xóa danh mục thành công', 'Thông báo');
          });
        }
      });
  }
}