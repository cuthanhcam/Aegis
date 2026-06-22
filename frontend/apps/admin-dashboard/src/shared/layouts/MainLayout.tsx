import {
  AlertOutlined,
  ApiOutlined,
  AppstoreOutlined,
  AuditOutlined,
  BarChartOutlined,
  BellOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  CodeOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  DeploymentUnitOutlined,
  FileTextOutlined,
  FilterOutlined,
  GlobalOutlined,
  LineChartOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  NodeIndexOutlined,
  PlusOutlined,
  SearchOutlined,
  SettingOutlined,
  StarOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Badge, Button, Drawer, Input, Layout, Modal, Select, Space, Tag, Tooltip, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { protectedRoutes } from '@/app/routes/route-config';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { useAuth } from '@/app/providers/useAuth';
import { apiClient } from '@/shared/api';
import { TableSkeleton } from '@/shared/ui';

type ModuleKey = 'dashboard' | 'analytics' | 'agents' | 'services' | 'monitoring' | 'logs' | 'events' | 'alerts' | 'settings';

type ProductModule = {
  key: ModuleKey;
  label: string;
  path: string;
  icon: ReactNode;
  description: string;
  routes: string[];
  context?: boolean;
};

type ProductGroup = {
  label: string;
  modules: ProductModule[];
};

const moduleGroups: ProductGroup[] = [
  {
    label: 'Workspace',
    modules: [
      {
        key: 'dashboard',
        label: 'Dashboard',
        path: '/overview',
        icon: <DashboardOutlined />,
        description: 'Executive health, readiness, and operating context.',
        routes: ['/overview'],
      },
      {
        key: 'analytics',
        label: 'Analytics',
        path: '/test-console',
        icon: <BarChartOutlined />,
        description: 'Decision analytics, checks, explains, and batch diagnostics.',
        routes: ['/test-console', '/graph'],
        context: true,
      },
      {
        key: 'agents',
        label: 'Agents',
        path: '/assertions',
        icon: <DeploymentUnitOutlined />,
        description: 'Assertion suites, model safety checks, and launch presets.',
        routes: ['/assertions', '/presets'],
        context: true,
      },
      {
        key: 'services',
        label: 'Services',
        path: '/models',
        icon: <ApiOutlined />,
        description: 'Authorization models, tuple graph, and store-scoped resources.',
        routes: ['/models', '/relationships', '/stores'],
        context: true,
      },
    ],
  },
  {
    label: 'Telemetry',
    modules: [
      {
        key: 'monitoring',
        label: 'Monitoring',
        path: '/graph',
        icon: <LineChartOutlined />,
        description: 'Graph query surfaces and operational performance signals.',
        routes: ['/graph'],
      },
      {
        key: 'logs',
        label: 'Logs',
        path: '/audit',
        icon: <FileTextOutlined />,
        description: 'Audit trails, decision logs, and forensic evidence.',
        routes: ['/audit'],
      },
      {
        key: 'events',
        label: 'Events',
        path: '/relationships',
        icon: <AuditOutlined />,
        description: 'Relationship changes and store activity timelines.',
        routes: ['/relationships'],
      },
      {
        key: 'alerts',
        label: 'Alerts',
        path: '/presets',
        icon: <AlertOutlined />,
        description: 'Saved launch checks, readiness warnings, and guardrail views.',
        routes: ['/presets'],
      },
    ],
  },
  {
    label: 'Management',
    modules: [
      {
        key: 'settings',
        label: 'Settings',
        path: '/access',
        icon: <SettingOutlined />,
        description: 'Users, roles, permissions, profile, and workspace settings.',
        routes: ['/access', '/profile'],
        context: true,
      },
    ],
  },
];

const productModules = moduleGroups.flatMap((group) => group.modules);

const routeIconFallback: Record<string, ReactNode> = {
  '/overview': <DashboardOutlined />,
  '/stores': <DatabaseOutlined />,
  '/models': <CodeOutlined />,
  '/relationships': <NodeIndexOutlined />,
  '/assertions': <CheckCircleOutlined />,
  '/audit': <AuditOutlined />,
  '/test-console': <BarChartOutlined />,
  '/graph': <AppstoreOutlined />,
  '/presets': <StarOutlined />,
  '/access': <TeamOutlined />,
  '/profile': <UserOutlined />,
};

const savedViews = ['Production denies', 'Model publish risks', 'Tuple writes today', 'Audit exceptions'];
const favoriteViews = ['Backend overview', 'Assertion failures', 'Store graph health'];
const recentViews = ['document:roadmap', 'user:anne', 'model rollback', 'viewer relation'];

export function MainLayout() {
  const { accessToken, logout } = useAuth();
  const { activeStoreId, setActiveStoreId } = useActiveStore();
  const location = useLocation();
  const navigate = useNavigate();
  const [collapsed, setCollapsed] = useState(false);
  const [commandOpen, setCommandOpen] = useState(false);
  const [contextOpen, setContextOpen] = useState(false);

  const storesQuery = useQuery({
    queryKey: ['stores', accessToken],
    queryFn: () => apiClient.listStores(),
    enabled: Boolean(accessToken),
  });

  const profileQuery = useQuery({
    queryKey: ['profile-layout', accessToken],
    queryFn: () => apiClient.getProfile(),
    enabled: Boolean(accessToken),
  });

  const allowedRoutes = useMemo(() => {
    if (!profileQuery.data?.roles) {
      return protectedRoutes;
    }

    const roleSet = new Set((profileQuery.data?.roles ?? []).map((role) => role.toLowerCase()));
    return protectedRoutes.filter((route) => !route.requiredRole || roleSet.has(route.requiredRole.toLowerCase()));
  }, [profileQuery.data?.roles]);

  const routeByPath = useMemo(() => new Map(allowedRoutes.map((route) => [route.path, route])), [allowedRoutes]);
  const currentRoute = allowedRoutes.find((route) => location.pathname.startsWith(route.path)) ?? allowedRoutes[0];
  const currentModule =
    productModules.find((module) => module.routes.some((path) => location.pathname.startsWith(path))) ?? productModules[0];
  const stores = useMemo(() => storesQuery.data ?? [], [storesQuery.data]);
  const activeStore = stores.find((store) => store.id === activeStoreId);
  const activeStoreIsValid = !activeStoreId || stores.some((store) => store.id === activeStoreId);
  const isValidatingActiveStore = Boolean(activeStoreId) && (!storesQuery.isSuccess || !activeStoreIsValid);
  const roleText = (profileQuery.data?.roles ?? []).slice(0, 2).join(', ');

  const activeContextRoutes = currentModule.routes
    .map((path) => routeByPath.get(path))
    .filter((route): route is NonNullable<typeof currentRoute> => Boolean(route));
  const showContextPanel = Boolean(currentModule.context && activeContextRoutes.length > 1);
  const commandItems = allowedRoutes;

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  useEffect(() => {
    if (!storesQuery.isSuccess) {
      return;
    }

    if (stores.length === 0) {
      if (activeStoreId) {
        setActiveStoreId('');
      }

      return;
    }

    if (!activeStoreId || !stores.some((store) => store.id === activeStoreId)) {
      setActiveStoreId(stores[0].id);
    }
  }, [activeStoreId, setActiveStoreId, stores, storesQuery.isSuccess]);

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setCommandOpen(true);
      }
    };

    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  return (
    <Layout
      className={[
        'enterprise-shell',
        collapsed ? 'enterprise-shell-collapsed' : '',
        showContextPanel ? 'enterprise-shell-has-context' : 'enterprise-shell-no-context',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <Layout.Sider width={collapsed ? 72 : 268} theme="light" className="enterprise-sidebar">
        <div className="enterprise-sidebar-inner">
          <div>
            <div className="enterprise-brand">
              <button type="button" className="enterprise-brand-mark" onClick={() => navigate('/overview')} aria-label="Go to dashboard">
                <img src="/aegis.svg" alt="" />
              </button>
              {!collapsed ? (
                <div className="enterprise-brand-copy">
                  <strong>Aegis</strong>
                  <span>Authorization Cloud</span>
                </div>
              ) : null}
              <Button
                type="text"
                size="small"
                icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
                onClick={() => setCollapsed((value) => !value)}
                aria-label={collapsed ? 'Expand navigation' : 'Collapse navigation'}
              />
            </div>

            <button type="button" className="enterprise-workspace-switcher" onClick={() => setCommandOpen(true)}>
              <GlobalOutlined />
              {!collapsed ? (
                <span>
                  <strong>{profileQuery.data?.tenantId ?? 'Launch workspace'}</strong>
                  <small>{activeStore?.name ?? 'Select active store'}</small>
                </span>
              ) : null}
            </button>

            {!collapsed ? (
              <div className="enterprise-mode-switcher" aria-label="Workspace mode">
                <button type="button" className="is-active">
                  <CodeOutlined /> Code
                </button>
                <button type="button" onClick={() => navigate('/assertions')}>
                  <DeploymentUnitOutlined /> Agents
                </button>
              </div>
            ) : null}

            <button type="button" className="enterprise-global-search" onClick={() => setCommandOpen(true)}>
              <SearchOutlined />
              {!collapsed ? (
                <>
                  <span>Search</span>
                  <kbd>Ctrl K</kbd>
                </>
              ) : null}
            </button>

            <nav className="enterprise-primary-nav" aria-label="Product modules">
              {moduleGroups.map((group) => (
                <section key={group.label}>
                  {!collapsed ? <div className="enterprise-nav-group-label">{group.label}</div> : null}
                  {group.modules.map((module) => {
                    const selected = module.key === currentModule.key;
                    return (
                      <Tooltip key={module.key} title={collapsed ? module.label : undefined} placement="right">
                        <button
                          type="button"
                          className={selected ? 'is-active' : ''}
                          onClick={() => navigate(module.path)}
                          aria-current={selected ? 'page' : undefined}
                        >
                          {module.icon}
                          {!collapsed ? <span>{module.label}</span> : null}
                        </button>
                      </Tooltip>
                    );
                  })}
                </section>
              ))}
            </nav>
          </div>

          <div className="enterprise-sidebar-footer">
            <Tooltip title={collapsed ? 'Profile' : undefined} placement="right">
              <button type="button" className="enterprise-user-tile" onClick={() => navigate('/profile')}>
                <span className="enterprise-avatar">DU</span>
                {!collapsed ? (
                  <span>
                    <strong>{profileQuery.data?.username ?? 'Demo User'}</strong>
                    <small>{roleText || 'Operator'}</small>
                  </span>
                ) : null}
              </button>
            </Tooltip>
            <div className="enterprise-footer-actions">
              <Button type="text" icon={<BellOutlined />} aria-label="Notifications" />
              <Button type="text" icon={<SettingOutlined />} onClick={() => navigate('/access')} aria-label="Settings" />
              <Button type="text" icon={<LogoutOutlined />} onClick={handleLogout} aria-label="Logout" />
            </div>
          </div>
        </div>
      </Layout.Sider>

      {showContextPanel ? (
        <aside className="enterprise-context-panel">
          <div className="enterprise-context-header">
            <span className="enterprise-kicker">{currentModule.label}</span>
            <Typography.Title level={2}>{currentRoute?.label ?? currentModule.label}</Typography.Title>
            <Typography.Text>{currentModule.description}</Typography.Text>
          </div>

          <Input allowClear prefix={<SearchOutlined />} placeholder={`Search ${currentModule.label.toLowerCase()}`} />

          <section>
            <div className="enterprise-context-label">Views</div>
            <div className="enterprise-context-list">
              {activeContextRoutes.map((route) => {
                const selected = location.pathname.startsWith(route.path);
                return (
                  <button key={route.path} type="button" className={selected ? 'is-active' : ''} onClick={() => navigate(route.path)}>
                    {route.icon ?? routeIconFallback[route.path]}
                    <span>{route.label}</span>
                  </button>
                );
              })}
            </div>
          </section>

          <section>
            <div className="enterprise-context-label">Saved filters</div>
            <div className="enterprise-chip-list">
              {savedViews.map((view) => (
                <button key={view} type="button">
                  <FilterOutlined />
                  {view}
                </button>
              ))}
            </div>
          </section>

          <section>
            <div className="enterprise-context-label">Favorites</div>
            <div className="enterprise-chip-list">
              {favoriteViews.map((view) => (
                <button key={view} type="button">
                  <StarOutlined />
                  {view}
                </button>
              ))}
            </div>
          </section>

          <section>
            <div className="enterprise-context-label">Recent</div>
            <div className="enterprise-recent-list">
              {recentViews.map((view) => (
                <button key={view} type="button">
                  {view}
                </button>
              ))}
            </div>
          </section>

          <Button block icon={<PlusOutlined />} onClick={() => navigate(currentModule.path)}>
            Quick create
          </Button>
        </aside>
      ) : null}

      <Layout className="enterprise-main-shell">
        <header className="enterprise-topbar">
          <div className="enterprise-page-heading">
            {showContextPanel ? (
              <Button className="enterprise-context-toggle" icon={<AppstoreOutlined />} onClick={() => setContextOpen(true)}>
                Context
              </Button>
            ) : null}
            <span className="enterprise-breadcrumb">Aegis / {currentModule.label} / {currentRoute?.label ?? 'Overview'}</span>
            <Typography.Title level={1}>{currentRoute?.label ?? 'Dashboard'}</Typography.Title>
            <Typography.Text>{currentRoute?.description ?? currentModule.description}</Typography.Text>
          </div>

          <div className="enterprise-status-strip">
            <Tag color="success">Live</Tag>
            {profileQuery.data?.tenantId ? <Tag>Tenant: {profileQuery.data.tenantId}</Tag> : null}
            {roleText ? <Tag color="red">Role: {roleText}</Tag> : null}
            <span>
              <ClockCircleOutlined /> Updated now
            </span>
          </div>
        </header>

        <div className="enterprise-filterbar">
          <Select
            showSearch
            className="enterprise-store-select"
            placeholder="Select active store"
            loading={storesQuery.isLoading}
            value={activeStoreId || undefined}
            options={stores.map((store) => ({ value: store.id, label: `${store.name} (${store.id.slice(0, 8)})` }))}
            onChange={(value) => setActiveStoreId(value)}
          />
          <Select
            className="enterprise-filter-select"
            defaultValue="production"
            options={[
              { value: 'production', label: 'Production' },
              { value: 'staging', label: 'Staging' },
              { value: 'development', label: 'Development' },
            ]}
          />
          <Input className="enterprise-inline-search" allowClear prefix={<SearchOutlined />} placeholder="Search this view" />
          <Button icon={<FilterOutlined />}>Filters</Button>
          <Button>Last 24 hours</Button>
          <Button>Export</Button>
        </div>

        <Layout.Content className="enterprise-content">
          {isValidatingActiveStore ? <TableSkeleton rows={5} columns={4} /> : <Outlet />}
        </Layout.Content>
      </Layout>

      <Drawer
        title="Module context"
        placement="left"
        open={contextOpen}
        width={320}
        onClose={() => setContextOpen(false)}
        className="enterprise-mobile-context"
      >
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          {activeContextRoutes.map((route) => (
            <Button key={route.path} block onClick={() => navigate(route.path)}>
              {route.label}
            </Button>
          ))}
        </Space>
      </Drawer>

      <Modal
        title="Command palette"
        open={commandOpen}
        footer={null}
        onCancel={() => setCommandOpen(false)}
        className="enterprise-command-modal"
      >
        <Input autoFocus prefix={<SearchOutlined />} placeholder="Search pages, stores, saved filters, recent resources..." />
        <div className="enterprise-command-list">
          {commandItems.map((route) => (
            <button
              key={route.path}
              type="button"
              onClick={() => {
                navigate(route.path);
                setCommandOpen(false);
              }}
            >
              <span>{route.icon ?? routeIconFallback[route.path]}</span>
              <strong>{route.label}</strong>
              <small>{route.description}</small>
            </button>
          ))}
          {stores.map((store) => (
            <button
              key={store.id}
              type="button"
              onClick={() => {
                setActiveStoreId(store.id);
                setCommandOpen(false);
              }}
            >
              <span>
                <DatabaseOutlined />
              </span>
              <strong>{store.name}</strong>
              <small>{store.id}</small>
              {store.id === activeStoreId ? <Badge status="processing" text="Active" /> : null}
            </button>
          ))}
        </div>
      </Modal>
    </Layout>
  );
}
