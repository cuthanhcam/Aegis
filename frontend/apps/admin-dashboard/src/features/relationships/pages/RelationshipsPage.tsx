import { useEffect, useState } from 'react';
import { Button, Card, Input, Popconfirm, Select, Space, Table, Tooltip, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, TableSkeleton, TableEmptyState } from '@/shared/ui';
import { useNotification, useUrlState } from '@/shared/hooks';
import { tableColumnWidths, tableEllipsisMax } from '@/shared/utils';

export function RelationshipsPage() {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();
  const notification = useNotification();
  const { getState: getFilterState, setState: setFilterState } = useUrlState({
    defaultValues: {
      subject: '',
      relation: '',
      object: '',
    },
  });

  const [subject, setSubject] = useState('');
  const [relation, setRelation] = useState('');
  const [objectValue, setObjectValue] = useState('');
  const [effect, setEffect] = useState('allow');
  const [selectedRowKeys, setSelectedRowKeys] = useState<string[]>([]);
  const [selectedRows, setSelectedRows] = useState<Array<{ subject: string; relation: string; object: string }>>([]);

  const filterSubject = getFilterState('subject', '');
  const filterRelation = getFilterState('relation', '');
  const filterObject = getFilterState('object', '');

  const relationshipsQuery = useQuery({
    queryKey: ['relationships', activeStoreId, filterSubject, filterRelation, filterObject],
    queryFn: () =>
      apiClient.listRelationships(activeStoreId, {
        subject: filterSubject || undefined,
        relation: filterRelation || undefined,
        obj: filterObject || undefined,
      }),
    enabled: isAuthenticated && Boolean(activeStoreId),
  });

  const upsertMutation = useMutation({
    mutationFn: () =>
      apiClient.upsertRelationship(activeStoreId, {
        subject,
        relation,
        object: objectValue,
        effect,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['relationships', activeStoreId] });
      setSubject('');
      setRelation('');
      setObjectValue('');
      setEffect('allow');
      notification.success('Relationship saved successfully');
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to save relationship');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (payload: { subject: string; relation: string; object: string }) =>
      apiClient.deleteRelationship(activeStoreId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['relationships', activeStoreId] });
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to delete relationship');
    },
  });

  const bulkDeleteMutation = useMutation({
    mutationFn: async (rows: Array<{ subject: string; relation: string; object: string }>) => {
      const uniqueRows = Array.from(
        new Map(rows.map((row) => [`${row.subject}|${row.relation}|${row.object}`, row])).values(),
      );

      const results = await Promise.allSettled(
        uniqueRows.map((row) =>
          apiClient.deleteRelationship(activeStoreId, {
            subject: row.subject,
            relation: row.relation,
            object: row.object,
          }),
        ),
      );

      const failed = results.filter((result) => result.status === 'rejected').length;
      return { total: uniqueRows.length, failed };
    },
    onSuccess: ({ total, failed }) => {
      queryClient.invalidateQueries({ queryKey: ['relationships', activeStoreId] });
      setSelectedRowKeys([]);
      setSelectedRows([]);

      if (failed === 0) {
        notification.success(`Deleted ${total} relationship${total === 1 ? '' : 's'} successfully`);
        return;
      }

      notification.warning(`Deleted ${total - failed}/${total} relationships. ${failed} failed.`);
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to delete selected relationships');
    },
  });

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSelectedRowKeys([]);
      setSelectedRows([]);
    }, 0);

    return () => window.clearTimeout(timer);
  }, [activeStoreId, filterObject, filterRelation, filterSubject]);

  if (!isAuthenticated) {
    return <AccessGate title="Relationships" message="Login first to manage relationships." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Relationships" message="Set an active store first." />;
  }

  const relationships = relationshipsQuery.data ?? [];
  const showEmptyState = !relationshipsQuery.isLoading && relationships.length === 0;

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Relationships
          </Typography.Title>
          <Typography.Text type="secondary">Manage relationship tuples for store: {activeStoreId}</Typography.Text>
        </div>

        <Space wrap>
          <Input
            style={{ width: 220 }}
            placeholder="subject (user:anne)"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
          />
          <Input
            style={{ width: 180 }}
            placeholder="relation (viewer)"
            value={relation}
            onChange={(e) => setRelation(e.target.value)}
          />
          <Input
            style={{ width: 260 }}
            placeholder="object (document:plan)"
            value={objectValue}
            onChange={(e) => setObjectValue(e.target.value)}
          />
          <Select
            style={{ width: 120 }}
            value={effect}
            options={[
              { value: 'allow', label: 'allow' },
              { value: 'deny', label: 'deny' },
            ]}
            onChange={(value) => setEffect(value)}
          />
          <Button
            type="primary"
            onClick={() => upsertMutation.mutate()}
            loading={upsertMutation.isPending}
            disabled={!subject.trim() || !relation.trim() || !objectValue.trim()}
          >
            Save
          </Button>
        </Space>

        <Space wrap>
          <Input
            style={{ width: 220 }}
            placeholder="filter subject"
            value={filterSubject}
            onChange={(e) => setFilterState('subject', e.target.value)}
          />
          <Input
            style={{ width: 180 }}
            placeholder="filter relation"
            value={filterRelation}
            onChange={(e) => setFilterState('relation', e.target.value)}
          />
          <Input
            style={{ width: 260 }}
            placeholder="filter object"
            value={filterObject}
            onChange={(e) => setFilterState('object', e.target.value)}
          />
          <Button
            onClick={() => relationshipsQuery.refetch()}
            loading={relationshipsQuery.isFetching}
          >
            Apply
          </Button>
          <Popconfirm
            title="Delete selected relationships?"
            description={`This will permanently delete ${selectedRows.length} selected relationship${selectedRows.length === 1 ? '' : 's'}.`}
            okText="Delete"
            cancelText="Cancel"
            okButtonProps={{ danger: true, loading: bulkDeleteMutation.isPending }}
            onConfirm={() => bulkDeleteMutation.mutate(selectedRows)}
            disabled={selectedRows.length === 0}
          >
            <Button
              danger
              disabled={selectedRows.length === 0 || bulkDeleteMutation.isPending}
              loading={bulkDeleteMutation.isPending}
            >
              Delete Selected ({selectedRows.length})
            </Button>
          </Popconfirm>
        </Space>

        {relationshipsQuery.isLoading ? (
          <TableSkeleton rows={4} columns={5} />
        ) : showEmptyState ? (
          <TableEmptyState message="No relationships found. Create your first relationship tuple to get started." />
        ) : (
          <Table
            rowKey={(row) => `${row.subject}|${row.relation}|${row.object}|${row.effect}`}
            dataSource={relationships}
            pagination={{ pageSize: 10, showSizeChanger: true }}
            scroll={{ x: 'max-content' }}
            rowSelection={{
              selectedRowKeys,
              onChange: (keys, rows) => {
                setSelectedRowKeys(keys.map(String));
                setSelectedRows(
                  rows.map((row) => ({
                    subject: row.subject,
                    relation: row.relation,
                    object: row.object,
                  })),
                );
              },
            }}
            columns={[
              {
                title: 'Subject',
                dataIndex: 'subject',
                key: 'subject',
                width: tableColumnWidths.text,
                render: (value: string) => (
                  <Tooltip title={value}>
                    <Typography.Text ellipsis style={{ maxWidth: tableEllipsisMax.subject }}>
                      {value}
                    </Typography.Text>
                  </Tooltip>
                ),
              },
              { title: 'Relation', dataIndex: 'relation', key: 'relation' },
              {
                title: 'Object',
                dataIndex: 'object',
                key: 'object',
                width: tableColumnWidths.object,
                render: (value: string) => (
                  <Tooltip title={value}>
                    <Typography.Text ellipsis style={{ maxWidth: tableEllipsisMax.object }}>
                      {value}
                    </Typography.Text>
                  </Tooltip>
                ),
              },
              { title: 'Effect', dataIndex: 'effect', key: 'effect' },
              {
                title: 'Actions',
                key: 'actions',
                render: (_, row) => (
                  <Popconfirm
                    title="Delete Relationship?"
                    description={`This will permanently delete the relationship: ${row.subject} ${row.relation} ${row.object}`}
                    okText="Delete"
                    cancelText="Cancel"
                    okButtonProps={{ danger: true, loading: deleteMutation.isPending }}
                    onConfirm={() =>
                      deleteMutation.mutate({
                        subject: row.subject,
                        relation: row.relation,
                        object: row.object,
                      })
                    }
                  >
                    <Button danger disabled={deleteMutation.isPending} loading={deleteMutation.isPending}>
                      Delete
                    </Button>
                  </Popconfirm>
                ),
              },
            ]}
          />
        )}

        {relationshipsQuery.error ? (
          <Typography.Text type="danger">
            {(relationshipsQuery.error as Error).message}
          </Typography.Text>
        ) : null}
      </Space>
    </Card>
  );
}



