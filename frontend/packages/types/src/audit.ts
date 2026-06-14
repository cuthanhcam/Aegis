export type AuditEvent = {
  action: string;
  subject: string;
  relation: string;
  object: string;
  decision: string;
  reasonCode: string;
  createdAt: string;
};
