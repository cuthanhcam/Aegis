import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { AUTH_FORCE_LOGIN_EVENT, AUTH_SESSION_EVENT, apiClient, tokenStorage } from '@/shared/api';
import { AuthContext, type AuthContextValue } from './auth-context.ts';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(tokenStorage.getAccessToken());

  const applySession = useCallback((response: { accessToken: string; refreshToken?: string | null; expiresIn: number }) => {
    tokenStorage.setSession(response);
  }, []);

  const logout = useCallback(() => {
    apiClient.logout().catch(() => {
      // Session is cleared client-side regardless of revoke response.
    });
    tokenStorage.clear();
    setAccessToken(null);
  }, []);

  useEffect(() => {
    const syncSession = () => {
      setAccessToken(tokenStorage.getAccessToken());
    };

    const forceLogin = () => {
      tokenStorage.clear();
      setAccessToken(null);
      if (window.location.pathname !== '/login') {
        window.location.replace('/login');
      }
    };

    syncSession();
    window.addEventListener(AUTH_SESSION_EVENT, syncSession);
    window.addEventListener(AUTH_FORCE_LOGIN_EVENT, forceLogin);
    return () => {
      window.removeEventListener(AUTH_SESSION_EVENT, syncSession);
      window.removeEventListener(AUTH_FORCE_LOGIN_EVENT, forceLogin);
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      accessToken,
      isAuthenticated: Boolean(accessToken),
      async login(username: string, password: string) {
        const response = await apiClient.login(username, password);
        applySession(response);
      },
      logout,
    }),
    [accessToken, applySession, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}



