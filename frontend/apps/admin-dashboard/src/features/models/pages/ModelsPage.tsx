import { useState } from 'react';
import Editor from '@monaco-editor/react';
import { useMemo } from 'react';
import { Alert, Button, Col, Drawer, Dropdown, Input, Modal, Row, Select, Space, Statistic, Table, Tag, Tooltip, Typography, message } from 'antd';
import { MoreOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { AuthorizationModelDiff, AuthorizationModelValidationResult } from '@aegis/types/src/model';
import { useAuth } from '@/app/providers/useAuth';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { AccessGate, JsonEditor } from '@/shared/ui';
import { APP_CODE_FONT_FAMILY } from '@/shared/utils/fonts';

const MODEL_TEMPLATES: Record<string, string> = {
  'document-viewer': `type user

type document
  relations
    define viewer: [user]`,
  'document-editor': `type user

type document
  relations
    define viewer: [user]
    define editor: [user]
    define owner: [user]
    define can_edit: editor or owner`,
  'org-repo': `type user

type org
  relations
    define admin: [user]
    define member: [user]

type repo
  relations
    define parent: [org]
    define reader: [user, org#member]
    define writer: [user, org#admin]`,
  'folder-inheritance': `type user

type folder
  relations
    define viewer: [user]
    define editor: [user]

type document
  relations
    define parent: [folder]
    define viewer: [user] or viewer from parent
    define editor: [user] or editor from parent`,
  'approval-gate': `type user

type document
  relations
    define viewer: [user]
    define allowed: [user]
    define blocked: [user]
    define can_view: viewer and allowed but not blocked`,
};

export function ModelsPage() {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();
  const { activeStoreId } = useActiveStore();
  const [schemaVersion, setSchemaVersion] = useState('1.1');
  const [modelDsl, setModelDsl] = useState(MODEL_TEMPLATES['document-viewer']);
  const [selectedTemplate, setSelectedTemplate] = useState('document-viewer');
  const [dslDrawerOpen, setDslDrawerOpen] = useState(false);
  const [dslDrawerValue, setDslDrawerValue] = useState('');
  const [editDrawerOpen, setEditDrawerOpen] = useState(false);
  const [editingModelId, setEditingModelId] = useState('');
  const [editSchemaVersion, setEditSchemaVersion] = useState('1.1');
  const [editModelDsl, setEditModelDsl] = useState('');
  const [validationResult, setValidationResult] = useState<AuthorizationModelValidationResult | null>(null);
  const [diffDrawerOpen, setDiffDrawerOpen] = useState(false);
  const [diffResult, setDiffResult] = useState<AuthorizationModelDiff | null>(null);
  const [leftDiffModelId, setLeftDiffModelId] = useState('');
  const [rightDiffModelId, setRightDiffModelId] = useState('');

  const modelsQuery = useQuery({
    queryKey: ['models', activeStoreId],
    queryFn: () => apiClient.listAuthorizationModels(activeStoreId),
    enabled: isAuthenticated && Boolean(activeStoreId),
  });

  const createModelMutation = useMutation({
    mutationFn: () =>
      apiClient.createAuthorizationModel(activeStoreId, {
        schemaVersion,
        model: modelDsl,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['models', activeStoreId] });
    },
  });

  const validateModelMutation = useMutation({
    mutationFn: () =>
      apiClient.validateAuthorizationModel(activeStoreId, {
        schemaVersion,
        model: modelDsl,
      }),
    onSuccess: (result) => {
      setValidationResult(result);
      if (result.valid) {
        message.success('Model validation passed');
      } else {
        message.warning('Model validation found errors');
      }
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to validate model');
    },
  });

  const updateModelMutation = useMutation({
    mutationFn: () =>
      apiClient.updateAuthorizationModel(activeStoreId, editingModelId, {
        schemaVersion: editSchemaVersion,
        model: editModelDsl,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['models', activeStoreId] });
      setEditDrawerOpen(false);
      setEditingModelId('');
      message.success('Model updated successfully');
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to update model');
    },
  });

  const deleteModelMutation = useMutation({
    mutationFn: (authorizationModelId: string) => apiClient.deleteAuthorizationModel(activeStoreId, authorizationModelId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['models', activeStoreId] });
      message.success('Model deleted successfully');
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to delete model');
    },
  });

  const publishModelMutation = useMutation({
    mutationFn: (authorizationModelId: string) => apiClient.publishAuthorizationModel(activeStoreId, authorizationModelId),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['models', activeStoreId] });
      message.success(`Published model ${formatShortId(result.activeModelId)}`);
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to publish model');
    },
  });

  const rollbackModelMutation = useMutation({
    mutationFn: (authorizationModelId: string) => apiClient.rollbackAuthorizationModel(activeStoreId, authorizationModelId),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['models', activeStoreId] });
      message.success(`Active model rolled back to ${formatShortId(result.activeModelId)}`);
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to roll back model');
    },
  });

  const diffModelMutation = useMutation({
    mutationFn: () => apiClient.diffAuthorizationModels(activeStoreId, leftDiffModelId, rightDiffModelId),
    onSuccess: (result) => {
      setDiffResult(result);
      setDiffDrawerOpen(true);
    },
    onError: (error: unknown) => {
      message.error(error instanceof Error ? error.message : 'Failed to diff models');
    },
  });

  const modelDiagnostics = useMemo(() => {
    const types = [...modelDsl.matchAll(/^\s*type\s+([A-Za-z0-9_]+)/gm)].map((match) => match[1]);
    const relations = [...modelDsl.matchAll(/^\s*define\s+([A-Za-z0-9_]+)/gm)].map((match) => match[1]);
    const directRelations = [...modelDsl.matchAll(/^\s*define\s+[A-Za-z0-9_]+:\s*\[/gm)].length;
    const hasUnion = /\sor\s/.test(modelDsl);
    const hasIntersection = /\sand\s/.test(modelDsl);
    const hasExclusion = /\sbut not\s/.test(modelDsl);
    const hasInheritance = /\sfrom\s+[A-Za-z0-9_]+/.test(modelDsl);
    const warnings = [
      types.length === 0 ? 'Add at least one type definition.' : '',
      relations.length === 0 ? 'Add relations before publishing the model.' : '',
      directRelations === 0 ? 'At least one direct assignable relation is recommended for tuple writes.' : '',
    ].filter(Boolean);

    return {
      typeCount: new Set(types).size,
      relationCount: relations.length,
      directRelations,
      hasUnion,
      hasIntersection,
      hasExclusion,
      hasInheritance,
      warnings,
    };
  }, [modelDsl]);

  if (!isAuthenticated) {
    return <AccessGate title="Authorization Models" message="Login first to manage models." />;
  }

  if (!activeStoreId) {
    return <AccessGate title="Authorization Models" message="Set an active store first." />;
  }

  const getDslTypes = (value: string) => {
    const typeMatches = [...value.matchAll(/^\s*type\s+([A-Za-z0-9_]+)/gm)].map((match) => match[1]);
    return Array.from(new Set(typeMatches));
  };

  const getDslSummary = (value: string) => {
    const typeMatches = [...value.matchAll(/^\s*type\s+([A-Za-z0-9_]+)/gm)].map((match) => match[1]);
    const uniqueTypes = Array.from(new Set(typeMatches));
    const defineCount = [...value.matchAll(/^\s*define\s+/gm)].length;
    const typePreview = uniqueTypes.slice(0, 3).join(', ');
    const typeSuffix = uniqueTypes.length > 3 ? ', ...' : '';

    return `Types: ${typePreview || 'n/a'}${typeSuffix} (${uniqueTypes.length}) • Relations: ${defineCount}`;
  };

  const formatShortId = (value: string) => {
    if (value.length <= 18) {
      return value;
    }

    return `${value.slice(0, 8)}...${value.slice(-6)}`;
  };

  const modelOptions = (modelsQuery.data ?? []).map((model) => ({
    value: model.id,
    label: `${formatShortId(model.id)} - ${model.state}`,
  }));

  const stateColor = (state: string) => {
    switch (state) {
      case 'Published':
        return 'green';
      case 'Validated':
        return 'blue';
      case 'Archived':
        return 'default';
      case 'Deprecated':
        return 'orange';
      default:
        return 'gold';
    }
  };

  return (
    <div className="page-surface">
      <section className="page-section">
        <div className="page-toolbar">
          <div className="page-toolbar-main">
            <Input
              style={{ width: 140 }}
              value={schemaVersion}
              onChange={(e) => setSchemaVersion(e.target.value)}
              placeholder="schema version"
              addonBefore="Schema"
            />
            <Select
              style={{ minWidth: 260 }}
              value={selectedTemplate}
              options={[
                { value: 'document-viewer', label: 'Template: Document Viewer' },
                { value: 'document-editor', label: 'Template: Document Editor' },
                { value: 'org-repo', label: 'Template: Org + Repo' },
                { value: 'folder-inheritance', label: 'Template: Folder Inheritance' },
                { value: 'approval-gate', label: 'Template: Approval Gate' },
              ]}
              onChange={(value) => {
                setSelectedTemplate(value);
                setModelDsl(MODEL_TEMPLATES[value]);
                setValidationResult(null);
              }}
            />
          </div>
          <div className="page-toolbar-actions">
            <Button
              onClick={() => validateModelMutation.mutate()}
              loading={validateModelMutation.isPending}
              disabled={!schemaVersion.trim() || !modelDsl.trim()}
            >
              Validate
            </Button>
            <Button
              type="primary"
              onClick={() => createModelMutation.mutate()}
              loading={createModelMutation.isPending}
              disabled={!schemaVersion.trim() || !modelDsl.trim() || validationResult?.valid === false}
            >
              Create Version
            </Button>
            <Dropdown
              trigger={['click']}
              menu={{
                items: [
                  { key: 'copy', label: 'Copy DSL' },
                ],
                onClick: async ({ key }) => {
                  if (key === 'copy') {
                    await navigator.clipboard.writeText(modelDsl);
                    message.success('Model DSL copied.');
                  }
                },
              }}
            >
              <Button icon={<MoreOutlined />} />
            </Dropdown>
          </div>
        </div>

        <div className="two-pane">
          <div className="json-editor-wrap">
            <Editor
              path={`inmemory://model/openfga-model-${activeStoreId || 'draft'}.fga`}
              height={360}
              defaultLanguage="yaml"
              theme="vs"
              value={modelDsl}
              onChange={(next) => {
                setModelDsl(next ?? '');
                setValidationResult(null);
              }}
              options={{
                minimap: { enabled: false },
                wordWrap: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                lineNumbers: 'on',
                lineNumbersMinChars: 4,
                renderLineHighlight: 'line',
                fontSize: 13,
                fontFamily: APP_CODE_FONT_FAMILY,
                lineHeight: 20,
                tabSize: 2,
                formatOnPaste: false,
                formatOnType: false,
              }}
            />
          </div>
          <div className="page-section page-section-soft">
            <Typography.Text className="section-label">Draft Summary</Typography.Text>
            <div className="metrics-strip" style={{ gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' }}>
              <div className="metric-tile"><Statistic title="Types" value={modelDiagnostics.typeCount} /></div>
              <div className="metric-tile"><Statistic title="Relations" value={modelDiagnostics.relationCount} /></div>
              <div className="metric-tile"><Statistic title="Direct writes" value={modelDiagnostics.directRelations} /></div>
              <div className="metric-tile">
                <Space wrap size={[6, 6]}>
                  {modelDiagnostics.hasUnion ? <Tag color="processing">union</Tag> : null}
                  {modelDiagnostics.hasIntersection ? <Tag color="processing">intersection</Tag> : null}
                  {modelDiagnostics.hasExclusion ? <Tag color="processing">exclusion</Tag> : null}
                  {modelDiagnostics.hasInheritance ? <Tag color="processing">inheritance</Tag> : null}
                  {!modelDiagnostics.hasUnion
                    && !modelDiagnostics.hasIntersection
                    && !modelDiagnostics.hasExclusion
                    && !modelDiagnostics.hasInheritance ? <Tag>direct</Tag> : null}
                </Space>
              </div>
            </div>
            {modelDiagnostics.warnings.length > 0 ? (
              <Alert type="warning" showIcon message="Needs review" description={modelDiagnostics.warnings.join(' ')} />
            ) : (
              <Alert type="success" showIcon message="Ready to validate" />
            )}
          </div>
        </div>

        <details className="secondary-details">
          <summary>Compare model versions</summary>
          <div className="secondary-details-body">
            <div className="form-row">
              <Select
                showSearch
                placeholder="Base model"
                style={{ minWidth: 240 }}
                value={leftDiffModelId || undefined}
                options={modelOptions}
                onChange={setLeftDiffModelId}
              />
              <Select
                showSearch
                placeholder="Candidate model"
                style={{ minWidth: 240 }}
                value={rightDiffModelId || undefined}
                options={modelOptions}
                onChange={setRightDiffModelId}
              />
              <Button
                disabled={!leftDiffModelId || !rightDiffModelId || leftDiffModelId === rightDiffModelId}
                loading={diffModelMutation.isPending}
                onClick={() => diffModelMutation.mutate()}
              >
                View Diff
              </Button>
            </div>
          </div>
        </details>

        {validationResult ? (
          <Alert
            type={validationResult.valid ? 'success' : 'error'}
            showIcon
            message={validationResult.valid ? 'Backend validation passed' : 'Backend validation failed'}
            description={(
              <Space direction="vertical" size={4}>
                {validationResult.errors.map((issue) => (
                  <Typography.Text key={`${issue.code}-${issue.line ?? 'root'}`} type="danger">
                    {issue.line ? `Line ${issue.line}: ` : ''}{issue.code} - {issue.message}
                  </Typography.Text>
                ))}
                {validationResult.warnings.map((issue) => (
                  <Typography.Text key={`${issue.code}-${issue.line ?? 'root'}`} type="secondary">
                    {issue.line ? `Line ${issue.line}: ` : ''}{issue.code} - {issue.message}
                  </Typography.Text>
                ))}
                {validationResult.errors.length === 0 && validationResult.warnings.length === 0 ? (
                  <Typography.Text type="secondary">No errors or warnings.</Typography.Text>
                ) : null}
              </Space>
            )}
          />
        ) : null}
      </section>

      <section className="page-section">
        <div className="page-toolbar">
          <div>
            <Typography.Text className="section-label">Model Versions</Typography.Text>
            <Typography.Paragraph type="secondary" style={{ margin: 0 }}>
              Publish, roll back, or inspect versions for the active store.
            </Typography.Paragraph>
          </div>
        </div>

        <Table
          rowKey="id"
          loading={modelsQuery.isLoading}
          dataSource={modelsQuery.data ?? []}
          pagination={{ pageSize: 10, showSizeChanger: true }}
          scroll={{ x: 'max-content' }}
          columns={[
            {
              title: 'ID',
              dataIndex: 'id',
              key: 'id',
              width: 220,
              render: (value: string) => (
                <Tooltip title={value}>
                  <Typography.Text code ellipsis style={{ maxWidth: 200 }}>
                    {formatShortId(value)}
                  </Typography.Text>
                </Tooltip>
              ),
            },
            {
              title: 'Schema',
              dataIndex: 'schemaVersion',
              key: 'schemaVersion',
            },
            {
              title: 'State',
              dataIndex: 'state',
              key: 'state',
              render: (value: string, row) => (
                <Space direction="vertical" size={2}>
                  <Tag color={stateColor(value)}>{value}</Tag>
                  {row.supersededBy ? (
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      by {formatShortId(row.supersededBy)}
                    </Typography.Text>
                  ) : null}
                </Space>
              ),
            },
            {
              title: 'Created',
              dataIndex: 'createdAt',
              key: 'createdAt',
              render: (value: string) => new Date(value).toLocaleString('en-US'),
            },
            {
              title: 'Model DSL',
              dataIndex: 'model',
              key: 'model',
              width: 420,
              render: (value: string, row) => (
                <div
                  role="button"
                  tabIndex={0}
                  onClick={() => {
                    setDslDrawerValue(value);
                    setDslDrawerOpen(true);
                  }}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      setDslDrawerValue(value);
                      setDslDrawerOpen(true);
                    }
                  }}
                  style={{
                    border: '1px solid #f0f0f0',
                    borderRadius: 8,
                    padding: 8,
                    cursor: 'pointer',
                    width: '100%',
                  }}
                >
                  <Space direction="vertical" size={6} style={{ width: '100%' }}>
                    <Typography.Text type="secondary">{getDslSummary(value)}</Typography.Text>
                    <Space wrap size={[6, 6]}>
                      {getDslTypes(value).slice(0, 6).map((typeName) => (
                        <Tag key={typeName} color="blue">
                          {typeName}
                        </Tag>
                      ))}
                      {getDslTypes(value).length > 6 ? <Tag>+{getDslTypes(value).length - 6}</Tag> : null}
                    </Space>
                    <JsonEditor
                      readOnly
                      language="yaml"
                      value={value}
                      onChange={() => {}}
                      height={132}
                      path={`inmemory://model/model-dsl-preview-${row.id}.fga`}
                    />
                  </Space>
                </div>
              ),
            },
            {
              title: 'Actions',
              key: 'actions',
              render: (_, row) => (
                <Dropdown
                  trigger={['click']}
                  menu={{
                    items: [
                      {
                        key: 'publish',
                        label: 'Publish',
                        disabled: row.state === 'Published' || publishModelMutation.isPending,
                      },
                      {
                        key: 'rollback',
                        label: 'Rollback to this version',
                        disabled: row.state === 'Published' || rollbackModelMutation.isPending,
                      },
                      { key: 'edit', label: 'Edit DSL' },
                      { key: 'delete', label: 'Delete', danger: true, disabled: deleteModelMutation.isPending },
                    ],
                    onClick: ({ key }) => {
                      if (key === 'publish') {
                        publishModelMutation.mutate(row.id);
                      }

                      if (key === 'rollback') {
                        Modal.confirm({
                          title: 'Rollback active model?',
                          content: 'This model will become the active published version.',
                          okText: 'Rollback',
                          onOk: () => rollbackModelMutation.mutate(row.id),
                        });
                      }

                      if (key === 'edit') {
                        setEditingModelId(row.id);
                        setEditSchemaVersion(row.schemaVersion);
                        setEditModelDsl(row.model);
                        setEditDrawerOpen(true);
                      }

                      if (key === 'delete') {
                        Modal.confirm({
                          title: 'Delete model?',
                          content: 'This will permanently delete this authorization model.',
                          okText: 'Delete',
                          okButtonProps: { danger: true },
                          onOk: () => deleteModelMutation.mutate(row.id),
                        });
                      }
                    },
                  }}
                >
                  <Button size="small" icon={<MoreOutlined />} />
                </Dropdown>
              ),
            },
          ]}
        />
      </section>

        <Drawer
          title="Model Diff"
          width={720}
          open={diffDrawerOpen}
          onClose={() => setDiffDrawerOpen(false)}
          destroyOnClose
        >
          {diffResult ? (
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
              {diffResult.breakingChangeHints.length > 0 ? (
                <Alert
                  type="warning"
                  showIcon
                  message="Risk hints"
                  description={diffResult.breakingChangeHints.join(' ')}
                />
              ) : (
                <Alert type="success" showIcon message="No breaking-change hints detected" />
              )}
              <Row gutter={[12, 12]}>
                <Col span={8}><Statistic title="Added types" value={diffResult.addedTypes.length} /></Col>
                <Col span={8}><Statistic title="Removed types" value={diffResult.removedTypes.length} /></Col>
                <Col span={8}><Statistic title="Changed relations" value={diffResult.changedRelations.length} /></Col>
              </Row>
              <Typography.Text strong>Types</Typography.Text>
              <Space wrap>
                {diffResult.addedTypes.map((type) => <Tag key={`added-${type}`} color="green">+ {type}</Tag>)}
                {diffResult.removedTypes.map((type) => <Tag key={`removed-${type}`} color="red">- {type}</Tag>)}
                {diffResult.changedTypes.map((type) => <Tag key={`changed-${type}`} color="orange">~ {type}</Tag>)}
                {diffResult.addedTypes.length + diffResult.removedTypes.length + diffResult.changedTypes.length === 0 ? <Tag>No type changes</Tag> : null}
              </Space>
              <Typography.Text strong>Relations</Typography.Text>
              <Space direction="vertical" style={{ width: '100%' }}>
                {diffResult.addedRelations.map((relation) => (
                  <Alert key={`added-${relation.type}-${relation.relation}`} type="success" message={`+ ${relation.type}#${relation.relation}`} description={relation.expression} />
                ))}
                {diffResult.removedRelations.map((relation) => (
                  <Alert key={`removed-${relation.type}-${relation.relation}`} type="error" message={`- ${relation.type}#${relation.relation}`} description={relation.expression} />
                ))}
                {diffResult.changedRelations.map((relation) => (
                  <Alert
                    key={`changed-${relation.type}-${relation.relation}`}
                    type="warning"
                    message={`~ ${relation.type}#${relation.relation}`}
                    description={`${relation.leftExpression} -> ${relation.rightExpression}`}
                  />
                ))}
              </Space>
            </Space>
          ) : null}
        </Drawer>

        <Drawer
          title="Model DSL Preview"
          width={780}
          open={dslDrawerOpen}
          onClose={() => setDslDrawerOpen(false)}
          destroyOnClose
        >
          <Editor
            path={`inmemory://model/openfga-model-preview-${activeStoreId || 'draft'}.fga`}
            height={520}
            defaultLanguage="yaml"
            theme="vs"
            value={dslDrawerValue}
            options={{
              readOnly: true,
              minimap: { enabled: false },
              wordWrap: 'on',
              scrollBeyondLastLine: false,
              automaticLayout: true,
              lineNumbers: 'on',
              lineNumbersMinChars: 4,
              renderLineHighlight: 'line',
              fontSize: 13,
              fontFamily: APP_CODE_FONT_FAMILY,
              lineHeight: 20,
            }}
          />
        </Drawer>

        <Drawer
          title="Edit Authorization Model"
          width={860}
          open={editDrawerOpen}
          onClose={() => setEditDrawerOpen(false)}
          destroyOnClose
          extra={(
            <Button
              type="primary"
              loading={updateModelMutation.isPending}
              disabled={!editingModelId || !editSchemaVersion.trim() || !editModelDsl.trim()}
              onClick={() => updateModelMutation.mutate()}
            >
              Save Changes
            </Button>
          )}
        >
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Input
              style={{ width: 220 }}
              value={editSchemaVersion}
              onChange={(e) => setEditSchemaVersion(e.target.value)}
              placeholder="schema version"
            />
            <Editor
              path={`inmemory://model/openfga-model-edit-${editingModelId || 'draft'}.fga`}
              height={520}
              defaultLanguage="yaml"
              theme="vs"
              value={editModelDsl}
              onChange={(next) => setEditModelDsl(next ?? '')}
              options={{
                minimap: { enabled: false },
                wordWrap: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                lineNumbers: 'on',
                lineNumbersMinChars: 4,
                renderLineHighlight: 'line',
                fontSize: 13,
                fontFamily: APP_CODE_FONT_FAMILY,
                lineHeight: 20,
              }}
            />
          </Space>
        </Drawer>

        {modelsQuery.error ? <Typography.Text type="danger">{(modelsQuery.error as Error).message}</Typography.Text> : null}
        {createModelMutation.error ? (
          <Typography.Text type="danger">{(createModelMutation.error as Error).message}</Typography.Text>
        ) : null}
    </div>
  );
}
