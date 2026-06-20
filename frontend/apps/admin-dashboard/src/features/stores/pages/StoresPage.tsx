import { useState } from 'react';
import { Button, Card, Col, Input, Popconfirm, Row, Space, Statistic, Table, Tag, Tooltip, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, TableSkeleton, TableEmptyState } from '@/shared/ui';
import { useNotification } from '@/shared/hooks';
import { tableColumnWidths, tableEllipsisMax } from '@/shared/utils';

export function StoresPage() {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();
  const { activeStoreId, setActiveStoreId } = useActiveStore();
  const notification = useNotification();
  const [storeName, setStoreName] = useState('');

  const storesQuery = useQuery({
    queryKey: ['stores'],
    queryFn: () => apiClient.listStores(),
    enabled: isAuthenticated,
  });

  const createStoreMutation = useMutation({
    mutationFn: (name: string) => apiClient.createStore(name),
    onSuccess: () => {
      setStoreName('');
      queryClient.invalidateQueries({ queryKey: ['stores'] });
      notification.success('Store created successfully');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create store');
    },
  });

  const deleteStoreMutation = useMutation({
    mutationFn: (storeId: string) => apiClient.deleteStore(storeId),
    onSuccess: (_, deletedStoreId) => {
      if (activeStoreId === deletedStoreId) {
        setActiveStoreId('');
      }
      queryClient.invalidateQueries({ queryKey: ['stores'] });
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to delete store');
    },
  });

  const handleDeleteStore = (storeId: string) => {
    deleteStoreMutation.mutate(storeId);
  };

  const handleCreateStore = () => {
    const name = storeName.trim();
    if (!name) {
      notification.warning('Please enter a store name');
      return;
    }

    createStoreMutation.mutate(name);
  };

  if (!isAuthenticated) {
    return <AccessGate title="Stores" message="Login first to manage stores." />;
  }

  const stores = storesQuery.data ?? [];
  const showEmptyState = !storesQuery.isLoading && stores.length === 0;
  const totalModels = stores.reduce((sum, store) => sum + (store.modelCount ?? 0), 0);
  const totalRelationships = stores.reduce((sum, store) => sum + (store.relationshipCount ?? 0), 0);

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Stores
          </Typography.Title>
          <Typography.Text type="secondary">
            Create stores and set the active authorization context.
          </Typography.Text>
        </div>

        <Space wrap>
          <Input
            style={{ width: 280 }}
            placeholder="new-store-name"
            value={storeName}
            onChange={(e) => setStoreName(e.target.value)}
          />
          <Button type="primary" onClick={handleCreateStore} loading={createStoreMutation.isPending}>
            Create Store
          </Button>
        </Space>

        <Row gutter={[12, 12]}>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Stores" value={stores.length} loading={storesQuery.isLoading} />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Active store" value={activeStoreId ? 1 : 0} suffix="/ 1" />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Model versions" value={totalModels} loading={storesQuery.isLoading} />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Relationship tuples" value={totalRelationships} loading={storesQuery.isLoading} />
            </Card>
          </Col>
        </Row>

        {storesQuery.isLoading ? (
          <TableSkeleton rows={4} columns={4} />
        ) : showEmptyState ? (
          <TableEmptyState message="No stores created yet. Create your first store to get started." />
        ) : (
          <Table
            rowKey="id"
            dataSource={stores}
            pagination={{ pageSize: 10, showSizeChanger: true }}
            scroll={{ x: 'max-content' }}
            columns={[
              {
                title: 'Name',
                dataIndex: 'name',
                key: 'name',
                width: tableColumnWidths.base,
                render: (value: string) => (
                  <Tooltip title={value}>
                    <Typography.Text ellipsis style={{ maxWidth: tableEllipsisMax.text }}>
                      {value}
                    </Typography.Text>
                  </Tooltip>
                ),
              },
              {
                title: 'ID',
                dataIndex: 'id',
                key: 'id',
                width: tableColumnWidths.id,
                render: (value: string) => (
                  <Tooltip title={value}>
                    <Typography.Text code ellipsis style={{ maxWidth: tableEllipsisMax.value }}>
                      {value}
                    </Typography.Text>
                  </Tooltip>
                ),
              },
              {
                title: 'Status',
                key: 'status',
                render: (_, row) =>
                  activeStoreId === row.id ? <Tag color="processing">Active</Tag> : <Tag>Idle</Tag>,
              },
              {
                title: 'Models',
                dataIndex: 'modelCount',
                key: 'modelCount',
                width: tableColumnWidths.compact,
                render: (value?: number | null) => (typeof value === 'number' ? value : '-'),
              },
              {
                title: 'Relationships',
                dataIndex: 'relationshipCount',
                key: 'relationshipCount',
                width: tableColumnWidths.relation,
                render: (value?: number | null) => (typeof value === 'number' ? value : '-'),
              },
              {
                title: 'Actions',
                key: 'actions',
                render: (_, row) => (
                  <Space>
                    <Button
                      onClick={() => setActiveStoreId(row.id)}
                      type={activeStoreId === row.id ? 'primary' : 'default'}
                      style={{ minWidth: 96 }}
                    >
                      {activeStoreId === row.id ? 'Active' : 'Set Active'}
                    </Button>
                    <Popconfirm
                      title="Delete Store?"
                      description={`This will permanently delete "${row.name}" and all associated authorization models and relationships.`}
                      okText="Delete"
                      cancelText="Cancel"
                      okButtonProps={{
                        danger: true,
                      }}
                      onConfirm={() => handleDeleteStore(row.id)}
                    >
                      <Button danger disabled={deleteStoreMutation.isPending}>
                        Delete
                      </Button>
                    </Popconfirm>
                  </Space>
                ),
              },
            ]}
          />
        )}

        {storesQuery.error ? (
          <Typography.Text type="danger">{(storesQuery.error as Error).message}</Typography.Text>
        ) : null}
      </Space>
    </Card>
  );
}


