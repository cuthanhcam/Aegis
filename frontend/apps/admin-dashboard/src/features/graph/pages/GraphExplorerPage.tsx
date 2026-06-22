import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Input, Row, Select, Space, Statistic, Table, Tabs, Tag, Tree, Typography } from 'antd';
import type { TreeProps } from 'antd';
import { useMutation } from '@tanstack/react-query';
import type { ExpandNode } from '@aegis/types/src/graph';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, JsonEditor, TableEmptyState } from '@/shared/ui';

const CONSISTENCY_OPTIONS = [
  { value: 'fully_consistent', label: 'fully_consistent' },
  { value: 'minimize_latency', label: 'minimize_latency' },
];

type GraphPreset = {
  usersRelation: string;
  usersObject: string;
  objectsUser: string;
  objectsRelation: string;
  objectsType: string;
  expandRelation: string;
  expandObject: string;
};

function getGraphPreset(storeId: string): GraphPreset {
  const normalized = storeId.toLowerCase();

  if (normalized.includes('support')) {
    return {
      usersRelation: 'viewer',
      usersObject: 'ticket:INC-1001',
      objectsUser: 'user:agent1',
      objectsRelation: 'viewer',
      objectsType: 'ticket',
      expandRelation: 'viewer',
      expandObject: 'ticket:INC-1001',
    };
  }

  if (normalized.includes('billing')) {
    return {
      usersRelation: 'viewer',
      usersObject: 'account:acme',
      objectsUser: 'user:finance',
      objectsRelation: 'viewer',
      objectsType: 'account',
      expandRelation: 'viewer',
      expandObject: 'account:acme',
    };
  }

  if (normalized.includes('analytics')) {
    return {
      usersRelation: 'viewer',
      usersObject: 'dashboard:quality',
      objectsUser: 'user:intern',
      objectsRelation: 'viewer',
      objectsType: 'dashboard',
      expandRelation: 'viewer',
      expandObject: 'dashboard:quality',
    };
  }

  if (normalized.includes('lab') || normalized.includes('dev')) {
    return {
      usersRelation: 'viewer',
      usersObject: 'project:aegis-lab',
      objectsUser: 'user:intern',
      objectsRelation: 'viewer',
      objectsType: 'project',
      expandRelation: 'viewer',
      expandObject: 'project:aegis-lab',
    };
  }

  return {
    usersRelation: 'viewer',
    usersObject: 'document:roadmap',
    objectsUser: 'user:anne',
    objectsRelation: 'viewer',
    objectsType: 'document',
    expandRelation: 'viewer',
    expandObject: 'document:roadmap',
  };
}

function countExpandNodes(node: ExpandNode): number {
  return 1 + node.children.reduce((total, child) => total + countExpandNodes(child), 0);
}

function countExpandUsers(node: ExpandNode): number {
  return node.users.length + node.children.reduce((total, child) => total + countExpandUsers(child), 0);
}

function buildExpandTreeData(node: ExpandNode, path = 'root'): NonNullable<TreeProps['treeData']>[number] {
  const userChildren = node.users.map((user, index) => ({
    key: `${path}:user:${index}:${user}`,
    title: (
      <Space size={8}>
        <Tag color="success">user</Tag>
        <Typography.Text copyable>{user}</Typography.Text>
      </Space>
    ),
  }));

  return {
    key: `${path}:${node.kind}:${node.node}`,
    title: (
      <Space size={8} wrap>
        <Tag color={node.kind === 'object' ? 'processing' : 'default'}>{node.kind}</Tag>
        <Typography.Text strong copyable>{node.node}</Typography.Text>
        <Typography.Text type="secondary">{node.users.length} users</Typography.Text>
      </Space>
    ),
    children: [
      ...userChildren,
      ...node.children.map((child, index) => buildExpandTreeData(child, `${path}:child:${index}`)),
    ],
  };
}

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
  const [expandResult, setExpandResult] = useState<ExpandNode | null>(null);

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

  const expandTreeData = useMemo(() => (expandResult ? [buildExpandTreeData(expandResult)] : []), [expandResult]);
  const expandNodeCount = useMemo(() => (expandResult ? countExpandNodes(expandResult) : 0), [expandResult]);
  const expandUserCount = useMemo(() => (expandResult ? countExpandUsers(expandResult) : 0), [expandResult]);

  useEffect(() => {
    if (!activeStoreId) {
      return;
    }

    const timer = window.setTimeout(() => {
      const preset = getGraphPreset(activeStoreId);
      setUsersRelation(preset.usersRelation);
      setUsersObject(preset.usersObject);
      setObjectsUser(preset.objectsUser);
      setObjectsRelation(preset.objectsRelation);
      setObjectsType(preset.objectsType);
      setExpandRelation(preset.expandRelation);
      setExpandObject(preset.expandObject);
      setUsersResult([]);
      setObjectsResult([]);
      setExpandResult(null);
    }, 0);

    return () => window.clearTimeout(timer);
  }, [activeStoreId]);

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
                    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                      <Row gutter={[16, 16]}>
                        <Col xs={24} md={8}>
                          <Statistic title="Expanded nodes" value={expandNodeCount} />
                        </Col>
                        <Col xs={24} md={8}>
                          <Statistic title="Resolved users" value={expandUserCount} />
                        </Col>
                        <Col xs={24} md={8}>
                          <Statistic title="Direct children" value={expandResult.children.length} />
                        </Col>
                      </Row>

                      <Tree
                        showLine
                        defaultExpandAll
                        treeData={expandTreeData}
                        style={{ padding: 12, border: '1px solid #f0f0f0', borderRadius: 8 }}
                      />

                      <Tabs
                        size="small"
                        items={[
                          {
                            key: 'raw',
                            label: 'Raw JSON',
                            children: (
                              <JsonEditor
                                readOnly
                                value={JSON.stringify(expandResult, null, 2)}
                                onChange={() => {}}
                                height={280}
                                path={`inmemory://model/graph-expand-result-${activeStoreId}.json`}
                              />
                            ),
                          },
                        ]}
                      />
                    </Space>
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
