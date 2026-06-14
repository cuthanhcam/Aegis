import { useState } from 'react';
import { Alert, Button, Card, Input, Select, Space, Table, Tabs, Tooltip, Typography, message } from 'antd';
import { CopyOutlined, DownloadOutlined } from '@ant-design/icons';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { useAuditEventsQuery, useStoreChangesQuery } from '@/features/audit/api/useAuditApi';
import { AccessGate } from '@/shared/ui';

export function AuditPage() {
  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();
  const [activeTab, setActiveTab] = useState<'changes' | 'audit'>('changes');
  const [typeFilter, setTypeFilter] = useState('');
  const [continuationToken, setContinuationToken] = useState('');
  const [pageSize, setPageSize] = useState('50');
  const [auditAction, setAuditAction] = useState('');
  const [auditDecision, setAuditDecision] = useState('');

  const changesQuery = useStoreChangesQuery({
    isAuthenticated,
    activeStoreId,
    typeFilter,
    continuationToken,
    pageSize,
  });

  const auditEventsQuery = useAuditEventsQuery(isAuthenticated, auditAction, auditDecision);

  if (!isAuthenticated) {
    return <AccessGate title="Store Changes" message="Login from the sidebar first." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Store Changes" message="Set an active store first." />;
  }

  const exportChanges = () => {
    if (!changesQuery.data) {
      return;
    }

    const blob = new Blob([JSON.stringify(changesQuery.data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `audit-${activeStoreId}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const exportAuditEvents = () => {
    if (!auditEventsQuery.data) {
      return;
    }

    const blob = new Blob([JSON.stringify(auditEventsQuery.data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'audit-events.json';
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const copyNextToken = async () => {
    const next = changesQuery.data?.continuation_token;
    if (!next) {
      return;
    }

    await navigator.clipboard.writeText(next);
    message.success('Continuation token copied.');
  };

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Store Changes & Audit Events
          </Typography.Title>
          <Typography.Text type="secondary">
            Inspect the store-scoped change feed and tenant-wide audit events in one place.
          </Typography.Text>
        </div>

        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'changes' | 'audit')}
          items={[
            {
              key: 'changes',
              label: 'Store Changes',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input
                      style={{ width: 220 }}
                      placeholder="type filter (e.g. document)"
                      value={typeFilter}
                      onChange={(e) => setTypeFilter(e.target.value)}
                    />
                    <Input
                      style={{ width: 180 }}
                      placeholder="page size"
                      value={pageSize}
                      onChange={(e) => setPageSize(e.target.value)}
                    />
                    <Input
                      style={{ width: 420 }}
                      placeholder="continuation token"
                      value={continuationToken}
                      onChange={(e) => setContinuationToken(e.target.value)}
                    />
                    <Button loading={changesQuery.isFetching} onClick={() => changesQuery.refetch()}>
                      Load Changes
                    </Button>
                    <Button
                      disabled={!changesQuery.data?.continuation_token}
                      onClick={() => setContinuationToken(changesQuery.data?.continuation_token ?? '')}
                    >
                      Next Page
                    </Button>
                    <Button icon={<CopyOutlined />} disabled={!changesQuery.data?.continuation_token} onClick={copyNextToken}>
                      Copy Next Token
                    </Button>
                    <Button icon={<DownloadOutlined />} disabled={!changesQuery.data} onClick={exportChanges}>
                      Export JSON
                    </Button>
                  </Space>

                  {changesQuery.error ? <Alert type="error" showIcon message={(changesQuery.error as Error).message} /> : null}

                  <Table
                    rowKey={(row) => `${row.subject}|${row.relation}|${row.object}|${row.createdAt}|${row.operation}`}
                    loading={changesQuery.isLoading}
                    dataSource={changesQuery.data?.changes ?? []}
                    pagination={{ pageSize: 10, showSizeChanger: true }}
                    scroll={{ x: 'max-content' }}
                    columns={[
                      {
                        title: 'Subject',
                        dataIndex: 'subject',
                        key: 'subject',
                        width: 260,
                        render: (value: string) => (
                          <Tooltip title={value}>
                            <Typography.Text ellipsis style={{ maxWidth: 240 }}>
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
                        width: 280,
                        render: (value: string) => (
                          <Tooltip title={value}>
                            <Typography.Text ellipsis style={{ maxWidth: 260 }}>
                              {value}
                            </Typography.Text>
                          </Tooltip>
                        ),
                      },
                      { title: 'Operation', dataIndex: 'operation', key: 'operation' },
                      {
                        title: 'Created At',
                        dataIndex: 'createdAt',
                        key: 'createdAt',
                        render: (value: string) => new Date(value).toLocaleString('en-US'),
                      },
                    ]}
                  />

                  {changesQuery.data?.continuation_token ? (
                    <Typography.Text code>{changesQuery.data.continuation_token}</Typography.Text>
                  ) : (
                    <Typography.Text type="secondary">No continuation token returned.</Typography.Text>
                  )}
                </Space>
              ),
            },
            {
              key: 'audit',
              label: 'Audit Events',
              children: (
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <Space wrap>
                    <Input
                      style={{ width: 220 }}
                      placeholder="action filter (check/explain)"
                      value={auditAction}
                      onChange={(e) => setAuditAction(e.target.value)}
                    />
                    <Select
                      allowClear
                      style={{ width: 220 }}
                      placeholder="decision"
                      value={auditDecision || undefined}
                      options={[
                        { value: 'allow', label: 'allow' },
                        { value: 'deny', label: 'deny' },
                      ]}
                      onChange={(value) => setAuditDecision(value ?? '')}
                    />
                    <Button loading={auditEventsQuery.isFetching} onClick={() => auditEventsQuery.refetch()}>
                      Load Audit Events
                    </Button>
                    <Button icon={<DownloadOutlined />} disabled={!auditEventsQuery.data} onClick={exportAuditEvents}>
                      Export JSON
                    </Button>
                  </Space>

                  {auditEventsQuery.error ? <Alert type="error" showIcon message={(auditEventsQuery.error as Error).message} /> : null}

                  <Table
                    rowKey={(row) => `${row.action}|${row.subject}|${row.relation}|${row.object}|${row.createdAt}`}
                    loading={auditEventsQuery.isLoading}
                    dataSource={auditEventsQuery.data ?? []}
                    pagination={{ pageSize: 10, showSizeChanger: true }}
                    scroll={{ x: 'max-content' }}
                    columns={[
                      { title: 'Action', dataIndex: 'action', key: 'action' },
                      {
                        title: 'Subject',
                        dataIndex: 'subject',
                        key: 'subject',
                        width: 240,
                        render: (value: string) => (
                          <Tooltip title={value}>
                            <Typography.Text ellipsis style={{ maxWidth: 220 }}>
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
                        width: 260,
                        render: (value: string) => (
                          <Tooltip title={value}>
                            <Typography.Text ellipsis style={{ maxWidth: 240 }}>
                              {value}
                            </Typography.Text>
                          </Tooltip>
                        ),
                      },
                      { title: 'Decision', dataIndex: 'decision', key: 'decision' },
                      { title: 'Reason', dataIndex: 'reasonCode', key: 'reasonCode' },
                      {
                        title: 'Created At',
                        dataIndex: 'createdAt',
                        key: 'createdAt',
                        render: (value: string) => new Date(value).toLocaleString('en-US'),
                      },
                    ]}
                  />
                </Space>
              ),
            },
          ]}
        />
      </Space>
    </Card>
  );
}



