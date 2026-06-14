import { lazy, type ComponentType, type LazyExoticComponent, type ReactNode } from 'react';
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

type RouteComponent = LazyExoticComponent<ComponentType>;

export type ProtectedRouteConfig = {
  path: string;
  Component: RouteComponent;
  label: string;
  icon: ReactNode;
  requiredRole?: string;
};

export const protectedRoutes: ProtectedRouteConfig[] = [
  {
    path: '/stores',
    Component: lazy(() => import('@/features/stores').then((module) => ({ default: module.StoresPage }))),
    label: 'Stores',
    icon: <DatabaseOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/models',
    Component: lazy(() => import('@/features/models').then((module) => ({ default: module.ModelsPage }))),
    label: 'Models',
    icon: <FileTextOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/relationships',
    Component: lazy(() => import('@/features/relationships').then((module) => ({ default: module.RelationshipsPage }))),
    label: 'Relationships',
    icon: <NodeIndexOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/assertions',
    Component: lazy(() => import('@/features/assertions').then((module) => ({ default: module.AssertionsPage }))),
    label: 'Assertions',
    icon: <CheckCircleOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/audit',
    Component: lazy(() => import('@/features/audit').then((module) => ({ default: module.AuditPage }))),
    label: 'Audit',
    icon: <AuditOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/test-console',
    Component: lazy(() => import('@/features/test-console').then((module) => ({ default: module.TestConsolePage }))),
    label: 'Test Console',
    icon: <DeploymentUnitOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/graph',
    Component: lazy(() => import('@/features/graph').then((module) => ({ default: module.GraphExplorerPage }))),
    label: 'Graph Explorer',
    icon: <NodeIndexOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/presets',
    Component: lazy(() => import('@/features/presets').then((module) => ({ default: module.PresetCatalogPage }))),
    label: 'Presets',
    icon: <SaveOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/access',
    Component: lazy(() => import('@/features/access').then((module) => ({ default: module.AccessManagementPage }))),
    label: 'Access',
    icon: <SafetyCertificateOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/profile',
    Component: lazy(() => import('@/features/profile').then((module) => ({ default: module.ProfilePage }))),
    label: 'Profile',
    icon: <UserOutlined />,
  },
];

const navigationGroups: Array<{ label: string; routes: string[] }> = [
  {
    label: 'Authorization',
    routes: ['/stores', '/models', '/relationships', '/assertions'],
  },
  {
    label: 'Evaluation',
    routes: ['/test-console', '/graph', '/audit', '/presets'],
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
