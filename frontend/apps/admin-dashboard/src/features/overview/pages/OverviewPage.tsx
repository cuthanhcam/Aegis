import {
  ApiOutlined,
  AuditOutlined,
  CheckCircleOutlined,
  ClusterOutlined,
  DatabaseOutlined,
  DeploymentUnitOutlined,
  FileTextOutlined,
  NodeIndexOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons';
import { Alert, Button, Card, Col, Descriptions, Progress, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, TableEmptyState } from '@/shared/ui';

type WorkflowStep = {
  key: string;
  title: string;
  description: string;
  path: string;
  ready: boolean;
};

function parsePrometheusMetric(text: string | undefined, metricName: string) {
  if (!text) {
    return null;
  }

  const line = text
    .split('\n')
    .find((item) => item.startsWith(metricName) && !item.startsWith('#'));

  if (!line) {
    return null;
  }

  const value = Number(line.trim().split(/\s+/).at(-1));
  return Number.isFinite(value) ? value : null;
}

export function OverviewPage() {
  const navigate = useNavigate();
  const { isAuthenticated, accessToken } = useAuth();
  const { activeStoreId } = useActiveStore();

  const profileQuery = useQuery({
    queryKey: ['overview-profile', accessToken],
    queryFn: () => apiClient.getProfile(),
    enabled: Boolean(accessToken),
  });

  const storesQuery = useQuery({
    queryKey: ['stores'],
    queryFn: () => apiClient.listStores(),
    enabled: isAuthenticated,
  });

  const modelsQuery = useQuery({
    queryKey: ['models', activeStoreId],
    queryFn: () => apiClient.listAuthorizationModels(activeStoreId),
    enabled: isAuthenticated && Boolean(activeStoreId),
  });

  const relationshipsQuery = useQuery({
    queryKey: ['relationships', activeStoreId, '', '', ''],
    queryFn: () => apiClient.listRelationships(activeStoreId),
    enabled: isAuthenticated && Boolean(activeStoreId),
  });

  const metricsQuery = useQuery({
    queryKey: ['authorization-metrics'],
    queryFn: () => apiClient.getAuthorizationMetrics(),
    enabled: isAuthenticated,
    retry: false,
  });

  if (!isAuthenticated) {
    return <AccessGate title="Overview" message="Login first to inspect your authorization workspace." />;
  }

  const stores = storesQuery.data ?? [];
  const activeStore = stores.find((store) => store.id === activeStoreId);
  const models = modelsQuery.data ?? [];
  const relationships = relationshipsQuery.data ?? [];
  const activeModel = models[0];

  const workflow: WorkflowStep[] = [
    {
      key: 'store',
      title: 'Create a store',
      description: 'Stores isolate models, tuples, assertions, and graph queries by tenant.',
      path: '/stores',
      ready: stores.length > 0,
    },
    {
      key: 'model',
      title: 'Publish an authorization model',
      description: 'Define types, direct relations, computed relations, and inheritance rules.',
      path: '/models',
      ready: models.length > 0,
    },
    {
      key: 'tuples',
      title: 'Write relationship tuples',
      description: 'Add subject-relation-object edges such as user:anne viewer document:roadmap.',
      path: '/relationships',
      ready: relationships.length > 0,
    },
    {
      key: 'evaluate',
      title: 'Evaluate access',
      description: 'Run check, explain, batch-check, list-users, list-objects, and expand.',
      path: '/test-console',
      ready: false,
    },
  ];

  const readinessScore = Math.round((workflow.filter((step) => step.ready).length / workflow.length) * 100);

  const dbQueryCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_db_queries_total');
  const dbResultCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_db_results_total');
  const memoHitCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_memo_hits_total');
  const memoMissCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_memo_misses_total');
  const checkCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_checks_total');
  const allowCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_allowed_total');
  const denyCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_denied_total');
  const errorCount = parsePrometheusMetric(metricsQuery.data, 'aegis_authorization_errors_total');

  const quickActions = [
    { path: '/models', label: 'Model Playground', icon: <FileTextOutlined /> },
    { path: '/relationships', label: 'Tuple Explorer', icon: <NodeIndexOutlined /> },
    { path: '/test-console', label: 'Run Check', icon: <CheckCircleOutlined /> },
    { path: '/graph', label: 'Graph Queries', icon: <ClusterOutlined /> },
    { path: '/assertions', label: 'Assertion Suites', icon: <DeploymentUnitOutlined /> },
    { path: '/audit', label: 'Audit Trail', icon: <AuditOutlined /> },
  ];

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Row gutter={[16, 16]}>
        <Col xs={24} xl={16}>
          <Card className="hero-panel">
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
              <Space wrap style={{ justifyContent: 'space-between', width: '100%' }}>
                <div>
                  <Typography.Text className="pro-kicker">Authorization command center</Typography.Text>
                  <Typography.Title level={3} style={{ margin: '4px 0 6px' }}>
                    Build, test, and operate fine-grained access control.
                  </Typography.Title>
                  <Typography.Text type="secondary">
                    Aegis maps the OpenFGA-style workflow into tenant stores, model versions, relationship tuples,
                    explainable checks, graph queries, assertions, presets, and audit evidence.
                  </Typography.Text>
                </div>
                <Tag color={activeStore ? 'success' : 'warning'}>
                  {activeStore ? `Active: ${activeStore.name}` : 'No active store'}
                </Tag>
              </Space>
              <Space wrap>
                {quickActions.map((action) => (
                  <Button key={action.path} icon={action.icon} onClick={() => navigate(action.path)}>
                    {action.label}
                  </Button>
                ))}
              </Space>
            </Space>
          </Card>
        </Col>
        <Col xs={24} xl={8}>
          <Card>
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
              <div>
                <Typography.Text className="pro-kicker">Production readiness</Typography.Text>
                <Typography.Title level={4} style={{ marginBottom: 4 }}>
                  Workspace setup
                </Typography.Title>
              </div>
              <Progress percent={readinessScore} strokeColor="#FF3366" />
              <Space direction="vertical" size={8} style={{ width: '100%' }}>
                {workflow.map((step) => (
                  <div key={step.key} className="readiness-row">
                    <CheckCircleOutlined className={step.ready ? 'readiness-ok' : 'readiness-pending'} />
                    <button type="button" className="link-button" onClick={() => navigate(step.path)}>
                      {step.title}
                    </button>
                  </div>
                ))}
              </Space>
            </Space>
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]}>
        <Col xs={24} md={12} xl={6}>
          <Card>
            <Statistic title="Stores" value={stores.length} prefix={<DatabaseOutlined />} loading={storesQuery.isLoading} />
          </Card>
        </Col>
        <Col xs={24} md={12} xl={6}>
          <Card>
            <Statistic title="Model versions" value={models.length} prefix={<FileTextOutlined />} loading={modelsQuery.isLoading} />
          </Card>
        </Col>
        <Col xs={24} md={12} xl={6}>
          <Card>
            <Statistic
              title="Relationship tuples"
              value={relationships.length}
              prefix={<NodeIndexOutlined />}
              loading={relationshipsQuery.isLoading}
            />
          </Card>
        </Col>
        <Col xs={24} md={12} xl={6}>
          <Card>
            <Statistic
              title="Auth checks"
              value={checkCount ?? 0}
              prefix={<SafetyCertificateOutlined />}
              loading={metricsQuery.isLoading}
            />
          </Card>
        </Col>
      </Row>

      {metricsQuery.error ? (
        <Alert
          type="info"
          showIcon
          message="Metrics endpoint is not available yet. Core management workflows still work."
        />
      ) : null}

      <Row gutter={[16, 16]}>
        <Col xs={24} xl={14}>
          <Card title="Recommended workflow">
            <Table
              rowKey="key"
              dataSource={workflow}
              pagination={false}
              columns={[
                {
                  title: 'Step',
                  dataIndex: 'title',
                  key: 'title',
                  render: (value: string, row) => (
                    <Space direction="vertical" size={0}>
                      <Typography.Text strong>{value}</Typography.Text>
                      <Typography.Text type="secondary">{row.description}</Typography.Text>
                    </Space>
                  ),
                },
                {
                  title: 'Status',
                  key: 'status',
                  width: 140,
                  render: (_, row) => (row.ready ? <Tag color="success">Ready</Tag> : <Tag>Pending</Tag>),
                },
                {
                  title: 'Action',
                  key: 'action',
                  width: 140,
                  render: (_, row) => (
                    <Button size="small" type={row.ready ? 'default' : 'primary'} onClick={() => navigate(row.path)}>
                      Open
                    </Button>
                  ),
                },
              ]}
            />
          </Card>
        </Col>
        <Col xs={24} xl={10}>
          <Card title="Active context">
            {activeStore ? (
              <Descriptions column={1} size="small" bordered>
                <Descriptions.Item label="Tenant">{profileQuery.data?.tenantId ?? '-'}</Descriptions.Item>
                <Descriptions.Item label="Store">{activeStore.name}</Descriptions.Item>
                <Descriptions.Item label="Store ID">
                  <Typography.Text code copyable>
                    {activeStore.id}
                  </Typography.Text>
                </Descriptions.Item>
                <Descriptions.Item label="Latest model">
                  {activeModel ? (
                    <Typography.Text code copyable>
                      {activeModel.id}
                    </Typography.Text>
                  ) : (
                    '-'
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Metrics">
                  <Space wrap>
                    <Tag color="success">allow {allowCount ?? 0}</Tag>
                    <Tag color="error">deny {denyCount ?? 0}</Tag>
                    <Tag color={errorCount ? 'warning' : 'default'}>error {errorCount ?? 0}</Tag>
                    <Tag color="processing">memo hit {memoHitCount ?? 0}</Tag>
                    <Tag>memo miss {memoMissCount ?? 0}</Tag>
                    <Tag>db query {dbQueryCount ?? 0}</Tag>
                    <Tag>db rows {dbResultCount ?? 0}</Tag>
                  </Space>
                </Descriptions.Item>
              </Descriptions>
            ) : (
              <TableEmptyState message="Create or select a store to activate model, tuple, graph, and assertion workflows." />
            )}
          </Card>
        </Col>
      </Row>

      <Card title="API surface map">
        <Row gutter={[12, 12]}>
          {[
            ['Stores', 'GET/POST /stores'],
            ['Models', 'GET/POST /stores/{storeId}/authorization-models'],
            ['Tuples', 'GET/POST/DELETE /stores/{storeId}/relationships'],
            ['Check', 'POST /stores/{storeId}/check'],
            ['Explain', 'POST /stores/{storeId}/explain'],
            ['Graph', 'POST /stores/{storeId}/graph/list-users|list-objects|expand'],
            ['Assertions', 'GET/POST /stores/{storeId}/assertions/{modelId}'],
            ['Audit', 'GET /tenants/{tenantId}/audit'],
          ].map(([title, endpoint]) => (
            <Col key={title} xs={24} md={12} xl={6}>
              <div className="api-map-item">
                <ApiOutlined />
                <div>
                  <Typography.Text strong>{title}</Typography.Text>
                  <Typography.Text code>{endpoint}</Typography.Text>
                </div>
              </div>
            </Col>
          ))}
        </Row>
      </Card>
    </Space>
  );
}
