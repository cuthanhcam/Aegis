import { diffLines } from 'diff';
import { normalizeJson } from './jsonDiffView.utils';

type JsonDiffViewProps = {
  left: string;
  right: string;
};

export function JsonDiffView({ left, right }: JsonDiffViewProps) {
  const normalizedLeft = normalizeJson(left);
  const normalizedRight = normalizeJson(right);
  const chunks = diffLines(normalizedLeft, normalizedRight);

  return (
    <pre className="json-diff-pre">
      {chunks.map((chunk, idx) => (
        <span
          key={`diff-${idx}`}
          className={chunk.added ? 'json-diff-added' : chunk.removed ? 'json-diff-removed' : 'json-diff-same'}
        >
          {chunk.value}
        </span>
      ))}
    </pre>
  );
}
