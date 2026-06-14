import { LogoutOutlined } from '@ant-design/icons';
import { Button, Layout, Menu, Select, Space, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { getNavigationItems, protectedRoutes } from '@/app/routes/route-config';
import { useActiveStore } from '@/app/providers/useActiveStore';
import { useAuth } from '@/app/providers/useAuth';
import { apiClient } from '@/shared/api';

export function MainLayout() {
  const { accessToken, logout } = useAuth();
  const { activeStoreId, setActiveStoreId } = useActiveStore();
  const location = useLocation();
  const navigate = useNavigate();

  const storesQuery = useQuery({
    queryKey: ['stores'],
    queryFn: () => apiClient.listStores(),
    enabled: Boolean(accessToken),
  });

  const profileQuery = useQuery({
    queryKey: ['profile-layout'],
    queryFn: () => apiClient.getProfile(),
    enabled: Boolean(accessToken),
  });

  const navItems = getNavigationItems(profileQuery.data?.roles ?? []);

  const roleText = (profileQuery.data?.roles ?? []).slice(0, 2).join(', ');
  const currentRoute = protectedRoutes.find((route) => location.pathname.startsWith(route.path));

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <Layout className="pro-shell">
      <Layout.Sider width={232} theme="light" className="pro-sider">
        <div className="pro-brand">
          <div className="pro-brand-mark">A</div>
          <div>
            <div className="pro-brand-title">Aegis</div>
            <div className="pro-brand-subtitle">Authorization</div>
          </div>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={navItems}
          onClick={(item) => navigate(String(item.key))}
          className="pro-menu"
        />
      </Layout.Sider>
      <Layout>
        <Layout.Header className="pro-header">
          <div className="pro-header-title">
            <Typography.Text className="pro-kicker">Project</Typography.Text>
            <Typography.Title level={4}>{currentRoute?.label ?? 'Aegis'}</Typography.Title>
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
              options={(storesQuery.data ?? []).map((s) => ({ value: s.id, label: `${s.name} (${s.id})` }))}
              onChange={(value) => setActiveStoreId(value)}
            />
            <Button icon={<LogoutOutlined />} onClick={handleLogout}>
              Logout
            </Button>
          </Space>
        </Layout.Header>
        <Layout.Content className="pro-content">
          <div className="pro-content-frame">
            <Outlet />
          </div>
        </Layout.Content>
      </Layout>
    </Layout>
  );
}


