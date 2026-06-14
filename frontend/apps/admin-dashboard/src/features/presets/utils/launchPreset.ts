export type PresetLaunchSource = 'assertions' | 'test-console';

type CatalogPresetItem = {
  id: string;
  source: PresetLaunchSource;
  storeId: string;
  scope: string;
  name: string;
  payload: string;
  updatedAt: string;
};

type LaunchPresetPayload = {
  source: PresetLaunchSource;
  item: CatalogPresetItem;
};

const CATALOG_LAUNCH_KEY = 'aegis:preset-catalog:launch';

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

export function setLaunchPreset(payload: LaunchPresetPayload) {
  localStorage.setItem(CATALOG_LAUNCH_KEY, JSON.stringify(payload));
}

export function getLaunchPreset(): LaunchPresetPayload | null {
  return safeParse<LaunchPresetPayload>(localStorage.getItem(CATALOG_LAUNCH_KEY));
}

export function clearLaunchPreset() {
  localStorage.removeItem(CATALOG_LAUNCH_KEY);
}
