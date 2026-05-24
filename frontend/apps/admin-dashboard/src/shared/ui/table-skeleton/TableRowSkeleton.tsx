import { Skeleton } from 'antd';

export function TableRowSkeleton() {
  return (
    <div style={{ display: 'flex', gap: '12px', marginBottom: '12px' }}>
      <Skeleton.Avatar size="large" shape="square" />
      <Skeleton paragraph={{ rows: 2, width: '100%' }} title={false} />
    </div>
  );
}
