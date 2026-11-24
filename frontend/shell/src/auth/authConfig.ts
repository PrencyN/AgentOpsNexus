import {
  BrowserCacheLocation,
  LogLevel,
  type Configuration,
  type RedirectRequest,
  type SilentRequest,
} from "@azure/msal-browser";

function readRequiredEnvironmentValue(
  name: keyof ImportMetaEnv,
): string {
  const value =
    import.meta.env[name];

  if (
    typeof value !== "string" ||
    value.trim().length === 0
  ) {
    throw new Error(
      `Missing required environment variable: ${name}`,
    );
  }

  return value.trim();
}

export const authenticationEnabled =
  import.meta.env.VITE_AUTH_ENABLED ===
  "true";

const tenantId =
  authenticationEnabled
    ? readRequiredEnvironmentValue(
        "VITE_ENTRA_TENANT_ID",
      )
    : "";

const clientId =
  authenticationEnabled
    ? readRequiredEnvironmentValue(
        "VITE_ENTRA_CLIENT_ID",
      )
    : "";

const authority =
  authenticationEnabled
    ? readRequiredEnvironmentValue(
        "VITE_ENTRA_AUTHORITY",
      )
    : "";

const redirectUri =
  authenticationEnabled
    ? readRequiredEnvironmentValue(
        "VITE_ENTRA_REDIRECT_URI",
      )
    : window.location.origin;

export const apiScope =
  authenticationEnabled
    ? readRequiredEnvironmentValue(
        "VITE_API_SCOPE",
      )
    : "";

export const msalConfiguration:
  Configuration = {
    auth: {
      clientId,
      authority,
      redirectUri,

      postLogoutRedirectUri:
        redirectUri,
    },

    cache: {
      cacheLocation:
        BrowserCacheLocation.SessionStorage,
    },

    system: {
      loggerOptions: {
        logLevel:
          LogLevel.Warning,

        piiLoggingEnabled:
          false,

        loggerCallback: (
          level,
          message,
          containsPii,
        ) => {
          if (containsPii) {
            return;
          }

          if (
            level === LogLevel.Error
          ) {
            console.error(
              message,
            );
          }
        },
      },
    },
  };

export const loginRequest:
  RedirectRequest = {
    scopes: [
      apiScope,
    ],

    prompt:
      "select_account",
  };

export const silentTokenRequest:
  Omit<SilentRequest, "account"> = {
    scopes: [
      apiScope,
    ],
  };

export const activeTenantId =
  tenantId;
