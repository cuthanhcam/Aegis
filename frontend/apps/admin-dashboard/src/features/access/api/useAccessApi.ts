import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/shared/api';
import { useNotification } from '@/shared/hooks';

type UpdateUserPayload = {
  userId: string;
  email?: string;
  displayName?: string;
};

export function useAccessQueries(selectedUserId: string | null) {
  const rolesQuery = useQuery({
    queryKey: ['roles'],
    queryFn: () => apiClient.listRoles(),
  });

  const permissionsQuery = useQuery({
    queryKey: ['permissions'],
    queryFn: () => apiClient.listPermissions(),
  });

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: () => apiClient.listUsers(),
  });

  const userRolesQuery = useQuery({
    queryKey: ['users', selectedUserId, 'roles'],
    queryFn: () => apiClient.getUserRoles(selectedUserId as string),
    enabled: Boolean(selectedUserId),
  });

  return {
    rolesQuery,
    permissionsQuery,
    usersQuery,
    userRolesQuery,
  };
}

export function useAccessMutations(selectedUserId: string | null) {
  const queryClient = useQueryClient();
  const notification = useNotification();

  const createRoleMutation = useMutation({
    mutationFn: (payload: { name: string; description?: string }) => apiClient.createRole(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      notification.success('Role created.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create role.');
    },
  });

  const createPermissionMutation = useMutation({
    mutationFn: (payload: { relation: string; object: string }) => apiClient.createPermission(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['permissions'] });
      notification.success('Permission created.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create permission.');
    },
  });

  const assignPermissionMutation = useMutation({
    mutationFn: (payload: { roleName: string; relation: string; object: string }) => apiClient.assignPermissionToRole(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['permissions'] });
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      notification.success('Permission assigned to role.');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to assign permission to role.');
    },
  });

  const assignUserRoleMutation = useMutation({
    mutationFn: (payload: { userId: string; roleName: string }) =>
      apiClient.assignRoleToUser(payload.userId, { roleName: payload.roleName }),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      if (selectedUserId && selectedUserId === payload.userId) {
        queryClient.invalidateQueries({ queryKey: ['users', selectedUserId, 'roles'] });
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
        queryClient.invalidateQueries({ queryKey: ['users', selectedUserId, 'roles'] });
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
