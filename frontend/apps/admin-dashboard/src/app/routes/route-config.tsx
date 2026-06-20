import { lazy, type ComponentType, type LazyExoticComponent, type ReactNode } from 'react';
import {
  AuditOutlined,
  CheckCircleOutlined,
  DatabaseOutlined,
  DashboardOutlined,
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
  description: string;
  icon: ReactNode;
  requiredRole?: string;
};

export const protectedRoutes: ProtectedRouteConfig[] = [
  {
    path: '/overview',
    Component: lazy(() => import('@/features/overview').then((module) => ({ default: module.OverviewPage }))),
    label: 'Overview',
    description: 'Command center for stores, models, tuples, checks, metrics, and launch readiness.',
    icon: <DashboardOutlined />,
  },
  {
    path: '/stores',
    Component: lazy(() => import('@/features/stores').then((module) => ({ default: module.StoresPage }))),
    label: 'Stores',
    description: 'Tenant-scoped authorization stores and active runtime context.',
    icon: <DatabaseOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/models',
    Component: lazy(() => import('@/features/models').then((module) => ({ default: module.ModelsPage }))),
    label: 'Models',
    description: 'Versioned authorization DSL mapped to each active store.',
    icon: <FileTextOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/relationships',
    Component: lazy(() => import('@/features/relationships').then((module) => ({ default: module.RelationshipsPage }))),
    label: 'Relationships',
    description: 'Tuple writes, filters, and delete flows for ReBAC graph edges.',
    icon: <NodeIndexOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/assertions',
    Component: lazy(() => import('@/features/assertions').then((module) => ({ default: module.AssertionsPage }))),
    label: 'Assertions',
    description: 'Model assertion suites with import, export, presets, and validation.',
    icon: <CheckCircleOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/audit',
    Component: lazy(() => import('@/features/audit').then((module) => ({ default: module.AuditPage }))),
    label: 'Audit',
    description: 'Store change feeds and tenant-wide authorization audit decisions.',
    icon: <AuditOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/test-console',
    Component: lazy(() => import('@/features/test-console').then((module) => ({ default: module.TestConsolePage }))),
    label: 'Test Console',
    description: 'Check, explain, and compatibility batch requests against backend APIs.',
    icon: <DeploymentUnitOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/graph',
    Component: lazy(() => import('@/features/graph').then((module) => ({ default: module.GraphExplorerPage }))),
    label: 'Graph Explorer',
    description: 'List users, list objects, and expand usersets from the graph API.',
    icon: <NodeIndexOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/presets',
    Component: lazy(() => import('@/features/presets').then((module) => ({ default: module.PresetCatalogPage }))),
    label: 'Presets',
    description: 'Saved launch presets and catalog metadata across console workflows.',
    icon: <SaveOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/access',
    Component: lazy(() => import('@/features/access').then((module) => ({ default: module.AccessManagementPage }))),
    label: 'Access',
    description: 'Tenant users, roles, permissions, and assignment administration.',
    icon: <SafetyCertificateOutlined />,
    requiredRole: 'authorization_admin',
  },
  {
    path: '/profile',
    Component: lazy(() => import('@/features/profile').then((module) => ({ default: module.ProfilePage }))),
    label: 'Profile',
    description: 'Current session, tenant identity, and onboarding details.',
    icon: <UserOutlined />,
  },
];

const navigationGroups: Array<{ label: string; routes: string[] }> = [
  {
    label: 'Authorization',
    routes: ['/overview', '/stores', '/models', '/relationships', '/assertions'],
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
