import {
  createContext,
  useContext,
} from "react";
import type {
  AccountInfo,
} from "@azure/msal-browser";

export interface AuthContextValue {
  enabled: boolean;
  authenticated: boolean;
  loading: boolean;
  account: AccountInfo | null;
  error: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
  getAccessToken:
    () => Promise<string | null>;
}

export const AuthContext =
  createContext<
    AuthContextValue | undefined
  >(undefined);

export function useAuth():
  AuthContextValue {
  const context =
    useContext(AuthContext);

  if (!context) {
    throw new Error(
      "useAuth must be used inside " +
      "AuthProvider.",
    );
  }

  return context;
}
