/**
 * Aegis Dashboard - Client SDK
 *
 * Central export hub for all public APIs, hooks, components, and utilities
 * used throughout the Aegis Dashboard application.
 */

// ============================================================================
// API & Client Layer
// ============================================================================
export { apiClient, tokenStorage } from '../../src/shared/api';

// ============================================================================
// Hooks - Application State & Effects
// ============================================================================
export { useAuth } from '../../src/app/providers/useAuth';
export { useActiveStore } from '../../src/app/providers/useActiveStore';

// ============================================================================
// Hooks - Reusable Custom Hooks
// ============================================================================
export { useNotification } from '../../src/shared/hooks/useNotification';
export { useUrlState, type UseUrlStateOptions } from '../../src/shared/hooks/useUrlState';

// ============================================================================
// UI Components - Layout & Access Control
// ============================================================================
export { AccessGate } from '../../src/shared/ui/AccessGate';
export { ProtectedRoute } from '../../src/shared/ui/ProtectedRoute';

// ============================================================================
// UI Components - Data Display
// ============================================================================
export { JsonEditor } from '../../src/shared/ui/JsonEditor';
export { JsonDiffView } from '../../src/shared/ui/JsonDiffView';
export { TableSkeleton, TableRowSkeleton, FormSkeleton, type TableSkeletonProps, type SkeletonProps } from '../../src/shared/ui/TableSkeleton';
export { EmptyState, TableEmptyState, ListEmptyState, type EmptyStateProps } from '../../src/shared/ui/EmptyState';

// ============================================================================
// Utilities - Preset Management
// ============================================================================
export {
	listCatalogPresets,
	deleteCatalogPreset,
	readCatalogMeta,
	writeCatalogMeta,
	toggleCatalogMeta,
	setCatalogMetaField,
	exportCatalogSnapshot,
	importCatalogSnapshot,
	setLaunchPreset,
	getLaunchPreset,
	clearLaunchPreset,
	type CatalogPresetItem,
	type PresetSource,
} from '../../src/shared/utils/presetCatalog';
export {
	getDocumentViewerAssertionSeedPresets,
	getDocumentViewerConsoleSeedPresets,
} from '../../src/shared/utils/seedPresets';

// ============================================================================
// Features - Page Components
// ============================================================================
export { StoresPage } from '../../src/features/stores/pages/StoresPage';
export { RelationshipsPage } from '../../src/features/relationships/pages/RelationshipsPage';
export { AssertionsPage } from '../../src/features/assertions/pages/AssertionsPage';
export { PresetCatalogPage } from '../../src/features/presets/pages/PresetCatalogPage';
export { TestConsolePage } from '../../src/features/test-console/pages/TestConsolePage';
export { AccessManagementPage } from '../../src/features/access/pages/AccessManagementPage';
export { ProfilePage } from '../../src/features/profile/pages/ProfilePage';
export { AuditPage } from '../../src/features/audit/pages/AuditPage';

// ============================================================================
// Re-exports - Common Ant Design Components (for convenience)
// ============================================================================
export type { InputProps } from 'antd';
export { Button, Card, Input, Select, Table, Space, Typography, Modal, Popconfirm, Alert, message, notification } from 'antd';

// ============================================================================
// Version & Constants
// ============================================================================
export const CLIENT_VERSION = '1.0.0';
export const AEGIS_API_TIMEOUT = 30000; // milliseconds
export const DEFAULT_PAGINATION_SIZE = 20;
export const UNDO_TIMEOUT_MS = 7000; // milliseconds for undo grace period
