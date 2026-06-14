export type RelationshipTuple = {
  subject: string;
  relation: string;
  object: string;
  effect: string;
  createdAt: string;
};

export type RelationshipWriteRequest = {
  subject: string;
  relation: string;
  object: string;
  effect?: string;
};

export type RelationshipDeleteRequest = {
  subject: string;
  relation: string;
  object: string;
};

export type RelationshipQuery = {
  subject?: string;
  relation?: string;
  obj?: string;
  effect?: string;
};
