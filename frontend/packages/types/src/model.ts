export type AuthorizationModel = {
  id: string;
  storeId: string;
  schemaVersion: string;
  model: string;
  createdAt: string;
};

export type CreateAuthorizationModelRequest = {
  schemaVersion: string;
  model: string;
};

export type UpdateAuthorizationModelRequest = {
  schemaVersion: string;
  model: string;
};

export type ValidateAuthorizationModelRequest = {
  schemaVersion: string;
  model: string;
};

export type AuthorizationModelValidationIssue = {
  code: string;
  message: string;
  line?: number | null;
};

export type AuthorizationModelValidationSummary = {
  typeCount: number;
  relationCount: number;
  directRelationCount: number;
  hasUnion: boolean;
  hasIntersection: boolean;
  hasExclusion: boolean;
  hasTupleToUserset: boolean;
};

export type AuthorizationModelValidationResult = {
  valid: boolean;
  summary: AuthorizationModelValidationSummary;
  errors: AuthorizationModelValidationIssue[];
  warnings: AuthorizationModelValidationIssue[];
};
