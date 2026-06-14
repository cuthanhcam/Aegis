export type ListUsersRequest = {
  relation: string;
  object: string;
  consistency?: string;
  authorizationModelId?: string;
};

export type ListUsersResponse = {
  users: string[];
};

export type ListObjectsRequest = {
  user: string;
  relation: string;
  type: string;
  consistency?: string;
  authorizationModelId?: string;
};

export type ListObjectsResponse = {
  objects: string[];
};

export type ExpandRequest = {
  relation: string;
  object: string;
  consistency?: string;
  authorizationModelId?: string;
};

export type ExpandNode = {
  node: string;
  kind: string;
  users: string[];
  children: ExpandNode[];
};
