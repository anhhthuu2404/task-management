import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTagDto {
  name: string;
  colorCode?: string;
  categoryId?: string;
}

export interface GetTagListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  categoryId?: string;
}

export interface TagDto extends FullAuditedEntityDto<string> {
  name?: string;
  colorCode?: string;
  categoryId?: string;
  categoryName?: string;
}
