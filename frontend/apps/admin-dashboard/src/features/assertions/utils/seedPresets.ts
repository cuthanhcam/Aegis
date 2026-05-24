export type SeedPreset = {
  name: string;
  payload: string;
  updatedAt: string;
};

export function getDocumentViewerAssertionSeedPresets(): SeedPreset[] {
  return [
    {
      name: 'seed-allow-user-viewer',
      payload: JSON.stringify(
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
      updatedAt: new Date(0).toISOString(),
    },
    {
      name: 'seed-deny-user-viewer',
      payload: JSON.stringify(
        {
          assertions: [
            {
              tuple_key: {
                user: 'user:bob',
                relation: 'viewer',
                object: 'document:roadmap',
              },
              expectation: false,
            },
          ],
        },
        null,
        2,
      ),
      updatedAt: new Date(0).toISOString(),
    },
  ];
}
