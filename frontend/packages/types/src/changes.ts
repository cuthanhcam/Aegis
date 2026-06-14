export type RelationshipChange = {
  subject: string;
  relation: string;
  object: string;
  operation: string;
  createdAt: string;
};

export type ReadChangesResponse = {
  changes: RelationshipChange[];
  continuation_token?: string;
};
