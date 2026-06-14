import type { JsonSchema } from './jsonSchemaRegistry';

export type JsonEditorProps = {
  value: string;
  onChange: (nextValue: string) => void;
  height?: number;
  readOnly?: boolean;
  path?: string;
  schema?: JsonSchema;
  language?: 'json' | 'yaml' | 'plaintext';
};
