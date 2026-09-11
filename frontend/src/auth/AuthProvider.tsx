import React, { createContext, useContext, useMemo, useState, useCallback } from 'react';
import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider, useMsal, useIsAuthenticated } from '@azure/msal-react';
import { isAadConfigured, loginRequest, msalConfig } from './config';

interface AuthContextValue {
  isAuthenticated: boolean;
  displayName: string;
  isDevBypass: boolean;
  login: () => void;
  logout: () => void;
  getAccessToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const DEV_BYPASS_KEY = 'xray_dev_bypass_signed_in';

/**
 * Real Microsoft Entra ID auth via MSAL when VITE_AAD_CLIENT_ID/VITE_AAD_TENANT_ID are configured.
 * Otherwise falls back to a local "dev bypass" (mirrors the backend's DevBypassAuthHandler) so the
 * product can be demoed before an App Registration exists — see documents/setup-guide.md.
 */
function DevBypassAuthProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => localStorage.getItem(DEV_BYPASS_KEY) === '1');

  const login = useCallback(() => {
    localStorage.setItem(DEV_BYPASS_KEY, '1');
    setIsAuthenticated(true);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(DEV_BYPASS_KEY);
    setIsAuthenticated(false);
  }, []);

  const getAccessToken = useCallback(async () => null, []);

  const value = useMemo<AuthContextValue>(
    () => ({ isAuthenticated, displayName: 'Harshita S.', isDevBypass: true, login, logout, getAccessToken }),
    [isAuthenticated, login, logout, getAccessToken]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function MsalBackedAuthProvider({ children }: { children: React.ReactNode }) {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const login = useCallback(() => {
    void instance.loginRedirect(loginRequest);
  }, [instance]);

  const logout = useCallback(() => {
    void instance.logoutRedirect();
  }, [instance]);

  const getAccessToken = useCallback(async () => {
    if (accounts.length === 0) return null;
    try {
      const result = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
      return result.accessToken;
    } catch {
      return null;
    }
  }, [instance, accounts]);

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated,
      displayName: accounts[0]?.name ?? accounts[0]?.username ?? 'User',
      isDevBypass: false,
      login,
      logout,
      getAccessToken
    }),
    [isAuthenticated, accounts, login, logout, getAccessToken]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  if (!isAadConfigured) {
    return <DevBypassAuthProvider>{children}</DevBypassAuthProvider>;
  }
  const pca = new PublicClientApplication(msalConfig);
  return (
    <MsalProvider instance={pca}>
      <MsalBackedAuthProvider>{children}</MsalBackedAuthProvider>
    </MsalProvider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
