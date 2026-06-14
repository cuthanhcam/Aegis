import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Input, Popconfirm, Segmented, Select, Space, Table, Tag, Tooltip, Typography, Upload, message } from 'antd';
import { DownloadOutlined, UploadOutlined } from '@ant-design/icons';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import {
  useAssertionModelsQuery,
  useAssertionPresetDeleteMutation,
  useAssertionPresetSaveMutation,
  useAssertionPresetsQuery,
  useAssertionsQuery,
  useWriteAssertionsMutation,
} from '@/features/assertions/api/useAssertionsApi';
import { AccessGate, JsonDiffView, JsonEditor, TableEmptyState } from '@/shared/ui';
import { useNotification } from '@/shared/hooks';
import { clearLaunchPreset, getLaunchPreset } from '@/features/presets/utils';
import { getDocumentViewerAssertionSeedPresets } from '@/features/assertions/utils';
import { tableColumnWidths, tableEllipsisMax } from '@/shared/utils';

type AssertionTuple = {
  tuple_key: {
    user: string;
    relation: string;
    object: string;
  };
  expectation: boolean;
};

type AssertionsPayload = {
  assertions: AssertionTuple[];
};

const ASSERTION_SCHEMA = {
  uri: 'aegis://schemas/assertions.json',
  schema: {
    type: 'object',
    required: ['assertions'],
    additionalProperties: false,
    properties: {
      assertions: {
        type: 'array',
        items: {
          type: 'object',
          required: ['tuple_key', 'expectation'],
          additionalProperties: false,
          properties: {
            tuple_key: {
              type: 'object',
              required: ['user', 'relation', 'object'],
              additionalProperties: false,
              properties: {
                user: { type: 'string' },
                relation: { type: 'string' },
                object: { type: 'string' },
              },
            },
            expectation: { type: 'boolean' },
          },
        },
      },
    },
  },
};

function parseAndValidateAssertionsPayload(rawJson: string): AssertionsPayload {
  const parsed = JSON.parse(rawJson) as {
    assertions?: Array<{ tuple_key?: { user?: string; relation?: string; object?: string }; expectation?: unknown }>;
  };

  const rootKeys = Object.keys(parsed as object);
  if (rootKeys.length !== 1 || !rootKeys.includes('assertions')) {
    throw new Error('Payload must contain only the "assertions" property.');
  }

  if (!Array.isArray(parsed.assertions)) {
    throw new Error('assertions must be an array.');
  }

  parsed.assertions.forEach((item, index) => {
    const tuple = item?.tuple_key;
    if (!tuple || typeof tuple !== 'object') {
      throw new Error(`assertions[${index}].tuple_key must be an object.`);
    }

    const tupleKeys = Object.keys(tuple);
    if (tupleKeys.length !== 3 || !tupleKeys.includes('user') || !tupleKeys.includes('relation') || !tupleKeys.includes('object')) {
      throw new Error(`assertions[${index}].tuple_key must include only user, relation, object.`);
    }

    if (!tuple.user?.trim() || !tuple.relation?.trim() || !tuple.object?.trim()) {
      throw new Error(`assertions[${index}].tuple_key values must be non-empty strings.`);
    }

    const itemKeys = Object.keys(item as object);
    if (itemKeys.length !== 2 || !itemKeys.includes('tuple_key') || !itemKeys.includes('expectation')) {
      throw new Error(`assertions[${index}] must include only tuple_key and expectation.`);
    }

    if (typeof item.expectation !== 'boolean') {
      throw new Error(`assertions[${index}].expectation must be boolean.`);
    }
  });

  return parsed as AssertionsPayload;
}

export function AssertionsPage() {
  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();
  const notification = useNotification();
  const [jsonError, setJsonError] = useState('');

  const [authorizationModelId, setAuthorizationModelId] = useState('');
  const [presetName, setPresetName] = useState('');
  const [selectedPresetName, setSelectedPresetName] = useState('');
  const [assertionViewMode, setAssertionViewMode] = useState<'table' | 'cards'>('cards');
  const [validateMessage, setValidateMessage] = useState('');
  const [assertionsJson, setAssertionsJson] = useState(
    JSON.stringify(
      {
        assertions: [
          {
            tuple_key: {
              user: 'user:anne',
              relation: 'viewer',
              object: 'document:roadmap',
            },
            expectation: true,
          },
        ],
      },
      null,
      2,
    ),
  );

  const presetsQuery = useAssertionPresetsQuery(isAuthenticated, activeStoreId, authorizationModelId);

  const presets = useMemo(() => {
    const seed = getDocumentViewerAssertionSeedPresets();
    const stored = presetsQuery.data ?? [];
    return [...seed, ...stored.filter((item) => !seed.some((s) => s.name === item.name))];
  }, [presetsQuery.data]);

  const selectedPresetPayload = useMemo(() => {
    if (!selectedPresetName) {
      return '';
    }

    return presets.find((item) => item.name === selectedPresetName)?.payload ?? '';
  }, [presets, selectedPresetName]);

  useEffect(() => {
    const launch = getLaunchPreset();
    if (!launch || launch.source !== 'assertions') {
      return;
    }

    const timer = window.setTimeout(() => {
      setAuthorizationModelId(launch.item.scope);
      setSelectedPresetName(launch.item.name);
      setAssertionsJson(launch.item.payload);
      clearLaunchPreset();
      message.success(`Loaded preset: ${launch.item.name}`);
    }, 0);

    return () => window.clearTimeout(timer);
  }, []);

  const modelsQuery = useAssertionModelsQuery(isAuthenticated, activeStoreId);

  const assertionsQuery = useAssertionsQuery(isAuthenticated, activeStoreId, authorizationModelId);

  const writeMutation = useWriteAssertionsMutation(() => {
    setJsonError('');
  });

  const presetSaveMutation = useAssertionPresetSaveMutation(() => {
    presetsQuery.refetch();
  });

  const presetDeleteMutation = useAssertionPresetDeleteMutation(() => {
    presetsQuery.refetch();
  });

  const parsedPayload = useMemo(() => {
    try {
      const payload = JSON.parse(assertionsJson) as {
        assertions?: Array<{ tuple_key?: { user?: string; relation?: string; object?: string }; expectation?: boolean }>;
      };
      return payload;
    } catch {
      return null;
    }
  }, [assertionsJson]);

  const assertionRows = parsedPayload?.assertions ?? [];
  const allowCount = assertionRows.filter((item) => Boolean(item.expectation)).length;
  const denyCount = assertionRows.length - allowCount;

  if (!isAuthenticated) {
    return <AccessGate title="Assertions" message="Login from sidebar first." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Assertions" message="Set an active store first." />;
  }

  const canLoad = Boolean(authorizationModelId.trim());
  const canSave = Boolean(authorizationModelId.trim()) && Array.isArray(parsedPayload?.assertions);

  const formatJson = () => {
    try {
      const payload = JSON.parse(assertionsJson);
      setAssertionsJson(JSON.stringify(payload, null, 2));
      setJsonError('');
    } catch (error) {
      setJsonError(error instanceof Error ? error.message : 'Invalid JSON payload.');
    }
  };

  const exportJson = () => {
    const blob = new Blob([assertionsJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `assertions-${authorizationModelId || 'draft'}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const handleSave = () => {
    try {
      const payload = parseAndValidateAssertionsPayload(assertionsJson);
      writeMutation.mutate({
        activeStoreId,
        authorizationModelId,
        assertions: payload.assertions,
      });
    } catch (error) {
      setJsonError(error instanceof Error ? error.message : 'Invalid JSON payload.');
    }
  };

  const validateOnly = () => {
    setValidateMessage('');
    try {
      parseAndValidateAssertionsPayload(assertionsJson);

      setValidateMessage('Payload is valid for assertions contract.');
    } catch (error) {
      setJsonError(error instanceof Error ? error.message : 'Validation failed.');
    }
  };

  const handleSavePreset = async () => {
    if (!activeStoreId || !authorizationModelId || !presetName.trim()) {
      return;
    }

    try {
      await presetSaveMutation.mutateAsync({
        activeStoreId,
        authorizationModelId,
        name: presetName.trim(),
        assertionsJson,
      });
      setPresetName('');
      message.success('Preset saved.');
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Failed to save preset.');
    }
  };

  const handleLoadPreset = () => {
    if (!activeStoreId || !authorizationModelId || !selectedPresetName) {
      return;
    }

    const found = presets.find((item) => item.name === selectedPresetName);
    if (!found) {
      return;
    }

    setAssertionsJson(found.payload);
    setJsonError('');
    message.success('Preset loaded.');
  };

  const handleDeletePreset = async () => {
    if (!activeStoreId || !authorizationModelId || !selectedPresetName) {
      return;
    }

    if (getDocumentViewerAssertionSeedPresets().some((item) => item.name === selectedPresetName)) {
      notification.warning('Seed presets cannot be deleted.');
      return;
    }

    try {
      await presetDeleteMutation.mutateAsync({
        activeStoreId,
        authorizationModelId,
        name: selectedPresetName,
      });
      setSelectedPresetName('');
      notification.success('Preset deleted successfully');
    } catch (error) {
      notification.error(error instanceof Error ? error.message : 'Failed to delete preset');
    }
  };

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Assertions
          </Typography.Title>
          <Typography.Text type="secondary">Run assertion suites against model versions in the active store.</Typography.Text>
        </div>

        <Space wrap>
          <Select
            showSearch
            style={{ width: 460 }}
            placeholder="Select authorization model ID"
            loading={modelsQuery.isLoading}
            value={authorizationModelId || undefined}
            options={(modelsQuery.data ?? []).map((m) => ({ value: m.id, label: `${m.id} (${m.schemaVersion})` }))}
            onChange={(value) => setAuthorizationModelId(value)}
          />
          <Button disabled={!canLoad} onClick={() => assertionsQuery.refetch()}>
            Load
          </Button>
        </Space>

        <JsonEditor
          value={assertionsJson}
          onChange={setAssertionsJson}
          height={320}
          path={`inmemory://model/assertions-${activeStoreId}-${authorizationModelId || 'draft'}.json`}
          schema={ASSERTION_SCHEMA}
        />

        <Space wrap>
          <Button type="primary" disabled={!canSave} loading={writeMutation.isPending} onClick={handleSave}>
            Save Assertions
          </Button>
          <Button onClick={validateOnly}>Validate Only</Button>
          <Button onClick={formatJson}>Format JSON</Button>
          <Button icon={<DownloadOutlined />} onClick={exportJson}>
            Export JSON
          </Button>
          <Upload
            accept="application/json"
            showUploadList={false}
            beforeUpload={async (file) => {
              const content = await file.text();
              setAssertionsJson(content);
              setJsonError('');
              message.success('Assertions JSON loaded.');
              return false;
            }}
          >
            <Button icon={<UploadOutlined />}>Import JSON</Button>
          </Upload>
        </Space>

        <Space wrap>
          <Input
            style={{ width: 220 }}
            placeholder="new preset name"
            value={presetName}
            onChange={(e) => setPresetName(e.target.value)}
          />
          <Button disabled={!presetName.trim() || !authorizationModelId.trim()} onClick={handleSavePreset}>
            Save Preset
          </Button>
          <Select
            style={{ width: 280 }}
            placeholder="load preset"
            value={selectedPresetName || undefined}
            options={presets.map((item) => ({ value: item.name, label: `${item.name} (${new Date(item.updatedAt).toLocaleString('en-US')})` }))}
            onChange={(value) => setSelectedPresetName(value)}
          />
          <Button disabled={!selectedPresetName} onClick={handleLoadPreset}>
            Load Preset
          </Button>
          <Popconfirm
            title="Delete Preset?"
            description={`This will permanently delete preset "${selectedPresetName}".`}
            okText="Delete"
            cancelText="Cancel"
            okButtonProps={{ danger: true, loading: presetDeleteMutation.isPending }}
            onConfirm={handleDeletePreset}
          >
            <Button danger disabled={!selectedPresetName || presetDeleteMutation.isPending}>
              Delete Preset
            </Button>
          </Popconfirm>
        </Space>

        {jsonError ? <Alert type="error" showIcon message={jsonError} /> : null}
        {validateMessage ? <Alert type="success" showIcon message={validateMessage} /> : null}
        {assertionsQuery.error ? <Alert type="error" showIcon message={(assertionsQuery.error as Error).message} /> : null}
        {writeMutation.error ? <Alert type="error" showIcon message={(writeMutation.error as Error).message} /> : null}
        {writeMutation.isSuccess ? <Alert type="success" showIcon message="Assertions saved." /> : null}

        {assertionRows.length === 0 ? (
          <TableEmptyState message="No assertions loaded. Load an authorization model or create assertions above." />
        ) : (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Space wrap style={{ justifyContent: 'space-between', width: '100%' }}>
              <Space wrap>
                <Tag color="blue">Total: {assertionRows.length}</Tag>
                <Tag color="green">Allow: {allowCount}</Tag>
                <Tag color="red">Deny: {denyCount}</Tag>
              </Space>
              <Segmented
                options={[
                  { label: 'Cards', value: 'cards' },
                  { label: 'Table', value: 'table' },
                ]}
                value={assertionViewMode}
                onChange={(value) => setAssertionViewMode(value as 'table' | 'cards')}
              />
            </Space>

            {assertionViewMode === 'table' ? (
              <Table
                rowKey={(_, index) => `assertion-${index}`}
                dataSource={assertionRows}
                pagination={{ pageSize: 10, showSizeChanger: true }}
                scroll={{ x: 'max-content' }}
                columns={[
                  {
                    title: 'User',
                    dataIndex: ['tuple_key', 'user'],
                    key: 'user',
                    width: tableColumnWidths.text,
                    render: (value: string) => (
                      <Tooltip title={value}>
                        <Typography.Text ellipsis style={{ maxWidth: tableEllipsisMax.subject }}>
                          {value}
                        </Typography.Text>
                      </Tooltip>
                    ),
                  },
                  { title: 'Relation', dataIndex: ['tuple_key', 'relation'], key: 'relation' },
                  {
                    title: 'Object',
                    dataIndex: ['tuple_key', 'object'],
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
                  {
                    title: 'Expectation',
                    dataIndex: 'expectation',
                    key: 'expectation',
                    render: (value: boolean) => (value ? 'allow' : 'deny'),
                  },
                ]}
              />
            ) : (
              <Space direction="vertical" size="small" style={{ width: '100%' }}>
                {assertionRows.map((row, index) => (
                  <Card
                    key={`assertion-card-${index}`}
                    size="small"
                    title={`Assertion ${index + 1}`}
                    extra={<Tag color={row.expectation ? 'green' : 'red'}>{row.expectation ? 'allow' : 'deny'}</Tag>}
                  >
                    <Space direction="vertical" size={4} style={{ width: '100%' }}>
                      <Typography.Text>
                        <Typography.Text strong>User: </Typography.Text>
                        {row.tuple_key?.user ?? '-'}
                      </Typography.Text>
                      <Typography.Text>
                        <Typography.Text strong>Relation: </Typography.Text>
                        {row.tuple_key?.relation ?? '-'}
                      </Typography.Text>
                      <Typography.Text>
                        <Typography.Text strong>Object: </Typography.Text>
                        {row.tuple_key?.object ?? '-'}
                      </Typography.Text>
                    </Space>
                  </Card>
                ))}
              </Space>
            )}
          </Space>
        )}

        {assertionsQuery.data ? (
          <JsonEditor
            readOnly
            value={JSON.stringify(assertionsQuery.data, null, 2)}
            onChange={() => {}}
            height={260}
            path={`inmemory://model/assertions-read-result-${activeStoreId}-${authorizationModelId || 'draft'}.json`}
          />
        ) : null}

        {selectedPresetPayload ? (
          <Space direction="vertical" style={{ width: '100%' }}>
            <Typography.Text strong>Preset vs Current Payload (Diff)</Typography.Text>
            <JsonDiffView left={selectedPresetPayload} right={assertionsJson} />
          </Space>
        ) : null}
      </Space>
    </Card>
  );
}



