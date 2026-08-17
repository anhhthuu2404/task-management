import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ListService, PagedResultDto } from '@abp/ng.core';
import {
  NgxDatatableDefaultDirective,
  NgxDatatableListDirective,
  ModalComponent,
  ModalCloseDirective,
  ConfirmationService,
  Confirmation,
  ToasterService,
} from '@abp/ng.theme.shared';
import { PageModule } from '@abp/ng.components/page';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { NgbDropdownModule, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';

import { TagService } from '../proxy/tags/tag.service';
import { TagDto } from '../proxy/tags/models';

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
    NgxDatatableDefaultDirective,
  ],
  providers: [ListService],
})
export class TagComponent implements OnInit {
  private service = inject(TagService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  public readonly list = inject(ListService);
  private noti = inject(ToasterService);

  items: PagedResultDto<TagDto> = { items: [], totalCount: 0 };
  selected = {} as TagDto;
  form!: FormGroup;
  searchForm!: FormGroup;
  isModalOpen = false;

  modalOptions: NgbModalOptions = { size: 'lg' };

  ngOnInit() {
    this.buildSearchForm();
    this.loadData();
  }

  loadData() {
    const searchValue = this.searchForm?.value;
    this.service
      .getList({
        filter: searchValue?.keyword || '',
        maxResultCount: this.list.maxResultCount,
        skipCount: this.list.page * this.list.maxResultCount,
      } as any)
      .subscribe(res => {
        this.items = res;
      });
  }

  buildSearchForm() {
    this.searchForm = this.fb.group({
      keyword: [''],
    });
  }

  search() {
    this.list.page = 0;
    this.loadData();
  }

  reset() {
    this.searchForm.reset();
    this.list.page = 0;
    this.loadData();
  }

  create() {
    this.selected = {} as TagDto;
    this.buildForm();
    this.isModalOpen = true;
  }

  edit(id: any) {
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
      name: [(this.selected as any).name || '', [Validators.required, Validators.maxLength(100)]],
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const dto = this.form.value;
    const request = this.selected.id
      ? this.service.update(this.selected.id, dto)
      : this.service.create(dto);

    request.subscribe(() => {
      this.isModalOpen = false;
      this.loadData();
      this.noti.success('Lưu thẻ thành công', 'Thông báo');
    });
  }

  delete(id: any) {
    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa thẻ này?', 'Xác nhận xóa')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.service.delete(id).subscribe(() => {
            this.loadData();
            this.noti.success('Xóa thẻ thành công', 'Thông báo');
          });
        }
      });
  }
}