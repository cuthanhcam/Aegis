import { useState } from 'react';
import Editor from '@monaco-editor/react';
import { useMemo } from 'react';
import { Alert, Button, Card, Col, Drawer, Input, Popconfirm, Row, Select, Space, Statistic, Table, Tag, Tooltip, Typography, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { AuthorizationModelValidationResult } from '@aegis/types/src/model';
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

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Authorization Models
          </Typography.Title>
          <Typography.Text type="secondary">Build authorization models for store: {activeStoreId}</Typography.Text>
        </div>

        <Space align="center" wrap>
          <Typography.Text strong>Schema Version</Typography.Text>
          <Input
            style={{ width: 220 }}
            value={schemaVersion}
            onChange={(e) => setSchemaVersion(e.target.value)}
            placeholder="schema version"
          />
        </Space>

        <Space wrap>
          <Select
            style={{ width: 280 }}
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
          <Button
            onClick={async () => {
              await navigator.clipboard.writeText(modelDsl);
            }}
          >
            Copy DSL
          </Button>
          <Button
            onClick={() => validateModelMutation.mutate()}
            loading={validateModelMutation.isPending}
            disabled={!schemaVersion.trim() || !modelDsl.trim()}
          >
            Validate Model
          </Button>
        </Space>

        <Row gutter={[12, 12]}>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Types" value={modelDiagnostics.typeCount} />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Relations" value={modelDiagnostics.relationCount} />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Statistic title="Direct writes" value={modelDiagnostics.directRelations} />
            </Card>
          </Col>
          <Col xs={12} lg={6}>
            <Card size="small">
              <Space wrap size={[6, 6]}>
                {modelDiagnostics.hasUnion ? <Tag color="processing">union</Tag> : null}
                {modelDiagnostics.hasIntersection ? <Tag color="processing">intersection</Tag> : null}
                {modelDiagnostics.hasExclusion ? <Tag color="processing">exclusion</Tag> : null}
                {modelDiagnostics.hasInheritance ? <Tag color="processing">inheritance</Tag> : null}
                {!modelDiagnostics.hasUnion
                  && !modelDiagnostics.hasIntersection
                  && !modelDiagnostics.hasExclusion
                  && !modelDiagnostics.hasInheritance ? <Tag>direct only</Tag> : null}
              </Space>
            </Card>
          </Col>
        </Row>

        {modelDiagnostics.warnings.length > 0 ? (
          <Alert
            type="warning"
            showIcon
            message="Model readiness checks"
            description={modelDiagnostics.warnings.join(' ')}
          />
        ) : (
          <Alert
            type="success"
            showIcon
            message="Model has publishable structure"
            description="The DSL includes types, relations, and at least one direct assignable relation for tuple writes."
          />
        )}

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

        <div className="json-editor-wrap">
          <Editor
            path={`inmemory://model/openfga-model-${activeStoreId || 'draft'}.fga`}
            height={300}
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

        <Button
          type="primary"
          onClick={() => createModelMutation.mutate()}
          loading={createModelMutation.isPending}
          disabled={!schemaVersion.trim() || !modelDsl.trim() || validationResult?.valid === false}
        >
          Create Model Version
        </Button>

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
                <Space>
                  <Button
                    size="small"
                    onClick={() => {
                      setEditingModelId(row.id);
                      setEditSchemaVersion(row.schemaVersion);
                      setEditModelDsl(row.model);
                      setEditDrawerOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                  <Popconfirm
                    title="Delete Model?"
                    description="This will permanently delete this authorization model."
                    okText="Delete"
                    cancelText="Cancel"
                    okButtonProps={{ danger: true, loading: deleteModelMutation.isPending }}
                    onConfirm={() => deleteModelMutation.mutate(row.id)}
                  >
                    <Button size="small" danger disabled={deleteModelMutation.isPending}>
                      Delete
                    </Button>
                  </Popconfirm>
                </Space>
              ),
            },
          ]}
        />

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
      </Space>
    </Card>
  );
}
