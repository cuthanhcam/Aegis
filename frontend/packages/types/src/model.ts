export type AuthorizationModel = {
  id: string;
  storeId: string;
  schemaVersion: string;
  model: string;
  createdAt: string;
  state: 'Draft' | 'Validated' | 'Published' | 'Archived' | 'Deprecated';
  publishedAt?: string | null;
  archivedAt?: string | null;
  supersededBy?: string | null;
};

export type PublishAuthorizationModelResponse = {
  publishedModel: AuthorizationModel;
  activeModelId: string;
  version: string;
};

export type RollbackAuthorizationModelResponse = {
  activeModel: AuthorizationModel;
  activeModelId: string;
  rolledBackFromModelId: string;
};

export type AuthorizationModelRelationDiff = {
  type: string;
  relation: string;
  expression: string;
};

export type AuthorizationModelRelationChange = {
  type: string;
  relation: string;
  leftExpression: string;
  rightExpression: string;
};

export type AuthorizationModelDiff = {
  leftModelId: string;
  rightModelId: string;
  addedTypes: string[];
  removedTypes: string[];
  changedTypes: string[];
  addedRelations: AuthorizationModelRelationDiff[];
  removedRelations: AuthorizationModelRelationDiff[];
  changedRelations: AuthorizationModelRelationChange[];
  breakingChangeHints: string[];
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
