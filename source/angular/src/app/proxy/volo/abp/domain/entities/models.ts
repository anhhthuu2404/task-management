export interface Entity<TKey = string> {
  id?: TKey;
}

export interface BasicAggregateRoot<TKey> extends Entity<TKey> {
}

export interface AggregateRoot<TKey> extends BasicAggregateRoot<TKey> {
  extraProperties?: Record<string, object>;
  concurrencyStamp?: string;
}