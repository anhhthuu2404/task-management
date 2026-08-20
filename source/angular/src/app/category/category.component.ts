import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { ListService, PagedResultDto } from '@abp/ng.core';
import { ToasterService, ConfirmationService, Confirmation, ThemeSharedModule } from '@abp/ng.theme.shared';
import { PageModule } from '@abp/ng.components/page';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { CategoryService, CategoryDto, CreateUpdateCategoryDto } from '../proxy/categories';

export interface CategoryViewModel extends CategoryDto {
  isDefault?: boolean;
}

@Component({
  selector: 'app-category',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    ReactiveFormsModule,
    PageModule,
    ThemeSharedModule,  
    NgxDatatableModule  
  ],
  providers: [ListService],
  templateUrl: './category.component.html'
})
export class CategoryComponent implements OnInit {
  public readonly list = inject(ListService);
  private readonly categoryService = inject(CategoryService);
  private readonly noti = inject(ToasterService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);

  items: PagedResultDto<CategoryViewModel> = { items: [], totalCount: 0 };
  selectedCategory: CategoryViewModel | null = null;
  searchForm!: FormGroup;

  isModalOpen = false;
  isEditMode = false;

  modalOptions = {
    suppressUnsavedChangesWarning: true
  };

  formData: CreateUpdateCategoryDto = {
    name: '',
    description: '',
    isActive: true
  };

  ngOnInit(): void {
    this.buildSearchForm();
    this.loadCategories();
  }

  private buildSearchForm(): void {
    this.searchForm = this.fb.group({
      filter: ['']
    });
  }

  loadCategories(): void {
    const categoryStream = (query: any) =>
      this.categoryService.getList({
        ...query,
        filter: this.searchForm?.value?.filter || ''
      });

    this.list.hookToQuery(categoryStream).subscribe({
      next: (res) => {
        this.items = {
          totalCount: res.totalCount,
          items: (res.items || []).map((item) => ({
            ...item,
            isDefault: (item as CategoryViewModel).isDefault ?? false
          }))
        };
      },
      error: (err) => {
        this.noti.error(err?.error?.error?.message || 'Không thể tải danh sách danh mục');
      }
    });
  }

  create(): void {
    this.openCreateModal();
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedCategory = null;
    this.formData = {
      name: '',
      description: '',
      isActive: true
    };
    this.isModalOpen = true;
  }

  openEditModal(category: CategoryViewModel): void {
    this.isEditMode = true;
    this.selectedCategory = category;
    this.formData = {
      name: category.name ?? '',
      description: category.description ?? '',
      isActive: category.isActive ?? true
    };
    this.isModalOpen = true;
  }

  search(): void {
    this.list.get();
  }

  reset(): void {
    this.searchForm.reset({ filter: '' });
    this.list.get();
  }

  saveCategory(): void {
    if (!this.formData.name) {
      this.noti.warn('Vui lòng nhập Tên danh mục');
      return;
    }

    const request = this.isEditMode && this.selectedCategory?.id
      ? this.categoryService.update(this.selectedCategory.id, this.formData, { skipHandleError: true })
      : this.categoryService.create(this.formData, { skipHandleError: true });

    request.subscribe({
      next: () => {
        this.noti.success(this.isEditMode ? 'Cập nhật danh mục thành công' : 'Tạo mới danh mục thành công');
        this.closeModal();
        this.list.get();
      },
      error: (err) => {
        this.noti.error(err?.error?.error?.message || 'Có lỗi xảy ra khi lưu danh mục!');
      }
    });
  }

  deleteCategory(category: CategoryViewModel): void {
    if (category.isDefault) {
      this.noti.warn('Không thể xóa danh mục mặc định của hệ thống!');
      return;
    }

    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa danh mục này?', 'Xác nhận xóa')
      .subscribe((status) => {
        if (status === Confirmation.Status.confirm && category.id) {
          this.categoryService.delete(category.id, { skipHandleError: true }).subscribe({
            next: () => {
              this.noti.success('Xóa danh mục thành công');
              this.list.get();
            },
            error: (err) => {
              this.noti.error(err?.error?.error?.message || 'Không thể xóa danh mục này');
            }
          });
        }
      });
  }

  closeModal(): void {
    this.isModalOpen = false;
  }
}