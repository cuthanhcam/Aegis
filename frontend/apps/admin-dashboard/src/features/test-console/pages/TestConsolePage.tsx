import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Table, Tabs, Tooltip, Typography, message } from 'antd';
import { useCallback } from 'react';
import { CopyOutlined, DeleteOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, JsonDiffView, JsonEditor, TableEmptyState } from '@/shared/ui';
import { useNotification } from '@/shared/hooks';
import { clearLaunchPreset, getLaunchPreset } from '@/features/presets/utils';
import { getDocumentViewerConsoleSeedPresets } from '@/features/test-console/utils';

type HistoryItem = {
  user: string;
  relation: string;
  object: string;
  consistency?: string;
  authorizationModelId?: string;
  createdAt: string;
};

type ConsolePreset = {
  name: string;
  user: string;
  relation: string;
  object: string;
  consistency?: string;
  authorizationModelId?: string;
  batchSize: string;
  contextualTuplesJson: string;
  contextJson: string;
  updatedAt: string;
};

type LaunchPresetPayload = {
  user?: string;
  relation?: string;
  object?: string;
  consistency?: string;
  authorizationModelId?: string;
  batchSize?: string;
  contextualTuplesJson?: string;
  contextJson?: string;
};

type LaunchPresetResolution = {
  name: string;
  payload: LaunchPresetPayload;
  parseError: boolean;
} | null;

const HISTORY_KEY = 'aegis:test-console:history';

const CONTEXTUAL_TUPLES_SCHEMA = {
  uri: 'aegis://schemas/contextual-tuples.json',
  schema: {
    type: 'array',
    items: {
      type: 'object',
      required: ['subject', 'relation', 'object'],
      additionalProperties: false,
      properties: {
        subject: { type: 'string' },
        relation: { type: 'string' },
        object: { type: 'string' },
        effect: { enum: ['allow', 'deny'] },
      },
    },
  },
};

const CONTEXT_SCHEMA = {
  uri: 'aegis://schemas/context-object.json',
  schema: {
    type: 'object',
    additionalProperties: true,
  },
};

function readHistory(): HistoryItem[] {
  try {
    const raw = localStorage.getItem(HISTORY_KEY);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw) as HistoryItem[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function writeHistory(items: HistoryItem[]) {
  localStorage.setItem(HISTORY_KEY, JSON.stringify(items.slice(0, 10)));
}

function resolveLaunchPreset(): LaunchPresetResolution {
  const launch = getLaunchPreset();
  if (!launch || launch.source !== 'test-console') {
    return null;
  }

  try {
    const payload = JSON.parse(launch.item.payload) as LaunchPresetPayload;
    return {
      name: launch.item.name,
      payload,
      parseError: false,
    };
  } catch {
    return {
      name: launch.item.name,
      payload: {},
      parseError: true,
    };
  }
}

export function TestConsolePage() {
  const launchPreset = useMemo(() => resolveLaunchPreset(), []);

  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();
  const notification = useNotification();

  const [user, setUser] = useState(launchPreset?.payload.user ?? 'user:anne');
  const [relation, setRelation] = useState(launchPreset?.payload.relation ?? 'viewer');
  const [objectValue, setObjectValue] = useState(launchPreset?.payload.object ?? 'document:roadmap');
  const [consistency, setConsistency] = useState(launchPreset?.payload.consistency ?? '');
  const [authorizationModelId, setAuthorizationModelId] = useState(launchPreset?.payload.authorizationModelId ?? '');
  const [batchSize, setBatchSize] = useState(launchPreset?.payload.batchSize ?? '1');
  const [contextualTuplesJson, setContextualTuplesJson] = useState(launchPreset?.payload.contextualTuplesJson ?? '[]');
  const [contextJson, setContextJson] = useState(launchPreset?.payload.contextJson ?? '{}');
  const [presetName, setPresetName] = useState('');
  const [selectedPresetName, setSelectedPresetName] = useState(launchPreset?.name ?? '');
  const [layoutMode, setLayoutMode] = useState<'stacked' | 'split'>('stacked');
  const [validateMessage, setValidateMessage] = useState('');
  const [result, setResult] = useState<unknown>(null);
  const [history, setHistory] = useState<HistoryItem[]>(() => readHistory());

  const presetsQuery = useQuery({
    queryKey: ['test-console-presets', activeStoreId],
    queryFn: async () => {
      const stored = await apiClient.listPresets({
        storeId: activeStoreId!,
        source: 'test-console',
        scope: 'global',
      });

      return stored.map<ConsolePreset>((item) => {
        const payload = JSON.parse(item.payload) as Omit<ConsolePreset, 'name' | 'updatedAt'>;
        return {
          name: item.name,
          updatedAt: item.updatedAt,
          user: payload.user,
          relation: payload.relation,
          object: payload.object,
          consistency: payload.consistency,
          authorizationModelId: payload.authorizationModelId,
          batchSize: payload.batchSize,
          contextualTuplesJson: payload.contextualTuplesJson,
          contextJson: payload.contextJson,
        };
      });
    },
    enabled: isAuthenticated && Boolean(activeStoreId),
  });

  const presets = useMemo(() => {
    const seed = getDocumentViewerConsoleSeedPresets();
    const stored = presetsQuery.data ?? [];
    return [...seed, ...stored.filter((item) => !seed.some((s) => s.name === item.name))];
  }, [presetsQuery.data]);

  const selectedPreset = useMemo(() => {
    if (!selectedPresetName) {
      return null;
    }

    return presets.find((item) => item.name === selectedPresetName) ?? null;
  }, [presets, selectedPresetName]);

  useEffect(() => {
    if (!launchPreset) {
      return;
    }

    if (launchPreset.parseError) {
      message.error('Failed to load preset payload.');
    } else {
      message.success(`Loaded preset: ${launchPreset.name}`);
    }

    clearLaunchPreset();
  }, [launchPreset]);

  const saveHistory = () => {
    const next: HistoryItem[] = [
      {
        user,
        relation,
        object: objectValue,
        consistency: consistency || undefined,
        authorizationModelId: authorizationModelId || undefined,
        createdAt: new Date().toISOString(),
      },
      ...history,
    ];

    setHistory(next.slice(0, 10));
    writeHistory(next);
  };

  const parseAdvancedPayload = useCallback(() => {
    const contextualTuples = JSON.parse(contextualTuplesJson) as Array<{
      subject: string;
      relation: string;
      object: string;
      effect?: 'allow' | 'deny';
    }>;
    const context = JSON.parse(contextJson) as Record<string, unknown>;

    return {
      contextualTuples: Array.isArray(contextualTuples) && contextualTuples.length > 0 ? contextualTuples : undefined,
      context: Object.keys(context).length > 0 ? context : undefined,
    };
  }, [contextJson, contextualTuplesJson]);

  const validateOnly = () => {
    setValidateMessage('');
    try {
      if (!user.trim() || !relation.trim() || !objectValue.trim()) {
        throw new Error('user, relation, object are required.');
      }

      const contextualTuples = JSON.parse(contextualTuplesJson);
      const context = JSON.parse(contextJson);

      if (!Array.isArray(contextualTuples)) {
        throw new Error('contextual tuples must be an array.');
      }

      if (typeof context !== 'object' || context === null || Array.isArray(context)) {
        throw new Error('context must be a JSON object.');
      }

      setValidateMessage('Payload is valid for test console request contract.');
    } catch (error) {
      const messageText = error instanceof Error ? error.message : 'Validation failed.';
      message.error(messageText);
    }
  };

  const checkMutation = useMutation({
    mutationFn: () => {
      const advanced = parseAdvancedPayload();
      return apiClient.checkInStore(activeStoreId, {
        user,
        relation,
        object: objectValue,
        contextualTuples: advanced.contextualTuples,
        context: advanced.context,
        consistency: consistency || undefined,
        authorizationModelId: authorizationModelId || undefined,
      });
    },
    onSuccess: (data) => {
      setResult(data);
      saveHistory();
    },
  });

  const explainMutation = useMutation({
    mutationFn: () => {
      const advanced = parseAdvancedPayload();
      return apiClient.explainInStore(activeStoreId, {
        user,
        relation,
        object: objectValue,
        contextualTuples: advanced.contextualTuples,
        context: advanced.context,
        consistency: consistency || undefined,
        authorizationModelId: authorizationModelId || undefined,
      });
    },
    onSuccess: (data) => {
      setResult(data);
      saveHistory();
    },
  });

  const batchMutation = useMutation({
    mutationFn: () => {
      const size = Math.max(1, Number(batchSize) || 1);
      return apiClient.batchCheckCompat(
        activeStoreId,
        Array.from({ length: size }).map((_, idx) => ({
          user,
          relation,
          object: objectValue,
          correlationId: `sample-${idx + 1}`,
        })),
      );
    },
    onSuccess: (data) => {
      setResult(data);
      saveHistory();
    },
  });

  const canRun = Boolean(user.trim()) && Boolean(relation.trim()) && Boolean(objectValue.trim());

  const anyPending = checkMutation.isPending || explainMutation.isPending || batchMutation.isPending;

  const errors = useMemo(() => {
    return [checkMutation.error, explainMutation.error, batchMutation.error].filter(Boolean) as Error[];
  }, [checkMutation.error, explainMutation.error, batchMutation.error]);

  const requestPreview = useMemo(() => {
    try {
      const advanced = parseAdvancedPayload();
      return {
        user,
        relation,
        object: objectValue,
        consistency: consistency || undefined,
        authorizationModelId: authorizationModelId || undefined,
        contextualTuples: advanced.contextualTuples,
        context: advanced.context,
      };
    } catch {
      return {
        user,
        relation,
        object: objectValue,
        consistency: consistency || undefined,
        authorizationModelId: authorizationModelId || undefined,
        contextualTuples: 'Invalid JSON',
        context: 'Invalid JSON',
      };
    }
  }, [authorizationModelId, consistency, objectValue, parseAdvancedPayload, relation, user]);

  const resultSummary = useMemo(() => {
    if (!result || typeof result !== 'object' || Array.isArray(result)) {
      return null;
    }

    const payload = result as { allowed?: boolean; decision?: string; reasonCode?: string; trace?: unknown[] };
    if (typeof payload.allowed !== 'boolean' && !payload.decision) {
      return null;
    }

    return payload;
  }, [result]);

  const contextualTuplePreview = useMemo(() => {
    try {
      const parsed = JSON.parse(contextualTuplesJson) as Array<{
        subject?: string;
        relation?: string;
        object?: string;
        effect?: 'allow' | 'deny';
      }>;

      if (!Array.isArray(parsed)) {
        return { items: [] as Array<{ subject: string; relation: string; object: string; effect: string }>, error: 'Contextual tuples must be an array.' };
      }

      return {
        items: parsed.map((item) => ({
          subject: item.subject ?? '',
          relation: item.relation ?? '',
          object: item.object ?? '',
          effect: item.effect ?? 'allow',
        })),
        error: '',
      };
    } catch (error) {
      return {
        items: [] as Array<{ subject: string; relation: string; object: string; effect: string }>,
        error: error instanceof Error ? error.message : 'Invalid contextual tuples JSON.',
      };
    }
  }, [contextualTuplesJson]);

  const contextPreview = useMemo(() => {
    try {
      const parsed = JSON.parse(contextJson) as Record<string, unknown>;
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
        return { entries: [] as Array<{ key: string; value: string }>, error: 'Context must be a JSON object.' };
      }

      return {
        entries: Object.entries(parsed).map(([key, value]) => ({ key, value: JSON.stringify(value) })),
        error: '',
      };
    } catch (error) {
      return {
        entries: [] as Array<{ key: string; value: string }>,
        error: error instanceof Error ? error.message : 'Invalid context JSON.',
      };
    }
  }, [contextJson]);

  if (!isAuthenticated) {
    return <AccessGate title="Test Console" message="Login from sidebar first." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Test Console" message="Set an active store first." />;
  }

  const runAll = async () => {
    if (!canRun) {
      return;
    }

    try {
      const check = await checkMutation.mutateAsync();
      const explain = await explainMutation.mutateAsync();
      const batch = await batchMutation.mutateAsync();
      setResult({ check, explain, batch });
      message.success('Executed check, explain and batch-check.');
    } catch {
      // Errors are already surfaced from query mutation states.
    }
  };

  const copyResult = async () => {
    if (!result) {
      return;
    }

    await navigator.clipboard.writeText(JSON.stringify(result, null, 2));
    message.success('Result copied.');
  };

  const savePreset = async () => {
    if (!activeStoreId || !presetName.trim()) {
      return;
    }

    try {
      await apiClient.upsertPreset({
        source: 'test-console',
        storeId: activeStoreId,
        scope: 'global',
        name: presetName.trim(),
        payload: JSON.stringify(
          {
            user,
            relation,
            object: objectValue,
            consistency: consistency || undefined,
            authorizationModelId: authorizationModelId || undefined,
            batchSize,
            contextualTuplesJson,
            contextJson,
          },
          null,
          2,
        ),
      });

      await presetsQuery.refetch();
      setPresetName('');
      message.success('Preset saved.');
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Failed to save preset.');
    }
  };

  const loadPreset = () => {
    if (!activeStoreId || !selectedPresetName) {
      return;
    }

    const found = presets.find((item) => item.name === selectedPresetName);
    if (!found) {
      return;
    }

    setUser(found.user);
    setRelation(found.relation);
    setObjectValue(found.object);
    setConsistency(found.consistency ?? '');
    setAuthorizationModelId(found.authorizationModelId ?? '');
    setBatchSize(found.batchSize);
    setContextualTuplesJson(found.contextualTuplesJson);
    setContextJson(found.contextJson);
    message.success('Preset loaded.');
  };

  const deletePreset = async () => {
    if (!activeStoreId || !selectedPresetName) {
      return;
    }

    if (getDocumentViewerConsoleSeedPresets().some((item) => item.name === selectedPresetName)) {
      notification.warning('Seed presets cannot be deleted.');
      return;
    }

    try {
      await apiClient.deletePreset({
        source: 'test-console',
        storeId: activeStoreId,
        scope: 'global',
        name: selectedPresetName,
      });

      await presetsQuery.refetch();
      setSelectedPresetName('');
    } catch (error) {
      notification.error(error instanceof Error ? error.message : 'Failed to delete preset');
    }
  };

  const inputPane = (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space wrap>
        <Input style={{ width: 220 }} value={user} onChange={(e) => setUser(e.target.value)} placeholder="user:anne" />
        <Input style={{ width: 180 }} value={relation} onChange={(e) => setRelation(e.target.value)} placeholder="viewer" />
        <Input style={{ width: 260 }} value={objectValue} onChange={(e) => setObjectValue(e.target.value)} placeholder="document:roadmap" />
        <Select
          allowClear
          style={{ width: 180 }}
          placeholder="consistency"
          value={consistency || undefined}
          options={[
            { value: 'fully_consistent', label: 'fully_consistent' },
            { value: 'minimize_latency', label: 'minimize_latency' },
          ]}
          onChange={(value) => setConsistency(value ?? '')}
        />
        <Input
          style={{ width: 280 }}
          value={authorizationModelId}
          onChange={(e) => setAuthorizationModelId(e.target.value)}
          placeholder="authorization model id (optional)"
        />
        <Input style={{ width: 130 }} value={batchSize} onChange={(e) => setBatchSize(e.target.value)} placeholder="batch size" />
      </Space>

      <Tabs
        items={[
          {
            key: 'advanced-tuples',
            label: 'Contextual Tuples JSON',
            children: (
              <Space direction="vertical" size="small" style={{ width: '100%' }}>
                <JsonEditor
                  value={contextualTuplesJson}
                  onChange={setContextualTuplesJson}
                  height={220}
                  path={`inmemory://model/contextual-tuples-${activeStoreId}.json`}
                  schema={CONTEXTUAL_TUPLES_SCHEMA}
                />
                {contextualTuplePreview.error ? <Alert type="error" showIcon message={contextualTuplePreview.error} /> : null}
                {contextualTuplePreview.items.length === 0 ? (
                  <TableEmptyState message="No contextual tuples. Add tuples in JSON editor to preview them here." />
                ) : (
                  <Table
                    size="small"
                    rowKey={(row) => `${row.subject}|${row.relation}|${row.object}`}
                    dataSource={contextualTuplePreview.items}
                    pagination={false}
                    scroll={{ x: 'max-content' }}
                    columns={[
                      { title: 'Subject', dataIndex: 'subject', key: 'subject', width: 240 },
                      { title: 'Relation', dataIndex: 'relation', key: 'relation', width: 160 },
                      { title: 'Object', dataIndex: 'object', key: 'object', width: 260 },
                      { title: 'Effect', dataIndex: 'effect', key: 'effect', width: 120 },
                    ]}
                  />
                )}
              </Space>
            ),
          },
          {
            key: 'advanced-context',
            label: 'Context JSON',
            children: (
              <Space direction="vertical" size="small" style={{ width: '100%' }}>
                <JsonEditor
                  value={contextJson}
                  onChange={setContextJson}
                  height={220}
                  path={`inmemory://model/context-${activeStoreId}.json`}
                  schema={CONTEXT_SCHEMA}
                />
                {contextPreview.error ? <Alert type="error" showIcon message={contextPreview.error} /> : null}
                {contextPreview.entries.length === 0 ? (
                  <TableEmptyState message="No context keys. Add key/value pairs in JSON editor to preview them here." />
                ) : (
                  <Table
                    size="small"
                    rowKey={(row) => row.key}
                    dataSource={contextPreview.entries}
                    pagination={false}
                    scroll={{ x: 'max-content' }}
                    columns={[
                      { title: 'Key', dataIndex: 'key', key: 'key', width: 220 },
                      {
                        title: 'Value',
                        dataIndex: 'value',
                        key: 'value',
                        render: (value: string) => (
                          <Typography.Text code ellipsis style={{ maxWidth: 560 }}>
                            {value}
                          </Typography.Text>
                        ),
                      },
                    ]}
                  />
                )}
              </Space>
            ),
          },
        ]}
      />

      <Space wrap>
        <Input style={{ width: 220 }} placeholder="new preset name" value={presetName} onChange={(e) => setPresetName(e.target.value)} />
        <Button disabled={!presetName.trim()} onClick={savePreset}>
          Save Preset
        </Button>
        <Select
          style={{ width: 320 }}
          placeholder="load preset"
          value={selectedPresetName || undefined}
          options={presets.map((item) => ({ value: item.name, label: `${item.name} (${new Date(item.updatedAt).toLocaleString('en-US')})` }))}
          onChange={(value) => setSelectedPresetName(value)}
        />
        <Button disabled={!selectedPresetName} onClick={loadPreset}>
          Load Preset
        </Button>
        <Popconfirm
          title="Delete Preset?"
          description={`This will permanently delete preset "${selectedPresetName}".`}
          okText="Delete"
          cancelText="Cancel"
          okButtonProps={{ danger: true }}
          onConfirm={deletePreset}
        >
          <Button danger disabled={!selectedPresetName}>
            Delete Preset
          </Button>
        </Popconfirm>
      </Space>
    </Space>
  );

  const outputPane = (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space wrap>
        <Button type="primary" disabled={!canRun} loading={checkMutation.isPending} onClick={() => checkMutation.mutate()}>
          Check
        </Button>
        <Button disabled={!canRun} loading={explainMutation.isPending} onClick={() => explainMutation.mutate()}>
          Explain
        </Button>
        <Button disabled={!canRun} loading={batchMutation.isPending} onClick={() => batchMutation.mutate()}>
          Batch-Check (Compat)
        </Button>
        <Button disabled={!canRun || anyPending} onClick={runAll}>
          Run All
        </Button>
        <Button onClick={validateOnly}>Validate Only</Button>
        <Button icon={<CopyOutlined />} disabled={!result} onClick={copyResult}>
          Copy Result
        </Button>
        <Button
          icon={<DeleteOutlined />}
          onClick={() => {
            setResult(null);
          }}
        >
          Clear Result
        </Button>
      </Space>

      {errors.map((error) => (
        <Alert key={error.message} type="error" showIcon message={error.message} />
      ))}
      {validateMessage ? <Alert type="success" showIcon message={validateMessage} /> : null}

      <Tabs
        items={[
          {
            key: 'result',
            label: 'Result',
            children: result ? (
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                {resultSummary ? (
                  <Alert
                    type={resultSummary.allowed ? 'success' : 'warning'}
                    showIcon
                    message={`Decision: ${resultSummary.decision ?? (resultSummary.allowed ? 'allow' : 'deny')}`}
                    description={`Reason: ${resultSummary.reasonCode ?? 'n/a'}${
                      resultSummary.trace ? ` • Trace steps: ${resultSummary.trace.length}` : ''
                    }`}
                  />
                ) : null}
                <JsonEditor
                  readOnly
                  value={JSON.stringify(result, null, 2)}
                  onChange={() => {}}
                  height={320}
                  path={`inmemory://model/test-console-result-${activeStoreId}.json`}
                />
              </Space>
            ) : (
              <Typography.Text type="secondary">No result yet.</Typography.Text>
            ),
          },
          {
            key: 'request-preview',
            label: 'Request Preview',
            children: (
              <JsonEditor
                readOnly
                value={JSON.stringify(requestPreview, null, 2)}
                onChange={() => {}}
                height={320}
                path={`inmemory://model/test-console-request-${activeStoreId}.json`}
              />
            ),
          },
          {
            key: 'history',
            label: 'Recent Inputs',
            children:
              history.length === 0 ? (
                <TableEmptyState message="No history yet. Run a check, explain, or batch-check to record inputs." />
              ) : (
                <Table
                  rowKey={(row) => `${row.user}|${row.relation}|${row.object}|${row.createdAt}`}
                  dataSource={history}
                  pagination={{ pageSize: 10, showSizeChanger: true }}
                  scroll={{ x: 'max-content' }}
                  columns={[
                    {
                      title: 'User',
                      dataIndex: 'user',
                      key: 'user',
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
                    {
                      title: 'Created',
                      dataIndex: 'createdAt',
                      key: 'createdAt',
                      render: (value: string) => new Date(value).toLocaleString('en-US'),
                    },
                    {
                      title: 'Action',
                      key: 'action',
                      render: (_, row) => (
                        <Button
                          size="small"
                          onClick={() => {
                            setUser(row.user);
                            setRelation(row.relation);
                            setObjectValue(row.object);
                            setConsistency(row.consistency ?? '');
                            setAuthorizationModelId(row.authorizationModelId ?? '');
                          }}
                        >
                          Reuse
                        </Button>
                      ),
                    },
                  ]}
                />
              ),
          },
          {
            key: 'preset-diff',
            label: 'Preset Diff',
            children: selectedPreset ? (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Typography.Text strong>Core Input Diff (Preset vs Current)</Typography.Text>
                <JsonDiffView
                  left={JSON.stringify(
                    {
                      user: selectedPreset.user,
                      relation: selectedPreset.relation,
                      object: selectedPreset.object,
                      consistency: selectedPreset.consistency,
                      authorizationModelId: selectedPreset.authorizationModelId,
                      batchSize: selectedPreset.batchSize,
                    },
                    null,
                    2,
                  )}
                  right={JSON.stringify(
                    {
                      user,
                      relation,
                      object: objectValue,
                      consistency,
                      authorizationModelId,
                      batchSize,
                    },
                    null,
                    2,
                  )}
                />
                <Typography.Text strong>Contextual Tuples Diff</Typography.Text>
                <JsonDiffView left={selectedPreset.contextualTuplesJson} right={contextualTuplesJson} />
                <Typography.Text strong>Context Diff</Typography.Text>
                <JsonDiffView left={selectedPreset.contextJson} right={contextJson} />
              </Space>
            ) : (
              <Typography.Text type="secondary">Select a preset to review diff.</Typography.Text>
            ),
          },
        ]}
      />
    </Space>
  );

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Test Console
          </Typography.Title>
          <Typography.Text type="secondary">Run check, explain, and compatibility batches against the active store.</Typography.Text>
        </div>

        <Segmented
          options={[
            { label: 'Stacked View', value: 'stacked' },
            { label: 'Split View', value: 'split' },
          ]}
          value={layoutMode}
          onChange={(value) => setLayoutMode(value as 'stacked' | 'split')}
        />

        {layoutMode === 'split' ? (
          <Row gutter={[16, 16]}>
            <Col xs={24} xl={12}>
              <Card size="small" title="Input">
                {inputPane}
              </Card>
            </Col>
            <Col xs={24} xl={12}>
              <Card size="small" title="Result & History">
                {outputPane}
              </Card>
            </Col>
          </Row>
        ) : (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            {inputPane}
            {outputPane}
          </Space>
        )}
      </Space>
    </Card>
  );
}
