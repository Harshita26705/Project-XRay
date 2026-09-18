import React, { createContext, useContext, useMemo, useState, useCallback, useEffect } from 'react';
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
    () => ({ isAuthenticated, displayName: 'Sayyed Amaan Ali', isDevBypass: true, login, logout, getAccessToken }),
    [isAuthenticated, login, logout, getAccessToken]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function MsalBackedAuthProvider({ children }: { children: React.ReactNode }) {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    let active = true;
    void instance.handleRedirectPromise()
      .then((result) => {
        if (!active) return;
        if (result?.account) instance.setActiveAccount(result.account);
        else if (!instance.getActiveAccount() && instance.getAllAccounts()[0]) instance.setActiveAccount(instance.getAllAccounts()[0]);
      })
      .finally(() => {
        if (active) setIsReady(true);
      });
    return () => { active = false; };
  }, [instance]);

  const login = useCallback(() => {
    void instance.loginRedirect(loginRequest);
  }, [instance]);

  const logout = useCallback(() => {
    void instance.logoutRedirect({ postLogoutRedirectUri: `${window.location.origin}/login` });
  }, [instance]);

  const getAccessToken = useCallback(async () => {
    const account = instance.getActiveAccount() ?? accounts[0];
    if (!account) return null;
    try {
      const result = await instance.acquireTokenSilent({ ...loginRequest, account });
      return result.accessToken;
    } catch (error) {
      if (error instanceof Error && error.name === 'InteractionRequiredAuthError') {
        await instance.acquireTokenRedirect({ ...loginRequest, account });
      }
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

  if (!isReady) return <div className="flex h-screen items-center justify-center bg-page text-sm text-text-muted">Connecting to Microsoft...</div>;
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  if (!isAadConfigured) {
    if (import.meta.env.PROD) {
      // eslint-disable-next-line no-console
      console.error('Azure AD is not configured in a production build — falling back to the local dev-bypass auth. This must not be used in a real deployment.');
    }
    return <DevBypassAuthProvider>{children}</DevBypassAuthProvider>;
  }
  const pca = useMemo(() => new PublicClientApplication(msalConfig), []);
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
