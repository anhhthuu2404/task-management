import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface CategoryDto extends FullAuditedEntityDto<string> {
  name?: string;
  description?: string;
  isActive: boolean;
  colorCode?: string;
}

export interface CreateUpdateCategoryDto {
  name: string;
  description?: string;
  isActive: boolean;
  colorCode?: string;
}
