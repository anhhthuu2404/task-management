import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface CreateUpdateTagDto {
  name: string;
  colorCode?: string;
}

export interface TagDto extends FullAuditedEntityDto<string> {
  name?: string;
  colorCode?: string;
  categoryName?: string;
  categoryId?: string;
}
