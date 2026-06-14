export type SeedPreset = {
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

export function getDocumentViewerConsoleSeedPresets(): SeedPreset[] {
  return [
    {
      name: 'seed-check-allow-viewer',
      user: 'user:anne',
      relation: 'viewer',
      object: 'document:roadmap',
      consistency: '',
      authorizationModelId: '',
      batchSize: '1',
      contextualTuplesJson: '[]',
      contextJson: '{}',
      updatedAt: new Date(0).toISOString(),
    },
    {
      name: 'seed-check-with-context',
      user: 'user:anne',
      relation: 'viewer',
      object: 'document:roadmap',
      consistency: 'fully_consistent',
      authorizationModelId: '',
      batchSize: '1',
      contextualTuplesJson: '[]',
      contextJson: JSON.stringify({ approved: true }, null, 2),
      updatedAt: new Date(0).toISOString(),
    },
  ];
}
