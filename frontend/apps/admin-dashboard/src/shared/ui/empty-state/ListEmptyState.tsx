import { Empty, Typography } from 'antd';
import { type ReactNode } from 'react';

type ListEmptyStateProps = {
  title?: string;
  description?: string;
  action?: ReactNode;
};

export function ListEmptyState({
  title = 'No items yet',
  description = 'Add your first item to get started',
  action,
}: ListEmptyStateProps) {
  return (
    <Empty
      description={
        <div>
          <Typography.Title level={4}>{title}</Typography.Title>
          <Typography.Text type="secondary">{description}</Typography.Text>
        </div>
      }
      style={{
        padding: '40px',
        marginTop: '20px',
        border: '1px solid #f0f0f0',
        borderRadius: '4px',
        background: '#fafafa',
      }}
    >
      {action && <div style={{ marginTop: '16px' }}>{action}</div>}
    </Empty>
  );
}
