import { useMemo, useState } from 'react';
import { Alert, Button, Card, Input, Select, Space, Table, Tabs, Typography } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, JsonEditor, TableEmptyState } from '@/shared/ui';

const CONSISTENCY_OPTIONS = [
  { value: 'fully_consistent', label: 'fully_consistent' },
  { value: 'minimize_latency', label: 'minimize_latency' },
];

export function GraphExplorerPage() {
  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();

  const [usersRelation, setUsersRelation] = useState('viewer');
  const [usersObject, setUsersObject] = useState('document:roadmap');
  const [usersConsistency, setUsersConsistency] = useState('');
  const [usersModelId, setUsersModelId] = useState('');
  const [usersResult, setUsersResult] = useState<string[]>([]);

  const [objectsUser, setObjectsUser] = useState('user:anne');
  const [objectsRelation, setObjectsRelation] = useState('viewer');
  const [objectsType, setObjectsType] = useState('document');
  const [objectsConsistency, setObjectsConsistency] = useState('');
  const [objectsModelId, setObjectsModelId] = useState('');
  const [objectsResult, setObjectsResult] = useState<string[]>([]);

  const [expandRelation, setExpandRelation] = useState('viewer');
  const [expandObject, setExpandObject] = useState('document:roadmap');
  const [expandConsistency, setExpandConsistency] = useState('');
  const [expandModelId, setExpandModelId] = useState('');
  const [expandResult, setExpandResult] = useState<unknown>(null);

  const listUsersMutation = useMutation({
    mutationFn: () =>
      apiClient.listUsersInStore(activeStoreId!, {
        relation: usersRelation,
        object: usersObject,
        consistency: usersConsistency || undefined,
        authorizationModelId: usersModelId || undefined,
      }),
    onSuccess: (data) => setUsersResult(data.users),
  });

  const listObjectsMutation = useMutation({
    mutationFn: () =>
      apiClient.listObjectsInStore(activeStoreId!, {
        user: objectsUser,
        relation: objectsRelation,
        type: objectsType,
        consistency: objectsConsistency || undefined,
        authorizationModelId: objectsModelId || undefined,
      }),
    onSuccess: (data) => setObjectsResult(data.objects),
  });

  const expandMutation = useMutation({
    mutationFn: () =>
      apiClient.expandInStore(activeStoreId!, {
        relation: expandRelation,
        object: expandObject,
        consistency: expandConsistency || undefined,
        authorizationModelId: expandModelId || undefined,
      }),
    onSuccess: (data) => setExpandResult(data),
  });

  const errors = useMemo(() => {
    return [listUsersMutation.error, listObjectsMutation.error, expandMutation.error].filter(Boolean) as Error[];
  }, [listUsersMutation.error, listObjectsMutation.error, expandMutation.error]);

  if (!isAuthenticated) {
    return <AccessGate title="Graph Explorer" message="Login from sidebar first." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Graph Explorer" message="Set an active store first." />;
  }

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Graph Explorer
          </Typography.Title>
          <Typography.Text type="secondary">
            Explore graph endpoints for list-users, list-objects, and expand using current active store.
          </Typography.Text>
        </div>

        {errors.map((error) => (
          <Alert key={error.message} type="error" showIcon message={error.message} />
        ))}

        <Tabs
          items={[
            {
              key: 'list-users',
              label: 'List Users',
              children: (
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input style={{ width: 220 }} value={usersRelation} onChange={(e) => setUsersRelation(e.target.value)} placeholder="relation" />
                    <Input style={{ width: 300 }} value={usersObject} onChange={(e) => setUsersObject(e.target.value)} placeholder="object" />
                    <Select
                      allowClear
                      style={{ width: 180 }}
                      value={usersConsistency || undefined}
                      placeholder="consistency"
                      options={CONSISTENCY_OPTIONS}
                      onChange={(value) => setUsersConsistency(value ?? '')}
                    />
                    <Input
                      style={{ width: 280 }}
                      value={usersModelId}
                      onChange={(e) => setUsersModelId(e.target.value)}
                      placeholder="authorization model id (optional)"
                    />
                    <Button
                      type="primary"
                      loading={listUsersMutation.isPending}
                      disabled={!usersRelation.trim() || !usersObject.trim()}
                      onClick={() => listUsersMutation.mutate()}
                    >
                      Run
                    </Button>
                  </Space>

                  {usersResult.length === 0 ? (
                    <TableEmptyState message="No users yet. Run query to see matching users." />
                  ) : (
                    <Table
                      rowKey={(value) => value}
                      dataSource={usersResult}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      columns={[
                        {
                          title: 'User',
                          dataIndex: '',
                          key: 'user',
                        },
                      ]}
                    />
                  )}
                </Space>
              ),
            },
            {
              key: 'list-objects',
              label: 'List Objects',
              children: (
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input style={{ width: 220 }} value={objectsUser} onChange={(e) => setObjectsUser(e.target.value)} placeholder="user" />
                    <Input style={{ width: 220 }} value={objectsRelation} onChange={(e) => setObjectsRelation(e.target.value)} placeholder="relation" />
                    <Input style={{ width: 180 }} value={objectsType} onChange={(e) => setObjectsType(e.target.value)} placeholder="type" />
                    <Select
                      allowClear
                      style={{ width: 180 }}
                      value={objectsConsistency || undefined}
                      placeholder="consistency"
                      options={CONSISTENCY_OPTIONS}
                      onChange={(value) => setObjectsConsistency(value ?? '')}
                    />
                    <Input
                      style={{ width: 280 }}
                      value={objectsModelId}
                      onChange={(e) => setObjectsModelId(e.target.value)}
                      placeholder="authorization model id (optional)"
                    />
                    <Button
                      type="primary"
                      loading={listObjectsMutation.isPending}
                      disabled={!objectsUser.trim() || !objectsRelation.trim() || !objectsType.trim()}
                      onClick={() => listObjectsMutation.mutate()}
                    >
                      Run
                    </Button>
                  </Space>

                  {objectsResult.length === 0 ? (
                    <TableEmptyState message="No objects yet. Run query to see matching objects." />
                  ) : (
                    <Table
                      rowKey={(value) => value}
                      dataSource={objectsResult}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      columns={[
                        {
                          title: 'Object',
                          dataIndex: '',
                          key: 'object',
                        },
                      ]}
                    />
                  )}
                </Space>
              ),
            },
            {
              key: 'expand',
              label: 'Expand',
              children: (
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input style={{ width: 220 }} value={expandRelation} onChange={(e) => setExpandRelation(e.target.value)} placeholder="relation" />
                    <Input style={{ width: 300 }} value={expandObject} onChange={(e) => setExpandObject(e.target.value)} placeholder="object" />
                    <Select
                      allowClear
                      style={{ width: 180 }}
                      value={expandConsistency || undefined}
                      placeholder="consistency"
                      options={CONSISTENCY_OPTIONS}
                      onChange={(value) => setExpandConsistency(value ?? '')}
                    />
                    <Input
                      style={{ width: 280 }}
                      value={expandModelId}
                      onChange={(e) => setExpandModelId(e.target.value)}
                      placeholder="authorization model id (optional)"
                    />
                    <Button
                      type="primary"
                      loading={expandMutation.isPending}
                      disabled={!expandRelation.trim() || !expandObject.trim()}
                      onClick={() => expandMutation.mutate()}
                    >
                      Run
                    </Button>
                  </Space>

                  {expandResult ? (
                    <JsonEditor
                      readOnly
                      value={JSON.stringify(expandResult, null, 2)}
                      onChange={() => {}}
                      height={320}
                      path={`inmemory://model/graph-expand-result-${activeStoreId}.json`}
                    />
                  ) : (
                    <TableEmptyState message="No expand result yet. Run query to inspect tree." />
                  )}
                </Space>
              ),
            },
          ]}
        />
      </Space>
    </Card>
  );
}
