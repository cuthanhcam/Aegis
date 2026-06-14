import { AegisApiClient } from '@aegis/api-client';

const API_BASE_URL = import.meta.env.VITE_AEGIS_API_BASE_URL;
if (!API_BASE_URL) {
  throw new Error('VITE_AEGIS_API_BASE_URL is required.');
}
const TOKEN_KEY = 'aegis.accessToken';
export const AUTH_SESSION_EVENT = 'aegis-auth-session-changed';
export const AUTH_FORCE_LOGIN_EVENT = 'aegis-auth-force-login';

function emitAuthSessionChanged() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event(AUTH_SESSION_EVENT));
  }
}

function emitAuthForceLogin() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event(AUTH_FORCE_LOGIN_EVENT));
  }
}

export const tokenStorage = {
  getAccessToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },
  setAccessToken(token: string) {
    localStorage.setItem(TOKEN_KEY, token);
  },
  setSession(session: { accessToken: string; refreshToken?: string | null; expiresIn: number }) {
    localStorage.setItem(TOKEN_KEY, session.accessToken);
    emitAuthSessionChanged();
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY);
    emitAuthSessionChanged();
  },
};

let refreshInFlight: Promise<boolean> | null = null;

export const apiClient = new AegisApiClient({
  baseUrl: API_BASE_URL,
  includeCredentials: true,
  getAccessToken: () => tokenStorage.getAccessToken(),
  refreshAccessToken: async () => {
    if (refreshInFlight) {
      return refreshInFlight;
    }

    refreshInFlight = (async () => {
      try {
        const response = await apiClient.refresh();
        tokenStorage.setSession(response);
        return true;
      } catch {
        tokenStorage.clear();
        emitAuthForceLogin();
        return false;
      } finally {
        refreshInFlight = null;
      }
    })();

    return refreshInFlight;
  },
});


