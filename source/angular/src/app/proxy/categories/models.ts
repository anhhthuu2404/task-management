import type { EntityDto } from '@abp/ng.core';

export interface CategoryDto extends EntityDto<string> {
  name?: string;
  description?: string;
  colorCode?: string;
}

export interface CreateUpdateCategoryDto {
  name: string;
  description?: string;
  isActive?: boolean;
  colorCode?: string;
}
