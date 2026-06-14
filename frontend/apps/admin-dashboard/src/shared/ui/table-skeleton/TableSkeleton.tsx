import { Skeleton } from 'antd';

export interface TableSkeletonProps {
  rows?: number;
  columns?: number;
}

export function TableSkeleton({ rows = 5, columns = 4 }: TableSkeletonProps) {
  const columnRatios = Array.from({ length: columns }).map((_, index) => {
    if (index === 0) return 1.4;
    if (index === columns - 1) return 1.2;
    return 1;
  });

  const totalRatio = columnRatios.reduce((sum, value) => sum + value, 0);

  return (
    <div style={{ width: '100%' }}>
      <div style={{ display: 'flex', gap: 12, marginBottom: 14, width: '100%' }}>
        {columnRatios.map((ratio, index) => (
          <Skeleton.Button
            key={`header-${index}`}
            active
            size="small"
            block
            style={{
              flex: ratio,
              maxWidth: `${(ratio / totalRatio) * 100}%`,
              height: 16,
            }}
          />
        ))}
      </div>

      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={`row-${rowIndex}`} style={{ display: 'flex', gap: 12, marginBottom: 12, width: '100%' }}>
          {columnRatios.map((ratio, colIndex) => (
            <Skeleton.Button
              key={`cell-${rowIndex}-${colIndex}`}
              active
              block
              style={{
                flex: ratio,
                maxWidth: `${(ratio / totalRatio) * 100}%`,
                height: 40,
              }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}
