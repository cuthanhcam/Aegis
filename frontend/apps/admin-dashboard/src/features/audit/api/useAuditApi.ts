import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/shared/api';

type StoreChangesParams = {
  isAuthenticated: boolean;
  activeStoreId: string;
  typeFilter: string;
  continuationToken: string;
  pageSize: string;
};

export function useStoreChangesQuery(params: StoreChangesParams) {
  return useQuery({
    queryKey: ['changes', params.activeStoreId, params.typeFilter, params.continuationToken, params.pageSize],
    queryFn: () =>
      apiClient.readChanges(params.activeStoreId, {
        pageSize: Number(params.pageSize) || 50,
        continuationToken: params.continuationToken || undefined,
        type: params.typeFilter || undefined,
      }),
    enabled: params.isAuthenticated && Boolean(params.activeStoreId),
  });
}

export function useAuditEventsQuery(isAuthenticated: boolean, auditAction: string, auditDecision: string) {
  return useQuery({
    queryKey: ['audit-events', auditAction, auditDecision],
    queryFn: () =>
      apiClient.readAuditEvents({
        action: auditAction || undefined,
        decision: auditDecision || undefined,
      }),
    enabled: isAuthenticated,
  });
}
