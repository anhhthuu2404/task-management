import { mapEnumToOptions } from '@abp/ng.core';

export enum RecurrenceFrequency {
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
}

export const recurrenceFrequencyOptions = mapEnumToOptions(RecurrenceFrequency);
