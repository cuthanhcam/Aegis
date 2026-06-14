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
