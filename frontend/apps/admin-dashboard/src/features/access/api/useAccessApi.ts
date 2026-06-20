import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/shared/api';
import { useNotification } from '@/shared/hooks';

type UpdateUserPayload = {
  userId: string;
  email?: string;
  displayName?: string;
};

export function useAccessQueries(activeStoreId: string, selectedUserId: string | null) {
  const rolesQuery = useQuery({
    queryKey: ['stores', activeStoreId, 'roles'],
    queryFn: () => apiClient.listStoreRoles(activeStoreId),
    enabled: Boolean(activeStoreId),
  });

  const permissionsQuery = useQuery({
    queryKey: ['stores', activeStoreId, 'permissions'],
    queryFn: () => apiClient.listStorePermissions(activeStoreId),
    enabled: Boolean(activeStoreId),
  });

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: () => apiClient.listUsers(),
  });

  const userRolesQuery = useQuery({
    queryKey: ['stores', activeStoreId, 'users', selectedUserId, 'roles'],
    queryFn: () => apiClient.getStoreUserRoles(activeStoreId, selectedUserId as string),
    enabled: Boolean(activeStoreId && selectedUserId),
  });

  return {
    rolesQuery,
    permissionsQuery,
    usersQuery,
    userRolesQuery,
  };
}

export function useAccessMutations(activeStoreId: string, selectedUserId: string | null) {
  const queryClient = useQueryClient();
  const notification = useNotification();

  const createRoleMutation = useMutation({
    mutationFn: (payload: { name: string; description?: string }) => apiClient.createStoreRole(activeStoreId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'roles'] });
      notification.success('Role created.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create role.');
    },
  });

  const createPermissionMutation = useMutation({
    mutationFn: (payload: { relation: string; object: string }) => apiClient.createStorePermission(activeStoreId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'permissions'] });
      notification.success('Permission created.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create permission.');
    },
  });

  const assignPermissionMutation = useMutation({
    mutationFn: (payload: { roleName: string; relation: string; object: string }) => apiClient.assignStorePermissionToRole(activeStoreId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'permissions'] });
      queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'roles'] });
      notification.success('Permission assigned to role.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to assign permission to role.');
    },
  });

  const assignUserRoleMutation = useMutation({
    mutationFn: (payload: { userId: string; roleName: string }) =>
      apiClient.assignStoreRoleToUser(activeStoreId, payload.userId, { roleName: payload.roleName }),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      if (selectedUserId && selectedUserId === payload.userId) {
        queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'users', selectedUserId, 'roles'] });
      }
      notification.success('Role assigned to user.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to assign role to user.');
    },
  });

  const createUserMutation = useMutation({
    mutationFn: (payload: { userId: string; email?: string; displayName?: string }) => apiClient.createUser(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      notification.success('User created.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create user.');
    },
  });

  const updateUserMutation = useMutation({
    mutationFn: (payload: UpdateUserPayload) =>
      apiClient.updateUser(payload.userId, {
        email: payload.email,
        displayName: payload.displayName,
      }),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      if (selectedUserId && selectedUserId === payload.userId) {
        queryClient.invalidateQueries({ queryKey: ['stores', activeStoreId, 'users', selectedUserId, 'roles'] });
      }
      notification.success('User updated.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to update user.');
    },
  });

  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => apiClient.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      notification.success('User deleted.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to delete user.');
    },
  });

  return {
    createRoleMutation,
    createPermissionMutation,
    assignPermissionMutation,
    assignUserRoleMutation,
    createUserMutation,
    updateUserMutation,
    deleteUserMutation,
  };
}
