import type { Monaco } from '@monaco-editor/react';

export type JsonSchema = {
  uri: string;
  schema: Record<string, unknown>;
};

type RegisteredSchema = {
  uri: string;
  fileMatch: string[];
  schema: Record<string, unknown>;
};

const schemaRegistry = new Map<string, RegisteredSchema>();

export function registerJsonSchema(monaco: Monaco | null, path: string, schema: JsonSchema) {
  if (!monaco) return;

  schemaRegistry.set(path, {
    uri: schema.uri,
    fileMatch: [path],
    schema: schema.schema,
  });

  monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
    validate: true,
    allowComments: false,
    schemas: Array.from(schemaRegistry.values()),
  });
}
