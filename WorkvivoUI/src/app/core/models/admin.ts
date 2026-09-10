export interface Role {
  id: string;
  name: string;
  description: string | null;
  memberCount: number;
  permissionCount: number;

  /** Seeded roles the platform depends on - not safe to delete. */
  isSystem: boolean;
}

export interface AuditEntry {
  id: number;
  timestamp: string;
  username: string | null;
  action: string;
  entityName: string | null;
  entityId: string | null;
  affectedColumns: string | null;
  ipAddress: string | null;
  correlationId: string | null;
}
