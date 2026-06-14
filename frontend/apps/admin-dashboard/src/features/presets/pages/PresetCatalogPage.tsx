import {
  CopyOutlined,
  DeleteOutlined,
  DownloadOutlined,
  PlayCircleOutlined,
  PushpinOutlined,
  StarOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { Alert, Button, Card, Input, Popconfirm, Select, Space, Table, Tabs, Tag, Tooltip, Typography, Upload, message } from 'antd';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { apiClient } from '@/shared/api';
import { JsonEditor, TableSkeleton, TableEmptyState } from '@/shared/ui';
import { useNotification, useUrlState } from '@/shared/hooks';
import { setLaunchPreset, type CatalogPresetItem } from '@/features/presets/utils';

type CatalogMeta = {
  pinned: boolean;
  favorite: boolean;
  tags: string[];
  group?: string;
};

type PresetCatalogSnapshot = {
  generatedAt: string;
  meta: Record<string, CatalogMeta>;
  items: CatalogPresetItem[];
};

const DEFAULT_META: CatalogMeta = {
  pinned: false,
  favorite: false,
  tags: [],
};

export function PresetCatalogPage() {
  const navigate = useNavigate();
  const { activeStoreId } = useActiveStore();
  const notification = useNotification();
  const { getState: getFilterState, setState: setFilterState } = useUrlState({
    defaultValues: {
      search: '',
      source: 'all',
    },
  });

  const search = getFilterState('search', '');
  const sourceFilter = (getFilterState('source', 'all') as 'all' | 'assertions' | 'test-console') || 'all';
  const [selectedId, setSelectedId] = useState('');
  const [tagInput, setTagInput] = useState('');
  const [groupInput, setGroupInput] = useState('');
  const [activeView, setActiveView] = useState<'list' | 'grouped'>('list');
  const [tagFilters, setTagFilters] = useState<string[]>([]);

  const presetsQuery = useQuery({
    queryKey: ['catalog-presets', activeStoreId],
    queryFn: async () => {
      const rows = await apiClient.listPresets({ storeId: activeStoreId || undefined });
      return rows.map<CatalogPresetItem>((row) => ({
        id: `${row.source}:${row.storeId}:${row.scope}:${row.name}`,
        source: row.source,
        storeId: row.storeId,
        scope: row.scope,
        name: row.name,
        payload: row.payload,
        updatedAt: row.updatedAt,
      }));
    },
  });

  const metaQuery = useQuery({
    queryKey: ['catalog-meta'],
    queryFn: () => apiClient.getPresetMeta(),
  });

  const saveMetaMutation = useMutation({
    mutationFn: (meta: Record<string, CatalogMeta>) => apiClient.setPresetMeta(meta),
    onSuccess: () => {
      metaQuery.refetch();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (item: CatalogPresetItem) =>
      apiClient.deletePreset({
        source: item.source,
        storeId: item.storeId,
        scope: item.scope,
        name: item.name,
      }),
    onSuccess: () => {
      presetsQuery.refetch();
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to delete preset');
    },
  });

  const meta = useMemo(() => (metaQuery.data ?? {}) as Record<string, CatalogMeta>, [metaQuery.data]);

  const availableTags = useMemo(() => {
    const tags = new Set<string>();
    for (const item of presetsQuery.data ?? []) {
      for (const tag of meta[item.id]?.tags ?? []) {
        tags.add(tag);
      }
    }

    return Array.from(tags).sort((a, b) => a.localeCompare(b));
  }, [meta, presetsQuery.data]);

  const items = useMemo(() => {
    const source = presetsQuery.data ?? [];

    const filtered = source.filter((item) => {
      if (sourceFilter !== 'all' && item.source !== sourceFilter) {
        return false;
      }

      if (!search.trim()) {
        return true;
      }

      const tags = (meta[item.id]?.tags ?? []).join(' ');
      const group = meta[item.id]?.group ?? '';
      const text = `${item.name} ${item.storeId} ${item.scope} ${item.source} ${tags} ${group}`.toLowerCase();
      const matchesSearch = text.includes(search.trim().toLowerCase());
      if (!matchesSearch) {
        return false;
      }

      if (tagFilters.length === 0) {
        return true;
      }

      const itemTags = meta[item.id]?.tags ?? [];
      return tagFilters.some((tag) => itemTags.includes(tag));
    });

    filtered.sort((a, b) => {
      const left = { ...DEFAULT_META, ...(meta[a.id] ?? {}) };
      const right = { ...DEFAULT_META, ...(meta[b.id] ?? {}) };

      if (left.pinned !== right.pinned) {
        return left.pinned ? -1 : 1;
      }

      if (left.favorite !== right.favorite) {
        return left.favorite ? -1 : 1;
      }

      return b.updatedAt.localeCompare(a.updatedAt);
    });

    return filtered;
  }, [search, sourceFilter, presetsQuery.data, meta, tagFilters]);

  const groupedItems = useMemo(() => {
    const groups = new Map<string, CatalogPresetItem[]>();

    for (const item of items) {
      const key = `${item.source} / ${item.scope}`;
      const existing = groups.get(key) ?? [];
      existing.push(item);
      groups.set(key, existing);
    }

    return Array.from(groups.entries())
      .map(([groupKey, groupRows]) => ({ groupKey, groupRows }))
      .sort((a, b) => a.groupKey.localeCompare(b.groupKey));
  }, [items]);

  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedMeta: CatalogMeta = selected ? { ...DEFAULT_META, ...(meta[selected.id] ?? {}) } : DEFAULT_META;

  const handleSelectPreset = (id: string) => {
    setSelectedId(id);
    const item = items.find((entry) => entry.id === id);
    if (!item) {
      setTagInput('');
      setGroupInput('');
      return;
    }

    const itemMeta = { ...DEFAULT_META, ...(meta[item.id] ?? {}) };
    setTagInput((itemMeta.tags ?? []).join(', '));
    setGroupInput(itemMeta.group ?? '');
  };

  const copyPayload = async (item: CatalogPresetItem) => {
    await navigator.clipboard.writeText(item.payload);
    message.success('Preset payload copied.');
  };

  const exportSnapshot = () => {
    const snapshot: PresetCatalogSnapshot = {
      generatedAt: new Date().toISOString(),
      meta,
      items: presetsQuery.data ?? [],
    };

    const blob = new Blob([JSON.stringify(snapshot, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `aegis-preset-catalog-${new Date().toISOString().slice(0, 19)}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const runPreset = (item: CatalogPresetItem) => {
    setLaunchPreset({ source: item.source, item });
    navigate(item.source === 'assertions' ? '/assertions' : '/test-console');
  };

  const saveMeta = async () => {
    if (!selected) {
      return;
    }

    const tags = tagInput
      .split(',')
      .map((x) => x.trim())
      .filter(Boolean);

    const next = {
      ...meta,
      [selected.id]: {
        ...(meta[selected.id] ?? DEFAULT_META),
        tags,
        group: groupInput.trim() || undefined,
      },
    };

    try {
      await saveMetaMutation.mutateAsync(next);
      message.success('Preset metadata saved.');
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Failed to save preset metadata.');
    }
  };

  const presetColumns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      width: 260,
      render: (value: string) => (
        <Tooltip title={value}>
          <Typography.Text ellipsis style={{ maxWidth: 240 }}>
            {value}
          </Typography.Text>
        </Tooltip>
      ),
    },
    {
      title: 'Source',
      dataIndex: 'source',
      key: 'source',
      render: (value: string) => <Tag>{value}</Tag>,
    },
    {
      title: 'Store',
      dataIndex: 'storeId',
      key: 'storeId',
      width: 260,
      render: (value: string) => (
        <Tooltip title={value}>
          <Typography.Text ellipsis style={{ maxWidth: 240 }}>
            {value}
          </Typography.Text>
        </Tooltip>
      ),
    },
    {
      title: 'Scope',
      dataIndex: 'scope',
      key: 'scope',
      width: 220,
      render: (value: string) => (
        <Tooltip title={value}>
          <Typography.Text ellipsis style={{ maxWidth: 200 }}>
            {value}
          </Typography.Text>
        </Tooltip>
      ),
    },
    {
      title: 'Updated',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      render: (value: string) => new Date(value).toLocaleString('en-US'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, row: CatalogPresetItem) => {
        const rowMeta = { ...DEFAULT_META, ...(meta[row.id] ?? {}) };
        return (
          <Space>
            <Button
              size="small"
              type={rowMeta.favorite ? 'primary' : 'default'}
              icon={<StarOutlined />}
              onClick={async (e) => {
                e.stopPropagation();
                try {
                  await saveMetaMutation.mutateAsync({
                    ...meta,
                    [row.id]: {
                      ...(meta[row.id] ?? DEFAULT_META),
                      favorite: !(meta[row.id]?.favorite ?? false),
                    },
                  });
                } catch (error) {
                  message.error(error instanceof Error ? error.message : 'Failed to update favorite.');
                }
              }}
            >
              Favorite
            </Button>
            <Button
              size="small"
              type={rowMeta.pinned ? 'primary' : 'default'}
              icon={<PushpinOutlined />}
              onClick={async (e) => {
                e.stopPropagation();
                try {
                  await saveMetaMutation.mutateAsync({
                    ...meta,
                    [row.id]: {
                      ...(meta[row.id] ?? DEFAULT_META),
                      pinned: !(meta[row.id]?.pinned ?? false),
                    },
                  });
                } catch (error) {
                  message.error(error instanceof Error ? error.message : 'Failed to update pin.');
                }
              }}
            >
              Pin
            </Button>
            <Button
              size="small"
              icon={<PlayCircleOutlined />}
              onClick={(e) => {
                e.stopPropagation();
                runPreset(row);
              }}
            >
              Run
            </Button>
            <Button
              size="small"
              icon={<CopyOutlined />}
              onClick={async (e) => {
                e.stopPropagation();
                await copyPayload(row);
              }}
            >
              Copy
            </Button>
            <Popconfirm
              title="Delete Preset?"
              description={`This will permanently delete preset "${row.name}".`}
              okText="Delete"
              cancelText="Cancel"
              okButtonProps={{ danger: true, loading: deleteMutation.isPending }}
              onConfirm={() => {
                deleteMutation.mutate(row, {
                  onSuccess: () => {
                    if (selectedId === row.id) {
                      setSelectedId('');
                      setTagInput('');
                      setGroupInput('');
                    }
                  },
                });
              }}
            >
              <Button
                size="small"
                danger
                icon={<DeleteOutlined />}
                onClick={(e) => e.stopPropagation()}
              >
                Delete
              </Button>
            </Popconfirm>
          </Space>
        );
      },
    },
  ];

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            Preset Catalog
          </Typography.Title>
          <Typography.Text type="secondary">
            Browse and organize saved presets for Assertions and Test Console.
          </Typography.Text>
        </div>

        {!activeStoreId ? (
          <Alert type="info" showIcon message="Select an active store to focus catalog items for that store." />
        ) : null}

        <Space wrap>
          <Input
            style={{ width: 280 }}
            value={search}
            placeholder="search by name/store/scope"
            onChange={(e) => setFilterState('search', e.target.value)}
          />
          <Select
            style={{ width: 220 }}
            value={sourceFilter}
            options={[
              { value: 'all', label: 'All Sources' },
              { value: 'assertions', label: 'Assertions' },
              { value: 'test-console', label: 'Test Console' },
            ]}
            onChange={(value) => setFilterState('source', value)}
          />
          <Select
            mode="multiple"
            allowClear
            style={{ width: 320 }}
            value={tagFilters}
            placeholder="quick filter tags"
            options={availableTags.map((tag) => ({ value: tag, label: tag }))}
            onChange={(values) => setTagFilters(values)}
          />
          <Button
            onClick={() => {
              presetsQuery.refetch();
              metaQuery.refetch();
            }}
          >
            Refresh
          </Button>
          <Button icon={<DownloadOutlined />} onClick={exportSnapshot}>
            Export Catalog
          </Button>
          <Upload
            accept="application/json"
            showUploadList={false}
            beforeUpload={async (file) => {
              try {
                const text = await file.text();
                const payload = JSON.parse(text) as PresetCatalogSnapshot;
                for (const item of payload.items ?? []) {
                  await apiClient.upsertPreset({
                    source: item.source,
                    storeId: item.storeId,
                    scope: item.scope,
                    name: item.name,
                    payload: item.payload,
                  });
                }

                await apiClient.setPresetMeta(payload.meta ?? {});
                await presetsQuery.refetch();
                await metaQuery.refetch();
                message.success('Catalog imported.');
              } catch (error) {
                message.error(error instanceof Error ? error.message : 'Failed to import catalog.');
              }

              return false;
            }}
          >
            <Button icon={<UploadOutlined />}>Import Catalog</Button>
          </Upload>
        </Space>

        {presetsQuery.isLoading ? (
          <TableSkeleton rows={4} columns={6} />
        ) : items.length === 0 ? (
          <TableEmptyState message="No presets found. Create presets in Assertions or Test Console to see them here." />
        ) : (
          <Tabs
            activeKey={activeView}
            onChange={(key) => setActiveView(key as 'list' | 'grouped')}
            items={[
              {
                key: 'list',
                label: 'List View',
                children: (
                  <Table
                    rowKey="id"
                    dataSource={items}
                    pagination={{ pageSize: 10, showSizeChanger: true }}
                    scroll={{ x: 'max-content' }}
                    onRow={(record) => ({
                      onClick: () => handleSelectPreset(record.id),
                    })}
                    columns={presetColumns}
                  />
                ),
              },
              {
                key: 'grouped',
                label: 'Grouped View',
                children: (
                  <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                    {groupedItems.map((group) => (
                      <Card
                        key={group.groupKey}
                        size="small"
                        title={`${group.groupKey} (${group.groupRows.length})`}
                      >
                        <Table
                          rowKey="id"
                          dataSource={group.groupRows}
                          pagination={false}
                          scroll={{ x: 'max-content' }}
                          onRow={(record) => ({
                            onClick: () => handleSelectPreset(record.id),
                          })}
                          columns={presetColumns}
                        />
                      </Card>
                    ))}
                  </Space>
                ),
              },
            ]}
          />
        )}

        {selected ? (
          <Space direction="vertical" style={{ width: '100%' }}>
            <Typography.Text strong>Preview: {selected.name}</Typography.Text>
            <Space wrap>
              <Input
                style={{ width: 320 }}
                placeholder="group (e.g. Smoke, Regression)"
                value={groupInput}
                onChange={(e) => setGroupInput(e.target.value)}
              />
              <Input
                style={{ width: 380 }}
                placeholder="tags comma separated (abac,negative,hotfix)"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
              />
              <Button onClick={saveMeta}>Save Metadata</Button>
              <Button type="primary" icon={<PlayCircleOutlined />} onClick={() => runPreset(selected)}>
                Run Preset
              </Button>
            </Space>
            <Space wrap>
              <Typography.Text type="secondary">
                Group: {selectedMeta.group || '-'}
              </Typography.Text>
              <Typography.Text type="secondary">Tags:</Typography.Text>
              {(selectedMeta.tags ?? []).length > 0 ? (selectedMeta.tags ?? []).map((tag) => <Tag key={tag}>{tag}</Tag>) : <Tag>none</Tag>}
            </Space>
            <JsonEditor
              value={selected.payload}
              onChange={() => undefined}
              height={280}
              readOnly
              path={`inmemory://model/catalog-${selected.id}.json`}
            />
          </Space>
        ) : (
          <Typography.Text type="secondary">Select a preset row to preview payload.</Typography.Text>
        )}
      </Space>
    </Card>
  );
}



