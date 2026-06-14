import type { ReactNode } from 'react';
import {
  AuditOutlined,
  CheckCircleOutlined,
  DatabaseOutlined,
  DeploymentUnitOutlined,
  FileTextOutlined,
  SafetyCertificateOutlined,
  SaveOutlined,
  NodeIndexOutlined,
  UserOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { AccessManagementPage } from '@/features/access';
import { AssertionsPage } from '@/features/assertions';
import { AuditPage } from '@/features/audit';
import { GraphExplorerPage } from '@/features/graph';
import { ModelsPage } from '@/features/models';
import { PresetCatalogPage } from '@/features/presets';
import { ProfilePage } from '@/features/profile';
import { RelationshipsPage } from '@/features/relationships';
import { StoresPage } from '@/features/stores';
import { TestConsolePage } from '@/features/test-console';

export type ProtectedRouteConfig = {
  path: string;
  element: ReactNode;
  label: string;
  icon: ReactNode;
  requiredRole?: string;
};

export const protectedRoutes: ProtectedRouteConfig[] = [
  { path: '/stores', element: <StoresPage />, label: 'Stores', icon: <DatabaseOutlined />, requiredRole: 'authorization_admin' },
  { path: '/models', element: <ModelsPage />, label: 'Models', icon: <FileTextOutlined />, requiredRole: 'authorization_admin' },
  { path: '/relationships', element: <RelationshipsPage />, label: 'Relationships', icon: <NodeIndexOutlined />, requiredRole: 'authorization_admin' },
  { path: '/assertions', element: <AssertionsPage />, label: 'Assertions', icon: <CheckCircleOutlined />, requiredRole: 'authorization_admin' },
  { path: '/audit', element: <AuditPage />, label: 'Store Changes', icon: <AuditOutlined />, requiredRole: 'authorization_admin' },
  { path: '/test-console', element: <TestConsolePage />, label: 'Test Console', icon: <DeploymentUnitOutlined />, requiredRole: 'authorization_admin' },
  { path: '/graph', element: <GraphExplorerPage />, label: 'Graph Explorer', icon: <NodeIndexOutlined />, requiredRole: 'authorization_admin' },
  { path: '/access', element: <AccessManagementPage />, label: 'Access Management', icon: <SafetyCertificateOutlined />, requiredRole: 'authorization_admin' },
  { path: '/profile', element: <ProfilePage />, label: 'Profile', icon: <UserOutlined /> },
  { path: '/presets', element: <PresetCatalogPage />, label: 'Preset Catalog', icon: <SaveOutlined />, requiredRole: 'authorization_admin' },
];

const navigationGroups: Array<{ label: string; routes: string[] }> = [
  {
    label: 'Design',
    routes: ['/stores', '/models', '/relationships', '/assertions'],
  },
  {
    label: 'Testing & Review',
    routes: ['/audit', '/test-console', '/graph', '/presets'],
  },
  {
    label: 'Administration',
    routes: ['/access', '/profile'],
  },
];

export function getNavigationItems(roles: string[]): MenuProps['items'] {
  const roleSet = new Set(roles.map((role) => role.toLowerCase()));
  const allowedRoutes = protectedRoutes.filter((route) => !route.requiredRole || roleSet.has(route.requiredRole.toLowerCase()));

  return navigationGroups
    .map((group) => ({
      type: 'group' as const,
      label: group.label,
      children: group.routes
        .map((path) => allowedRoutes.find((route) => route.path === path))
        .filter((route): route is ProtectedRouteConfig => Boolean(route))
        .map((route) => ({
          key: route.path,
          label: route.label,
          icon: route.icon,
        })),
    }))
    .filter((group) => group.children.length > 0);
}
