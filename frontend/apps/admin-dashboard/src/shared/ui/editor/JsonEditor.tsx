import Editor from '@monaco-editor/react';
import { APP_CODE_FONT_FAMILY } from '@/shared/utils/fonts';
import { registerJsonSchema } from './jsonSchemaRegistry';
import type { JsonEditorProps } from './types';

export function JsonEditor({
  value,
  onChange,
  height = 280,
  readOnly = false,
  path = 'inmemory://model/default.json',
  schema,
  language = 'json',
}: JsonEditorProps) {
  return (
    <div className="json-editor-wrap">
      <Editor
        path={path}
        height={height}
        defaultLanguage={language}
        theme="vs"
        value={value}
        beforeMount={(monaco) => {
          if (schema) {
            registerJsonSchema(monaco, path, schema);
          }
        }}
        onChange={(next) => onChange(next ?? '')}
        options={{
          readOnly,
          minimap: { enabled: false },
          wordWrap: 'on',
          scrollBeyondLastLine: false,
          automaticLayout: true,
          lineNumbers: 'on',
          lineNumbersMinChars: 4,
          renderLineHighlight: 'line',
          folding: true,
          tabSize: 2,
          fontSize: 13,
          fontFamily: APP_CODE_FONT_FAMILY,
          lineHeight: 20,
          fontLigatures: false,
          formatOnPaste: true,
          formatOnType: true,
        }}
      />
    </div>
  );
}
