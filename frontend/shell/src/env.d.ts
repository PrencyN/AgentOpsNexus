/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_AUTH_ENABLED?:
    string;

  readonly VITE_ENTRA_TENANT_ID?:
    string;

  readonly VITE_ENTRA_CLIENT_ID?:
    string;

  readonly VITE_ENTRA_AUTHORITY?:
    string;

  readonly VITE_ENTRA_REDIRECT_URI?:
    string;

  readonly VITE_API_SCOPE?:
    string;

  readonly VITE_API_BASE_URL?:
    string;
}

interface ImportMeta {
  readonly env:
    ImportMetaEnv;
}
