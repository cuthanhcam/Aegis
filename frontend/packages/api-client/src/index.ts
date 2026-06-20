import type { ApiResponse } from '@aegis/types/src/api';
import type {
  AssignPermissionToRoleRequest,
  AssignRoleToUserRequest,
  CreatePermissionRequest,
  CreateRoleRequest,
  CreateUserRequest,
  Permission,
  Role,
  UpdateUserRequest,
  User,
  UserRoles,
} from '@aegis/types/src/access';
import type { AssertionRunListResponse, AssertionRunRecord, ReadAssertionsResponse, WriteAssertionsRequest } from '@aegis/types/src/assertion';
import type { LoginResponse } from '@aegis/types/src/auth';
import type { UserProfile } from '@aegis/types/src/auth';
import type { AuditEvent } from '@aegis/types/src/audit';
import type {
  BatchCheckItemRequest,
  BatchCheckResponse,
  CheckResult,
  OpenFgaBatchCheckRequest,
  OpenFgaBatchCheckResponse,
  OpenFgaCheckRequest,
  OpenFgaCheckResponse,
  StoreCheckRequest,
} from '@aegis/types/src/check';
import type { ReadChangesResponse } from '@aegis/types/src/changes';
import type {
  ExpandNode,
  ExpandRequest,
  ListObjectsRequest,
  ListObjectsResponse,
  ListUsersRequest,
  ListUsersResponse,
} from '@aegis/types/src/graph';
import type {
  AuthorizationModel,
  AuthorizationModelDiff,
  AuthorizationModelValidationResult,
  CreateAuthorizationModelRequest,
  PublishAuthorizationModelResponse,
  RollbackAuthorizationModelResponse,
  UpdateAuthorizationModelRequest,
  ValidateAuthorizationModelRequest,
} from '@aegis/types/src/model';
import type { DeletePresetRequest, PresetItem, PresetMeta, PresetSource, UpsertPresetRequest } from '@aegis/types/src/preset';
import type {
  RelationshipDeleteRequest,
  RelationshipQuery,
  RelationshipTuple,
  RelationshipWriteRequest,
} from '@aegis/types/src/relationship';
import type { Store } from '@aegis/types/src/store';

export type ApiClientConfig = {
  baseUrl: string;
  getAccessToken?: () => string | null;
  refreshAccessToken?: () => Promise<boolean>;
  includeCredentials?: boolean;
};

export class AegisApiClient {
  constructor(private readonly config: ApiClientConfig) {}

  private decodeJwtPayload(token: string): Record<string, unknown> {
    const parts = token.split('.');
    if (parts.length < 2) {
      throw new Error('Invalid access token format.');
    }

    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padding = base64.length % 4 === 0 ? '' : '='.repeat(4 - (base64.length % 4));
    const normalized = `${base64}${padding}`;

    try {
      if (typeof atob === 'function') {
        return JSON.parse(atob(normalized)) as Record<string, unknown>;
      }

      throw new Error('Token decoding is not available in this runtime.');
    } catch {
      throw new Error('Failed to parse access token payload.');
    }
  }

  private resolveTenantId(): string {
    const token = this.config.getAccessToken?.();
    if (!token) {
      throw new Error('Missing access token for tenant-scoped request.');
    }

    const payload = this.decodeJwtPayload(token);
    const tenantId = payload.tenant_id ?? payload.tid;
    if (typeof tenantId !== 'string' || !tenantId.trim()) {
      throw new Error('Tenant claim is missing in access token.');
    }

    return tenantId;
  }

  private encodeStoreId(storeId: string): string {
    return encodeURIComponent(storeId);
  }

  private async toError(response: Response): Promise<Error> {
    const text = await response.text();
    if (!text) {
      return new Error(`Request failed (${response.status})`);
    }

    try {
      const payload = JSON.parse(text) as
        | { success?: boolean; error?: { code?: string; message?: string } }
        | { code?: string; message?: string };

      if ('success' in payload && payload.error?.message) {
        return new Error(payload.error.message);
      }

      if ('code' in payload && payload.message) {
        return new Error(`${payload.code}: ${payload.message}`);
      }
    } catch {
      // Fall back to raw text if response is not JSON.
    }

    return new Error(text);
  }

  private toQueryString(query?: Record<string, string | undefined>) {
    if (!query) {
      return '';
    }

    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
      if (value) {
        params.set(key, value);
      }
    }

    const qs = params.toString();
    return qs ? `?${qs}` : '';
  }

  private buildHeaders(init?: RequestInit) {
    const token = this.config.getAccessToken?.();
    const headers = new Headers(init?.headers);
    headers.set('Content-Type', 'application/json');
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }

  private async execute(path: string, init?: RequestInit): Promise<Response> {
    const response = await fetch(`${this.config.baseUrl}${path}`, {
      ...init,
      headers: this.buildHeaders(init),
      credentials: this.config.includeCredentials ? 'include' : init?.credentials,
    });

    const canTryRefresh =
      response.status === 401
      && !path.startsWith('/auth/')
      && Boolean(this.config.refreshAccessToken);

    if (!canTryRefresh) {
      return response;
    }

    const refreshed = await this.config.refreshAccessToken!();
    if (!refreshed) {
      return response;
    }

    return fetch(`${this.config.baseUrl}${path}`, {
      ...init,
      headers: this.buildHeaders(init),
      credentials: this.config.includeCredentials ? 'include' : init?.credentials,
    });
  }

  private async request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await this.execute(path, init);

    if (!response.ok) {
      throw await this.toError(response);
    }

    const payload = (await response.json()) as ApiResponse<T>;
    if (!payload.success) {
      throw new Error(payload.error?.message ?? 'Unknown API error');
    }

    return payload.data as T;
  }

  private async requestRaw<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await this.execute(path, init);

    if (!response.ok) {
      throw await this.toError(response);
    }

    return (await response.json()) as T;
  }

  private async requestText(path: string, init?: RequestInit): Promise<string> {
    const response = await this.execute(path, init);

    if (!response.ok) {
      throw await this.toError(response);
    }

    return response.text();
  }

  async login(username: string, password: string): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
  }

  async refresh(refreshToken?: string): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async logout(refreshToken?: string): Promise<string> {
    return this.request<string>('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async logoutAll(): Promise<string> {
    return this.request<string>('/auth/logout-all', {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async getProfile(): Promise<UserProfile> {
    return this.request<UserProfile>('/auth/me');
  }

  async getAuthorizationMetrics(): Promise<string> {
    return this.requestText('/metrics/authorization', {
      method: 'GET',
      headers: {
        Accept: 'text/plain',
      },
    });
  }

  async listStores(): Promise<Store[]> {
    return this.request<Store[]>('/stores');
  }

  async createStore(name: string): Promise<Store> {
    return this.request<Store>('/stores', {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
  }

  async deleteStore(storeId: string): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}`, { method: 'DELETE' });
  }

  async listAuthorizationModels(storeId: string): Promise<AuthorizationModel[]> {
    return this.request<AuthorizationModel[]>(`/stores/${this.encodeStoreId(storeId)}/authorization-models`);
  }

  async createAuthorizationModel(
    storeId: string,
    request: CreateAuthorizationModelRequest,
  ): Promise<AuthorizationModel> {
    return this.request<AuthorizationModel>(`/stores/${this.encodeStoreId(storeId)}/authorization-models`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async validateAuthorizationModel(
    storeId: string,
    request: ValidateAuthorizationModelRequest,
  ): Promise<AuthorizationModelValidationResult> {
    return this.request<AuthorizationModelValidationResult>(`/stores/${this.encodeStoreId(storeId)}/authorization-models/validate`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateAuthorizationModel(
    storeId: string,
    authorizationModelId: string,
    request: UpdateAuthorizationModelRequest,
  ): Promise<AuthorizationModel> {
    return this.request<AuthorizationModel>(`/stores/${this.encodeStoreId(storeId)}/authorization-models/${encodeURIComponent(authorizationModelId)}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async deleteAuthorizationModel(storeId: string, authorizationModelId: string): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/authorization-models/${encodeURIComponent(authorizationModelId)}`, {
      method: 'DELETE',
    });
  }

  async publishAuthorizationModel(storeId: string, authorizationModelId: string): Promise<PublishAuthorizationModelResponse> {
    return this.request<PublishAuthorizationModelResponse>(
      `/stores/${this.encodeStoreId(storeId)}/authorization-models/${encodeURIComponent(authorizationModelId)}/publish`,
      { method: 'POST' },
    );
  }

  async rollbackAuthorizationModel(storeId: string, authorizationModelId: string): Promise<RollbackAuthorizationModelResponse> {
    return this.request<RollbackAuthorizationModelResponse>(
      `/stores/${this.encodeStoreId(storeId)}/authorization-models/${encodeURIComponent(authorizationModelId)}/rollback`,
      { method: 'POST' },
    );
  }

  async diffAuthorizationModels(storeId: string, leftModelId: string, rightModelId: string): Promise<AuthorizationModelDiff> {
    return this.request<AuthorizationModelDiff>(
      `/stores/${this.encodeStoreId(storeId)}/authorization-models/${encodeURIComponent(leftModelId)}/diff/${encodeURIComponent(rightModelId)}`,
    );
  }

  async listRelationships(storeId: string, query?: RelationshipQuery): Promise<RelationshipTuple[]> {
    const qs = this.toQueryString({
      subject: query?.subject,
      relation: query?.relation,
      object: query?.obj,
      effect: query?.effect,
    });

    return this.request<RelationshipTuple[]>(`/stores/${this.encodeStoreId(storeId)}/relationships${qs}`);
  }

  async upsertRelationship(storeId: string, request: RelationshipWriteRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/relationships`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async deleteRelationship(storeId: string, request: RelationshipDeleteRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/relationships`, {
      method: 'DELETE',
      body: JSON.stringify(request),
    });
  }

  async checkInStore(storeId: string, request: StoreCheckRequest): Promise<CheckResult> {
    return this.request<CheckResult>(`/stores/${this.encodeStoreId(storeId)}/check`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async explainInStore(storeId: string, request: StoreCheckRequest): Promise<CheckResult> {
    return this.request<CheckResult>(`/stores/${this.encodeStoreId(storeId)}/explain`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async listUsersInStore(storeId: string, request: ListUsersRequest): Promise<ListUsersResponse> {
    return this.request<ListUsersResponse>(`/stores/${this.encodeStoreId(storeId)}/graph/list-users`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async listObjectsInStore(storeId: string, request: ListObjectsRequest): Promise<ListObjectsResponse> {
    return this.request<ListObjectsResponse>(`/stores/${this.encodeStoreId(storeId)}/graph/list-objects`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async expandInStore(storeId: string, request: ExpandRequest): Promise<ExpandNode> {
    return this.request<ExpandNode>(`/stores/${this.encodeStoreId(storeId)}/graph/expand`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async batchCheckInStore(
    storeId: string,
    items: BatchCheckItemRequest[],
  ): Promise<BatchCheckResponse> {
    return this.request<BatchCheckResponse>(`/stores/${this.encodeStoreId(storeId)}/batch-check`, {
      method: 'POST',
      body: JSON.stringify({
        items,
      }),
    });
  }

  async checkCompatInStore(storeId: string, request: OpenFgaCheckRequest): Promise<OpenFgaCheckResponse> {
    return this.requestRaw<OpenFgaCheckResponse>(`/stores/${this.encodeStoreId(storeId)}/check/compat`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async batchCheckCompatInStore(
    storeId: string,
    request: OpenFgaBatchCheckRequest,
  ): Promise<OpenFgaBatchCheckResponse> {
    return this.requestRaw<OpenFgaBatchCheckResponse>(`/stores/${this.encodeStoreId(storeId)}/batch-check/compat`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async readChanges(storeId: string, params?: { pageSize?: number; continuationToken?: string; type?: string }) {
    const qs = this.toQueryString({
      page_size: params?.pageSize ? String(params.pageSize) : undefined,
      continuation_token: params?.continuationToken,
      type: params?.type,
    });

    return this.request<ReadChangesResponse>(`/stores/${this.encodeStoreId(storeId)}/relationships/changes${qs}`);
  }

  async readAuditEvents(params?: { action?: string; decision?: string }): Promise<AuditEvent[]> {
    const tenantId = this.resolveTenantId();
    const qs = this.toQueryString({
      action: params?.action,
      decision: params?.decision,
    });

    return this.request<AuditEvent[]>(`/tenants/${encodeURIComponent(tenantId)}/audit${qs}`);
  }

  async readAssertions(storeId: string, authorizationModelId: string) {
    return this.request<ReadAssertionsResponse>(
      `/stores/${this.encodeStoreId(storeId)}/assertions/${encodeURIComponent(authorizationModelId)}`,
    );
  }

  async writeAssertions(
    storeId: string,
    authorizationModelId: string,
    request: WriteAssertionsRequest,
  ): Promise<void> {
    await this.request<string>(`/stores/${this.encodeStoreId(storeId)}/assertions/${encodeURIComponent(authorizationModelId)}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async runAssertions(storeId: string, authorizationModelId: string): Promise<AssertionRunRecord> {
    return this.request<AssertionRunRecord>(
      `/stores/${this.encodeStoreId(storeId)}/assertions/${encodeURIComponent(authorizationModelId)}/run`,
      { method: 'POST' },
    );
  }

  async listAssertionRuns(storeId: string, authorizationModelId: string): Promise<AssertionRunListResponse> {
    return this.request<AssertionRunListResponse>(
      `/stores/${this.encodeStoreId(storeId)}/assertions/${encodeURIComponent(authorizationModelId)}/runs`,
    );
  }

  async getAssertionRun(storeId: string, runId: string): Promise<AssertionRunRecord> {
    return this.request<AssertionRunRecord>(
      `/stores/${this.encodeStoreId(storeId)}/assertions/runs/${encodeURIComponent(runId)}`,
    );
  }

  async listRoles(): Promise<Role[]> {
    const tenantId = this.resolveTenantId();
    return this.request<Role[]>(`/tenants/${encodeURIComponent(tenantId)}/roles`);
  }

  async listStoreRoles(storeId: string): Promise<Role[]> {
    return this.request<Role[]>(`/stores/${this.encodeStoreId(storeId)}/roles`);
  }

  async createRole(request: CreateRoleRequest): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/roles`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async createStoreRole(storeId: string, request: CreateRoleRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/roles`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async assignPermissionToRole(request: AssignPermissionToRoleRequest): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/roles/assign-permission`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async assignStorePermissionToRole(storeId: string, request: AssignPermissionToRoleRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/roles/assign-permission`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async listPermissions(): Promise<Permission[]> {
    const tenantId = this.resolveTenantId();
    return this.request<Permission[]>(`/tenants/${encodeURIComponent(tenantId)}/permissions`);
  }

  async listStorePermissions(storeId: string): Promise<Permission[]> {
    return this.request<Permission[]>(`/stores/${this.encodeStoreId(storeId)}/permissions`);
  }

  async createPermission(request: CreatePermissionRequest): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/permissions`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async createStorePermission(storeId: string, request: CreatePermissionRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/permissions`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async assignRoleToUser(userId: string, request: AssignRoleToUserRequest): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}/roles`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async assignStoreRoleToUser(storeId: string, userId: string, request: AssignRoleToUserRequest): Promise<string> {
    return this.request<string>(`/stores/${this.encodeStoreId(storeId)}/users/${encodeURIComponent(userId)}/roles`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async listUsers(): Promise<User[]> {
    const tenantId = this.resolveTenantId();
    return this.request<User[]>(`/tenants/${encodeURIComponent(tenantId)}/users`);
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    const tenantId = this.resolveTenantId();
    return this.request<User>(`/tenants/${encodeURIComponent(tenantId)}/users`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateUser(userId: string, request: UpdateUserRequest): Promise<User> {
    const tenantId = this.resolveTenantId();
    return this.request<User>(`/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async deleteUser(userId: string): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`, {
      method: 'DELETE',
    });
  }

  async getUserRoles(userId: string): Promise<UserRoles> {
    const tenantId = this.resolveTenantId();
    return this.request<UserRoles>(`/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}/roles`);
  }

  async getStoreUserRoles(storeId: string, userId: string): Promise<UserRoles> {
    return this.request<UserRoles>(`/stores/${this.encodeStoreId(storeId)}/users/${encodeURIComponent(userId)}/roles`);
  }

  async listPresets(params?: { storeId?: string; source?: PresetSource; scope?: string }): Promise<PresetItem[]> {
    const tenantId = this.resolveTenantId();
    const qs = this.toQueryString({
      storeId: params?.storeId,
      source: params?.source,
      scope: params?.scope,
    });

    return this.request<PresetItem[]>(`/tenants/${encodeURIComponent(tenantId)}/presets${qs}`);
  }

  async upsertPreset(request: UpsertPresetRequest): Promise<PresetItem> {
    const tenantId = this.resolveTenantId();
    return this.request<PresetItem>(`/tenants/${encodeURIComponent(tenantId)}/presets`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async deletePreset(request: DeletePresetRequest): Promise<string> {
    const tenantId = this.resolveTenantId();
    return this.request<string>(`/tenants/${encodeURIComponent(tenantId)}/presets`, {
      method: 'DELETE',
      body: JSON.stringify(request),
    });
  }

  async getPresetMeta(): Promise<Record<string, PresetMeta>> {
    const tenantId = this.resolveTenantId();
    return this.request<Record<string, PresetMeta>>(`/tenants/${encodeURIComponent(tenantId)}/presets/meta`);
  }

  async setPresetMeta(meta: Record<string, PresetMeta>): Promise<Record<string, PresetMeta>> {
    const tenantId = this.resolveTenantId();
    return this.request<Record<string, PresetMeta>>(`/tenants/${encodeURIComponent(tenantId)}/presets/meta`, {
      method: 'PUT',
      body: JSON.stringify({ meta }),
    });
  }
}
