import { Empty, Typography } from 'antd';
import { type CSSProperties, type ReactNode } from 'react';

export interface EmptyStateProps {
  title?: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
  style?: CSSProperties;
}

export function EmptyState({
  title = 'No Data',
  description,
  icon,
  style,
}: EmptyStateProps) {
  return (
    <div
      style={{
        padding: '40px 20px',
        textAlign: 'center',
        ...style,
      }}
    >
      <Empty
        image={icon ? undefined : Empty.PRESENTED_IMAGE_SIMPLE}
        description={
          <div>
            {title && <Typography.Title level={4}>{title}</Typography.Title>}
            {description && <Typography.Text type="secondary">{description}</Typography.Text>}
          </div>
        }
      />
    </div>
  );
}
