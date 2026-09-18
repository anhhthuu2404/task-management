import type { AggregateRoot, Entity } from '../models';

export interface AuditedAggregateRoot<TKey> extends CreationAuditedAggregateRoot<TKey> {
  lastModificationTime?: string;
  lastModifierId?: string;
}

export interface CreationAuditedAggregateRoot<TKey> extends AggregateRoot<TKey> {
  creationTime?: string;
  creatorId?: string;
}

export interface CreationAuditedEntity<TKey> extends Entity<TKey> {
  creationTime?: string;
  creatorId?: string;
}

export interface FullAuditedAggregateRoot<TKey> extends AuditedAggregateRoot<TKey> {
  isDeleted?: boolean;
  deleterId?: string;
  deletionTime?: string;
}
