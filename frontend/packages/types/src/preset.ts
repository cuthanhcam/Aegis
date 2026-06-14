export type PresetSource = 'assertions' | 'test-console';

export type PresetItem = {
  source: PresetSource;
  storeId: string;
  scope: string;
  name: string;
  payload: string;
  updatedAt: string;
};

export type UpsertPresetRequest = {
  source: PresetSource;
  storeId: string;
  scope: string;
  name: string;
  payload: string;
};

export type DeletePresetRequest = {
  source: PresetSource;
  storeId: string;
  scope: string;
  name: string;
};

export type PresetMeta = {
  pinned: boolean;
  favorite: boolean;
  tags: string[];
  group?: string;
};
