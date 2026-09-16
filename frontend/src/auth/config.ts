import type { Configuration } from '@azure/msal-browser';

export const AAD_CLIENT_ID = import.meta.env.VITE_AAD_CLIENT_ID as string | undefined;
export const AAD_TENANT_ID = import.meta.env.VITE_AAD_TENANT_ID as string | undefined;
export const AAD_API_SCOPE = import.meta.env.VITE_AAD_API_SCOPE as string | undefined;

/** True once a real Azure AD App Registration has been configured via .env.local (see README). */
const isRealValue = (value: string | undefined) => Boolean(value && !value.startsWith('<') && !value.includes('REPLACE-WITH-YOUR-'));
export const isAadConfigured = isRealValue(AAD_CLIENT_ID) && isRealValue(AAD_TENANT_ID) && isRealValue(AAD_API_SCOPE);

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
  scopes: AAD_API_SCOPE ? [AAD_API_SCOPE] : ['openid', 'profile', 'email']
};
