import { Empty } from 'antd';
import { type ReactNode } from 'react';

type TableEmptyStateProps = {
  message?: string;
  action?: ReactNode;
};

export function TableEmptyState({
  message = 'No records found',
  action,
}: TableEmptyStateProps) {
  return (
    <div
      style={{
        padding: '40px 20px',
        textAlign: 'center',
      }}
    >
      <Empty
        description={message}
        style={{ margin: '0 auto' }}
      />
      {action && <div style={{ marginTop: '16px' }}>{action}</div>}
    </div>
  );
}
