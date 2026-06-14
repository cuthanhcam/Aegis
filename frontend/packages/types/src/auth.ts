export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken?: string | null;
  expiresIn: number;
};

export type UserProfile = {
  subject: string;
  username: string;
  tenantId: string;
  roles: string[];
  expiresAt?: string | null;
};
