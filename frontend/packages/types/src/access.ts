export type Role = {
  name: string;
  description?: string | null;
};

export type Permission = {
  relation: string;
  object: string;
};

export type CreateRoleRequest = {
  name: string;
  description?: string;
};

export type CreatePermissionRequest = {
  relation: string;
  object: string;
};

export type AssignPermissionToRoleRequest = {
  roleName: string;
  relation: string;
  object: string;
};

export type AssignRoleToUserRequest = {
  roleName: string;
};

export type User = {
  userId: string;
  email?: string | null;
  displayName?: string | null;
  createdAt: string;
};

export type CreateUserRequest = {
  userId: string;
  email?: string;
  displayName?: string;
};

export type UpdateUserRequest = {
  email?: string;
  displayName?: string;
};

export type UserRoles = {
  userId: string;
  roles: string[];
};
