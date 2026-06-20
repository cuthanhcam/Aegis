import { LogoutOutlined } from '@ant-design/icons';
import { Button, Layout, Menu, Select, Space, Tag, Tooltip, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { getNavigationItems, protectedRoutes } from '@/app/routes/route-config';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { useAuth } from '@/app/providers/useAuth';
import { apiClient } from '@/shared/api';
import { TableSkeleton } from '@/shared/ui';

export function MainLayout() {
  const { accessToken, logout } = useAuth();
  const { activeStoreId, setActiveStoreId } = useActiveStore();
  const location = useLocation();
  const navigate = useNavigate();

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

  const navItems = getNavigationItems(profileQuery.data?.roles ?? []);

  const roleText = (profileQuery.data?.roles ?? []).slice(0, 2).join(', ');
  const currentRoute = protectedRoutes.find((route) => location.pathname.startsWith(route.path)) ?? protectedRoutes[0];
  const stores = useMemo(() => storesQuery.data ?? [], [storesQuery.data]);
  const activeStore = stores.find((store) => store.id === activeStoreId);
  const activeStoreIsValid = !activeStoreId || stores.some((store) => store.id === activeStoreId);
  const isValidatingActiveStore = Boolean(activeStoreId) && (!storesQuery.isSuccess || !activeStoreIsValid);

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

  return (
    <Layout className="pro-shell">
      <Layout.Sider width={232} theme="light" className="pro-sider">
        <div className="pro-brand">
          <div className="pro-brand-mark">A</div>
          <div>
            <div className="pro-brand-title">Aegis</div>
            <div className="pro-brand-subtitle">Authorization Console</div>
          </div>
        </div>
        <div className="pro-sider-context">
          <span className="pro-kicker">Active store</span>
          <Tooltip title={activeStore ? `${activeStore.name} (${activeStore.id})` : 'No active store selected'}>
            <div className="pro-sider-store">{activeStore?.name ?? 'No store selected'}</div>
          </Tooltip>
          <div className="pro-sider-statline">
            <span>{stores.length} store{stores.length === 1 ? '' : 's'}</span>
            <span>{profileQuery.data?.tenantId ?? 'tenant pending'}</span>
          </div>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[currentRoute?.path ?? location.pathname]}
          items={navItems}
          onClick={(item) => navigate(String(item.key))}
          className="pro-menu"
        />
      </Layout.Sider>
      <Layout>
        <Layout.Header className="pro-header">
          <div className="pro-header-title">
            <Typography.Text className="pro-kicker">Aegis workspace</Typography.Text>
            <Typography.Title level={3}>{currentRoute?.label ?? 'Aegis'}</Typography.Title>
            <Typography.Text className="pro-route-description">
              {currentRoute?.description ?? 'Manage authorization state and evaluate access decisions.'}
            </Typography.Text>
          </div>
          <Space size={10} wrap className="pro-header-actions">
            {profileQuery.data?.tenantId ? <Tag>Tenant: {profileQuery.data.tenantId}</Tag> : null}
            {roleText ? <Tag color="blue">Role: {roleText}</Tag> : null}
            <Select
              showSearch
              className="pro-store-select"
              placeholder="Select active store"
              loading={storesQuery.isLoading}
              value={activeStoreId || undefined}
              options={stores.map((s) => ({ value: s.id, label: `${s.name} (${s.id})` }))}
              onChange={(value) => setActiveStoreId(value)}
            />
            <Button icon={<LogoutOutlined />} onClick={handleLogout}>
              Logout
            </Button>
          </Space>
        </Layout.Header>
        <Layout.Content className="pro-content">
          {isValidatingActiveStore ? <TableSkeleton rows={5} columns={4} /> : <Outlet />}
        </Layout.Content>
      </Layout>
    </Layout>
  );
}
