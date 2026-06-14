import { useMemo, useReducer, type ReactNode } from 'react';
import { useAuth } from './useAuth';
import { StoreContext, type StoreContextValue } from './store-context';

function decodeJwtPayload(token: string): Record<string, unknown> {
  const parts = token.split('.');
  if (parts.length < 2) {
    return {};
  }

  try {
    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padding = payload.length % 4 === 0 ? '' : '='.repeat(4 - (payload.length % 4));
    return JSON.parse(atob(`${payload}${padding}`)) as Record<string, unknown>;
  } catch {
    return {};
  }
}

export function StoreProvider({ children }: { children: ReactNode }) {
  const { accessToken } = useAuth();
  const tokenPayload = accessToken ? decodeJwtPayload(accessToken) : {};
  const tenantId = String(tokenPayload.tenant_id ?? tokenPayload.tid ?? '');
  const storeKey = tenantId ? `aegis.activeStoreId:${tenantId}` : 'aegis.activeStoreId:anonymous';
  const [, forceRender] = useReducer((value: number) => value + 1, 0);
  const activeStoreId = localStorage.getItem(storeKey) ?? '';

  const value = useMemo<StoreContextValue>(
    () => ({
      activeStoreId,
      setActiveStoreId(storeId: string) {
        if (storeId) {
          localStorage.setItem(storeKey, storeId);
        } else {
          localStorage.removeItem(storeKey);
        }

        forceRender();
      },
    }),
    [activeStoreId, storeKey],
  );

  return <StoreContext.Provider value={value}>{children}</StoreContext.Provider>;
}



