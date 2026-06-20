import { useMutation, useQuery } from '@tanstack/react-query';
import { apiClient } from '@/shared/api';

export type AssertionPreset = {
  name: string;
  payload: string;
  updatedAt: string;
};

export function useAssertionPresetsQuery(
  isAuthenticated: boolean,
  activeStoreId: string,
  authorizationModelId: string,
) {
  return useQuery({
    queryKey: ['assertion-presets', activeStoreId, authorizationModelId],
    queryFn: async () => {
      const presets = await apiClient.listPresets({
        storeId: activeStoreId,
        source: 'assertions',
        scope: authorizationModelId,
      });

      return presets.map<AssertionPreset>((item) => ({
        name: item.name,
        payload: item.payload,
        updatedAt: item.updatedAt,
      }));
    },
    enabled: isAuthenticated && Boolean(activeStoreId) && Boolean(authorizationModelId),
  });
}

export function useAssertionModelsQuery(isAuthenticated: boolean, activeStoreId: string) {
  return useQuery({
    queryKey: ['models-for-assertions', activeStoreId],
    queryFn: () => apiClient.listAuthorizationModels(activeStoreId),
    enabled: isAuthenticated && Boolean(activeStoreId),
  });
}

export function useAssertionsQuery(isAuthenticated: boolean, activeStoreId: string, authorizationModelId: string) {
  return useQuery({
    queryKey: ['assertions', activeStoreId, authorizationModelId],
    queryFn: () => apiClient.readAssertions(activeStoreId, authorizationModelId),
    enabled: isAuthenticated && Boolean(activeStoreId) && Boolean(authorizationModelId),
  });
}

export function useAssertionRunsQuery(isAuthenticated: boolean, activeStoreId: string, authorizationModelId: string) {
  return useQuery({
    queryKey: ['assertion-runs', activeStoreId, authorizationModelId],
    queryFn: () => apiClient.listAssertionRuns(activeStoreId, authorizationModelId),
    enabled: isAuthenticated && Boolean(activeStoreId) && Boolean(authorizationModelId),
  });
}

export function useRunAssertionsMutation(onSuccess: () => void) {
  return useMutation({
    mutationFn: (payload: { activeStoreId: string; authorizationModelId: string }) =>
      apiClient.runAssertions(payload.activeStoreId, payload.authorizationModelId),
    onSuccess,
  });
}

export function useWriteAssertionsMutation(onMutate?: () => void) {
  return useMutation({
    mutationFn: (payload: {
      activeStoreId: string;
      authorizationModelId: string;
      assertions: unknown;
    }) =>
      apiClient.writeAssertions(payload.activeStoreId, payload.authorizationModelId, {
        assertions: payload.assertions as never,
      }),
    onMutate,
  });
}

export function useAssertionPresetSaveMutation(onSuccess: () => void) {
  return useMutation({
    mutationFn: async (payload: {
      activeStoreId: string;
      authorizationModelId: string;
      name: string;
      assertionsJson: string;
    }) => {
      await apiClient.upsertPreset({
        source: 'assertions',
        storeId: payload.activeStoreId,
        scope: payload.authorizationModelId,
        name: payload.name,
        payload: payload.assertionsJson,
      });
    },
    onSuccess,
  });
}

export function useAssertionPresetDeleteMutation(onSuccess: () => void) {
  return useMutation({
    mutationFn: async (payload: {
      activeStoreId: string;
      authorizationModelId: string;
      name: string;
    }) => {
      await apiClient.deletePreset({
        source: 'assertions',
        storeId: payload.activeStoreId,
        scope: payload.authorizationModelId,
        name: payload.name,
      });
    },
    onSuccess,
  });
}
