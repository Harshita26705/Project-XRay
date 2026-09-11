import type { Configuration } from '@azure/msal-browser';

export const AAD_CLIENT_ID = import.meta.env.VITE_AAD_CLIENT_ID as string | undefined;
export const AAD_TENANT_ID = import.meta.env.VITE_AAD_TENANT_ID as string | undefined;
export const AAD_API_SCOPE = (import.meta.env.VITE_AAD_API_SCOPE as string | undefined) ?? 'openid profile email';

/** True once a real Azure AD App Registration has been configured via .env.local (see README). */
export const isAadConfigured = Boolean(AAD_CLIENT_ID && AAD_TENANT_ID);

export const msalConfig: Configuration = {
  auth: {
    clientId: AAD_CLIENT_ID ?? '',
    authority: `https://login.microsoftonline.com/${AAD_TENANT_ID ?? 'common'}`,
    redirectUri: window.location.origin
  },
  cache: {
    cacheLocation: 'localStorage'
  }
};

export const loginRequest = {
  scopes: AAD_API_SCOPE.split(' ')
};
