type CatalogMeta = {
  pinned?: boolean;
  favorite?: boolean;
  tags?: string[];
  group?: string;
};

export type PresetSource = 'assertions' | 'test-console';

export type CatalogPresetItem = {
  id: string;
  source: PresetSource;
  storeId: string;
  scope: string;
  name: string;
  payload: string;
  updatedAt: string;
};

type AssertionPreset = {
  name: string;
  payload: string;
  updatedAt: string;
};

type TestConsolePreset = {
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

const ASSERTIONS_PREFIX = 'aegis:assertions:presets:';
const TEST_CONSOLE_PREFIX = 'aegis:test-console:presets:';
const CATALOG_META_KEY = 'aegis:preset-catalog:meta';

function safeParse<T>(input: string | null): T | null {
  if (!input) {
    return null;
  }

  try {
    return JSON.parse(input) as T;
  } catch {
    return null;
  }
}

function buildAssertionItem(storeId: string, modelId: string, preset: AssertionPreset): CatalogPresetItem {
  return {
    id: `assertions:${storeId}:${modelId}:${preset.name}`,
    source: 'assertions',
    storeId,
    scope: modelId,
    name: preset.name,
    payload: preset.payload,
    updatedAt: preset.updatedAt,
  };
}

function buildConsoleItem(storeId: string, preset: TestConsolePreset): CatalogPresetItem {
  return {
    id: `test-console:${storeId}:global:${preset.name}`,
    source: 'test-console',
    storeId,
    scope: 'global',
    name: preset.name,
    payload: JSON.stringify(
      {
        user: preset.user,
        relation: preset.relation,
        object: preset.object,
        consistency: preset.consistency ?? '',
        authorizationModelId: preset.authorizationModelId ?? '',
        batchSize: preset.batchSize,
        contextualTuplesJson: preset.contextualTuplesJson,
        contextJson: preset.contextJson,
      },
      null,
      2,
    ),
    updatedAt: preset.updatedAt,
  };
}

export function listCatalogPresets(storeFilter?: string): CatalogPresetItem[] {
  const all: CatalogPresetItem[] = [];

  for (let i = 0; i < localStorage.length; i += 1) {
    const key = localStorage.key(i);
    if (!key) {
      continue;
    }

    if (key.startsWith(ASSERTIONS_PREFIX)) {
      const suffix = key.slice(ASSERTIONS_PREFIX.length);
      const firstSeparator = suffix.indexOf(':');
      if (firstSeparator <= 0) {
        continue;
      }

      const storeId = suffix.slice(0, firstSeparator);
      const modelId = suffix.slice(firstSeparator + 1);
      if (!storeId || !modelId) {
        continue;
      }

      if (storeFilter && storeFilter !== storeId) {
        continue;
      }

      const presets = safeParse<AssertionPreset[]>(localStorage.getItem(key)) ?? [];
      for (const preset of presets) {
        all.push(buildAssertionItem(storeId, modelId, preset));
      }
      continue;
    }

    if (key.startsWith(TEST_CONSOLE_PREFIX)) {
      const storeId = key.slice(TEST_CONSOLE_PREFIX.length);
      if (!storeId) {
        continue;
      }

      if (storeFilter && storeFilter !== storeId) {
        continue;
      }

      const presets = safeParse<TestConsolePreset[]>(localStorage.getItem(key)) ?? [];
      for (const preset of presets) {
        all.push(buildConsoleItem(storeId, preset));
      }
    }
  }

  return all;
}

export function deleteCatalogPreset(item: CatalogPresetItem): boolean {
  if (item.source === 'assertions') {
    const key = `${ASSERTIONS_PREFIX}${item.storeId}:${item.scope}`;
    const presets = safeParse<AssertionPreset[]>(localStorage.getItem(key)) ?? [];
    const next = presets.filter((preset) => preset.name !== item.name);
    localStorage.setItem(key, JSON.stringify(next));
    return next.length !== presets.length;
  }

  const key = `${TEST_CONSOLE_PREFIX}${item.storeId}`;
  const presets = safeParse<TestConsolePreset[]>(localStorage.getItem(key)) ?? [];
  const next = presets.filter((preset) => preset.name !== item.name);
  localStorage.setItem(key, JSON.stringify(next));
  return next.length !== presets.length;
}

export function readCatalogMeta(): Record<string, CatalogMeta> {
  return safeParse<Record<string, CatalogMeta>>(localStorage.getItem(CATALOG_META_KEY)) ?? {};
}

export function writeCatalogMeta(meta: Record<string, CatalogMeta>) {
  localStorage.setItem(CATALOG_META_KEY, JSON.stringify(meta));
}

export function toggleCatalogMeta(id: string, field: keyof CatalogMeta) {
  const meta = readCatalogMeta();
  const current = meta[id] ?? {};
  meta[id] = {
    ...current,
    [field]: !current[field],
  };
  writeCatalogMeta(meta);
  return meta[id];
}

export function setCatalogMetaField(id: string, patch: Partial<CatalogMeta>) {
  const meta = readCatalogMeta();
  meta[id] = {
    ...(meta[id] ?? {}),
    ...patch,
  };
  writeCatalogMeta(meta);
  return meta[id];
}

export type PresetCatalogSnapshot = {
  generatedAt: string;
  meta: Record<string, CatalogMeta>;
  entries: Array<{ key: string; value: unknown }>;
};

export function exportCatalogSnapshot(): PresetCatalogSnapshot {
  const entries: Array<{ key: string; value: unknown }> = [];
  for (let i = 0; i < localStorage.length; i += 1) {
    const key = localStorage.key(i);
    if (!key) {
      continue;
    }

    if (!key.startsWith(ASSERTIONS_PREFIX) && !key.startsWith(TEST_CONSOLE_PREFIX)) {
      continue;
    }

    const value = safeParse<unknown>(localStorage.getItem(key));
    entries.push({ key, value });
  }

  return {
    generatedAt: new Date().toISOString(),
    meta: readCatalogMeta(),
    entries,
  };
}

export function importCatalogSnapshot(snapshot: PresetCatalogSnapshot) {
  for (const entry of snapshot.entries) {
    if (!entry.key.startsWith(ASSERTIONS_PREFIX) && !entry.key.startsWith(TEST_CONSOLE_PREFIX)) {
      continue;
    }

    localStorage.setItem(entry.key, JSON.stringify(entry.value ?? []));
  }

  writeCatalogMeta(snapshot.meta ?? {});
}
