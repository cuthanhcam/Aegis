import { Skeleton } from 'antd';

export function FormSkeleton() {
  return (
    <div>
      <Skeleton paragraph={{ rows: 1, width: ['100%'] }} style={{ marginBottom: '16px' }} />
      <Skeleton paragraph={{ rows: 1, width: ['100%'] }} style={{ marginBottom: '16px' }} />
      <Skeleton paragraph={{ rows: 1, width: ['30%'] }} />
    </div>
  );
}
