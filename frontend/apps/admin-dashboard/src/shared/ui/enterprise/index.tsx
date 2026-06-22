import type { ReactNode } from 'react';
import { Button, Card, DatePicker, Empty, Input, Select, Skeleton, Space, Table, Tag, Typography } from 'antd';
import type { TableProps } from 'antd';
import { DownloadOutlined, FilterOutlined, SearchOutlined, SettingOutlined } from '@ant-design/icons';

type PageLayoutProps = {
  title: string;
  description?: string;
  breadcrumb?: string[];
  actions?: ReactNode;
  children: ReactNode;
};

export function PageLayout({ title, description, breadcrumb = [], actions, children }: PageLayoutProps) {
  return (
    <div className="enterprise-page-layout">
      <DashboardHeader title={title} description={description} breadcrumb={breadcrumb} actions={actions} />
      {children}
    </div>
  );
}

export function DashboardHeader({ title, description, breadcrumb = [], actions }: Omit<PageLayoutProps, 'children'>) {
  return (
    <header className="enterprise-dashboard-header">
      <div>
        {breadcrumb.length > 0 ? <div className="enterprise-breadcrumb">{breadcrumb.join(' / ')}</div> : null}
        <Typography.Title level={1}>{title}</Typography.Title>
        {description ? <Typography.Text className="enterprise-page-description">{description}</Typography.Text> : null}
      </div>
      {actions ? <div className="enterprise-header-actions">{actions}</div> : null}
    </header>
  );
}

export function FilterBar({ children }: { children?: ReactNode }) {
  return (
    <div className="enterprise-filter-bar">
      <SearchBar placeholder="Search resources, tuples, models..." />
      <TimePicker />
      <Select className="enterprise-filter-control" defaultValue="production" options={[{ value: 'production', label: 'Production' }]} />
      <Select className="enterprise-filter-control" defaultValue="platform" options={[{ value: 'platform', label: 'Platform team' }]} />
      {children}
      <Button icon={<FilterOutlined />}>Filters</Button>
      <Button icon={<DownloadOutlined />}>Export</Button>
    </div>
  );
}

export function SearchBar({ placeholder = 'Search' }: { placeholder?: string }) {
  return <Input className="enterprise-search-bar" allowClear prefix={<SearchOutlined />} placeholder={placeholder} />;
}

export function TimePicker() {
  return <DatePicker.RangePicker className="enterprise-time-picker" showTime />;
}

export function MetricCard({
  label,
  value,
  meta,
  tone = 'neutral',
}: {
  label: string;
  value: ReactNode;
  meta?: ReactNode;
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'primary';
}) {
  return (
    <Card className={`enterprise-metric-card enterprise-metric-card-${tone}`}>
      <span className="enterprise-metric-label">{label}</span>
      <strong>{value}</strong>
      {meta ? <span className="enterprise-metric-meta">{meta}</span> : null}
    </Card>
  );
}

export function TrendChart({ title, tone = 'primary' }: { title: string; tone?: 'primary' | 'success' | 'warning' | 'danger' }) {
  return (
    <Card className="enterprise-trend-card" title={title}>
      <div className={`enterprise-sparkline enterprise-sparkline-${tone}`} aria-hidden="true">
        {Array.from({ length: 34 }).map((_, index) => (
          <span key={index} style={{ height: `${28 + ((index * 17) % 52)}%` }} />
        ))}
      </div>
    </Card>
  );
}

export function StatusBadge({ status }: { status: 'healthy' | 'warning' | 'critical' | 'unknown' }) {
  const config = {
    healthy: { color: 'success', label: 'Healthy' },
    warning: { color: 'warning', label: 'Warning' },
    critical: { color: 'error', label: 'Critical' },
    unknown: { color: 'default', label: 'Unknown' },
  }[status];

  return <Tag color={config.color}>{config.label}</Tag>;
}

export function DataTable<T extends object>(props: TableProps<T>) {
  return <Table<T> size="middle" sticky pagination={{ pageSize: 10, showSizeChanger: true }} {...props} />;
}

export function EnterpriseEmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="enterprise-empty-state">
      <Empty description={title} />
      {description ? <Typography.Text type="secondary">{description}</Typography.Text> : null}
    </div>
  );
}

export function LoadingSkeleton() {
  return (
    <div className="enterprise-loading-skeleton">
      <Skeleton active paragraph={{ rows: 2 }} />
      <Skeleton active paragraph={{ rows: 5 }} />
    </div>
  );
}

export function CommandPalette({ openHint = 'Ctrl K' }: { openHint?: string }) {
  return (
    <Button className="enterprise-command-trigger" icon={<SearchOutlined />}>
      Search or jump to...
      <kbd>{openHint}</kbd>
    </Button>
  );
}

export function ContextualActions() {
  return (
    <Space size={8} wrap>
      <Button icon={<SettingOutlined />}>Columns</Button>
      <Button icon={<FilterOutlined />}>Saved filter</Button>
    </Space>
  );
}
