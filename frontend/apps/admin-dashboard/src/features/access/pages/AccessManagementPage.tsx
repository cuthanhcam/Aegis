import { useMemo, useState } from 'react';
import { Alert, Button, Card, Input, Popconfirm, Select, Space, Table, Tabs, Typography } from 'antd';
import { TableSkeleton, TableEmptyState } from '@/shared/ui';
import { useAccessMutations, useAccessQueries } from '@/features/access/api/useAccessApi';

export function AccessManagementPage() {
  const [roleName, setRoleName] = useState('');
  const [roleDescription, setRoleDescription] = useState('');

  const [permissionRelation, setPermissionRelation] = useState('viewer');
  const [permissionObject, setPermissionObject] = useState('document');

  const [assignRoleName, setAssignRoleName] = useState('');
  const [assignRelation, setAssignRelation] = useState('viewer');
  const [assignObject, setAssignObject] = useState('document');

  const [userId, setUserId] = useState('user:anne');
  const [userRoleName, setUserRoleName] = useState('');

  const [newUserId, setNewUserId] = useState('');
  const [newUserEmail, setNewUserEmail] = useState('');
  const [newUserDisplayName, setNewUserDisplayName] = useState('');
  const [userSearch, setUserSearch] = useState('');
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  const { rolesQuery, permissionsQuery, usersQuery, userRolesQuery } = useAccessQueries(selectedUserId);
  const {
    createRoleMutation,
    createPermissionMutation,
    assignPermissionMutation,
    assignUserRoleMutation,
    createUserMutation,
    updateUserMutation,
    deleteUserMutation,
  } = useAccessMutations(selectedUserId);

  const roleOptions = useMemo(
    () => (rolesQuery.data ?? []).map((role) => ({ value: role.name, label: role.name })),
    [rolesQuery.data],
  );

  const filteredUsers = useMemo(() => {
    const keyword = userSearch.trim().toLowerCase();
    const users = usersQuery.data ?? [];
    if (!keyword) {
      return users;
    }

    return users.filter((user) =>
      user.userId.toLowerCase().includes(keyword)
      || (user.email ?? '').toLowerCase().includes(keyword)
      || (user.displayName ?? '').toLowerCase().includes(keyword),
    );
  }, [userSearch, usersQuery.data]);

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Tenant Access Management
          </Typography.Title>
          <Typography.Text type="secondary">
            Manage roles, permissions, and users for the current tenant.
          </Typography.Text>
        </div>

        <Tabs
          items={[
            {
              key: 'roles',
              label: 'Roles',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input placeholder="role name" value={roleName} onChange={(e) => setRoleName(e.target.value)} />
                    <Input
                      placeholder="description"
                      value={roleDescription}
                      onChange={(e) => setRoleDescription(e.target.value)}
                    />
                    <Button
                      type="primary"
                      disabled={!roleName.trim()}
                      loading={createRoleMutation.isPending}
                      onClick={() =>
                        createRoleMutation.mutate(
                          { name: roleName.trim(), description: roleDescription.trim() || undefined },
                          {
                            onSuccess: () => {
                              setRoleName('');
                              setRoleDescription('');
                            },
                          },
                        )}
                    >
                      Create Role
                    </Button>
                  </Space>

                  {rolesQuery.isLoading ? (
                    <TableSkeleton rows={4} columns={2} />
                  ) : (rolesQuery.data ?? []).length === 0 ? (
                    <TableEmptyState message="No roles created yet. Create your first role to get started." />
                  ) : (
                    <Table
                      rowKey="name"
                      dataSource={rolesQuery.data ?? []}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      scroll={{ x: 'max-content' }}
                      columns={[
                        { title: 'Role', dataIndex: 'name', key: 'name' },
                        { title: 'Description', dataIndex: 'description', key: 'description' },
                      ]}
                    />
                  )}
                </Space>
              ),
            },
            {
              key: 'permissions',
              label: 'Permissions',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input
                      placeholder="relation"
                      value={permissionRelation}
                      onChange={(e) => setPermissionRelation(e.target.value)}
                    />
                    <Input
                      placeholder="object type"
                      value={permissionObject}
                      onChange={(e) => setPermissionObject(e.target.value)}
                    />
                    <Button
                      type="primary"
                      disabled={!permissionRelation.trim() || !permissionObject.trim()}
                      loading={createPermissionMutation.isPending}
                      onClick={() =>
                        createPermissionMutation.mutate({
                          relation: permissionRelation.trim(),
                          object: permissionObject.trim(),
                        })}
                    >
                      Create Permission
                    </Button>
                  </Space>

                  {permissionsQuery.isLoading ? (
                    <TableSkeleton rows={4} columns={2} />
                  ) : (permissionsQuery.data ?? []).length === 0 ? (
                    <TableEmptyState message="No permissions created yet. Create your first permission to get started." />
                  ) : (
                    <Table
                      rowKey={(row) => `${row.relation}|${row.object}`}
                      dataSource={permissionsQuery.data ?? []}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      scroll={{ x: 'max-content' }}
                      columns={[
                        { title: 'Relation', dataIndex: 'relation', key: 'relation' },
                        { title: 'Object', dataIndex: 'object', key: 'object' },
                      ]}
                    />
                  )}
                </Space>
              ),
            },
            {
              key: 'assignments',
              label: 'Assignments',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Typography.Text strong>Assign Permission To Role</Typography.Text>
                  <Space wrap>
                    <Select
                      style={{ width: 220 }}
                      showSearch
                      placeholder="role name"
                      value={assignRoleName || undefined}
                      options={roleOptions}
                      onChange={(value) => setAssignRoleName(value)}
                    />
                    <Input
                      placeholder="relation"
                      value={assignRelation}
                      onChange={(e) => setAssignRelation(e.target.value)}
                    />
                    <Input
                      placeholder="object"
                      value={assignObject}
                      onChange={(e) => setAssignObject(e.target.value)}
                    />
                    <Button
                      type="primary"
                      loading={assignPermissionMutation.isPending}
                      disabled={!assignRoleName.trim() || !assignRelation.trim() || !assignObject.trim()}
                      onClick={() =>
                        assignPermissionMutation.mutate(
                          {
                            roleName: assignRoleName.trim(),
                            relation: assignRelation.trim(),
                            object: assignObject.trim(),
                          },
                          {
                            onSuccess: () => {
                              setAssignRoleName('');
                              setAssignRelation('viewer');
                              setAssignObject('document');
                            },
                          },
                        )}
                    >
                      Assign
                    </Button>
                  </Space>

                  <Typography.Text strong>Assign Role To User</Typography.Text>
                  <Space wrap>
                    <Input placeholder="user id" value={userId} onChange={(e) => setUserId(e.target.value)} />
                    <Select
                      style={{ width: 220 }}
                      showSearch
                      placeholder="role name"
                      value={userRoleName || undefined}
                      options={roleOptions}
                      onChange={(value) => setUserRoleName(value)}
                    />
                    <Button
                      type="primary"
                      loading={assignUserRoleMutation.isPending}
                      disabled={!userId.trim() || !userRoleName.trim()}
                      onClick={() =>
                        assignUserRoleMutation.mutate(
                          { userId: userId.trim(), roleName: userRoleName.trim() },
                          {
                            onSuccess: () => {
                              setUserRoleName('');
                            },
                          },
                        )}
                    >
                      Assign Role
                    </Button>
                  </Space>

                  <Typography.Text type="secondary">
                    Tip: choose a user in the Users tab to quickly inspect current role assignments.
                  </Typography.Text>
                </Space>
              ),
            },
            {
              key: 'users',
              label: 'Users',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Typography.Text strong>Create User</Typography.Text>
                  <Space wrap>
                    <Input
                      placeholder="user id (user:alice)"
                      value={newUserId}
                      onChange={(e) => setNewUserId(e.target.value)}
                    />
                    <Input
                      placeholder="email"
                      value={newUserEmail}
                      onChange={(e) => setNewUserEmail(e.target.value)}
                    />
                    <Input
                      placeholder="display name"
                      value={newUserDisplayName}
                      onChange={(e) => setNewUserDisplayName(e.target.value)}
                    />
                    <Button
                      type="primary"
                      loading={createUserMutation.isPending}
                      disabled={!newUserId.trim()}
                      onClick={() =>
                        createUserMutation.mutate(
                          {
                            userId: newUserId.trim(),
                            email: newUserEmail.trim() || undefined,
                            displayName: newUserDisplayName.trim() || undefined,
                          },
                          {
                            onSuccess: () => {
                              setNewUserId('');
                              setNewUserEmail('');
                              setNewUserDisplayName('');
                            },
                          },
                        )}
                    >
                      Create User
                    </Button>
                  </Space>

                  <Input
                    style={{ maxWidth: 360 }}
                    placeholder="Search by user id, email, or display name"
                    value={userSearch}
                    onChange={(e) => setUserSearch(e.target.value)}
                  />

                  {usersQuery.isLoading ? (
                    <TableSkeleton rows={4} columns={5} />
                  ) : filteredUsers.length === 0 ? (
                    <TableEmptyState message="No users found. Create your first user to start role assignment." />
                  ) : (
                    <Table
                      rowKey="userId"
                      dataSource={filteredUsers}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      scroll={{ x: 'max-content' }}
                      columns={[
                        { title: 'User ID', dataIndex: 'userId', key: 'userId' },
                        {
                          title: 'Email',
                          dataIndex: 'email',
                          key: 'email',
                          render: (value: string | null | undefined, row: { userId: string; displayName?: string | null }) => (
                            <Input
                              placeholder="email"
                              defaultValue={value ?? ''}
                              onBlur={(e) => {
                                const email = e.target.value.trim();
                                updateUserMutation.mutate({
                                  userId: row.userId,
                                  email: email || undefined,
                                  displayName: row.displayName ?? undefined,
                                });
                              }}
                            />
                          ),
                        },
                        {
                          title: 'Display Name',
                          dataIndex: 'displayName',
                          key: 'displayName',
                          render: (value: string | null | undefined, row: { userId: string; email?: string | null }) => (
                            <Input
                              placeholder="display name"
                              defaultValue={value ?? ''}
                              onBlur={(e) => {
                                const displayName = e.target.value.trim();
                                updateUserMutation.mutate({
                                  userId: row.userId,
                                  email: row.email ?? undefined,
                                  displayName: displayName || undefined,
                                });
                              }}
                            />
                          ),
                        },
                        {
                          title: 'Created At',
                          dataIndex: 'createdAt',
                          key: 'createdAt',
                          render: (value: string) => new Date(value).toLocaleString(),
                        },
                        {
                          title: 'Actions',
                          key: 'actions',
                          render: (_: unknown, row: { userId: string }) => (
                            <Space>
                              <Button
                                size="small"
                                onClick={() => {
                                  setSelectedUserId(row.userId);
                                  setUserId(row.userId);
                                }}
                              >
                                View Roles
                              </Button>
                              <Popconfirm
                                title="Delete user"
                                description={`Delete ${row.userId}?`}
                                okText="Delete"
                                okButtonProps={{ danger: true }}
                                onConfirm={() =>
                                  deleteUserMutation.mutate(row.userId, {
                                    onSuccess: () => {
                                      if (selectedUserId === row.userId) {
                                        setSelectedUserId(null);
                                      }
                                    },
                                  })}
                              >
                                <Button size="small" danger loading={deleteUserMutation.isPending}>
                                  Delete
                                </Button>
                              </Popconfirm>
                            </Space>
                          ),
                        },
                      ]}
                    />
                  )}

                  {selectedUserId ? (
                    <Card size="small" title={`Roles of ${selectedUserId}`}>
                      {userRolesQuery.isLoading ? (
                        <Typography.Text type="secondary">Loading roles...</Typography.Text>
                      ) : (userRolesQuery.data?.roles ?? []).length === 0 ? (
                        <Typography.Text type="secondary">No roles assigned.</Typography.Text>
                      ) : (
                        <Space wrap>
                          {(userRolesQuery.data?.roles ?? []).map((role) => (
                            <Typography.Text key={role} code>
                              {role}
                            </Typography.Text>
                          ))}
                        </Space>
                      )}
                    </Card>
                  ) : null}
                </Space>
              ),
            },
          ]}
        />

        {rolesQuery.error ? <Alert type="error" showIcon message={(rolesQuery.error as Error).message} /> : null}
        {permissionsQuery.error ? <Alert type="error" showIcon message={(permissionsQuery.error as Error).message} /> : null}
        {usersQuery.error ? <Alert type="error" showIcon message={(usersQuery.error as Error).message} /> : null}
        {userRolesQuery.error ? <Alert type="error" showIcon message={(userRolesQuery.error as Error).message} /> : null}
      </Space>
    </Card>
  );
}



