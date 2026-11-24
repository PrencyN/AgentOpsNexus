import {
  InteractionRequiredAuthError,
  type AccountInfo,
} from "@azure/msal-browser";
import {
  MsalProvider,
} from "@azure/msal-react";
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import {
  apiScope,
  authenticationEnabled,
  loginRequest,
} from "./authConfig";
import {
  AuthContext,
  type AuthContextValue,
} from "./AuthContext";
import {
  msalInstance,
} from "./msalInstance";

function AuthStateProvider({
  children,
}: PropsWithChildren) {
  const [account, setAccount] =
    useState<AccountInfo | null>(
      () =>
        msalInstance
          .getActiveAccount() ??
        msalInstance
          .getAllAccounts()[0] ??
        null,
    );

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const refreshAccount =
    useCallback(() => {
      const nextAccount =
        msalInstance
          .getActiveAccount() ??
        msalInstance
          .getAllAccounts()[0] ??
        null;

      if (
        nextAccount &&
        !msalInstance
          .getActiveAccount()
      ) {
        msalInstance.setActiveAccount(
          nextAccount);
      }

      setAccount(nextAccount);
    }, []);

  useEffect(() => {
    if (!authenticationEnabled) {
      return;
    }

    refreshAccount();

    const callbackId =
      msalInstance.addEventCallback(
        () => {
          refreshAccount();
        },
      );

    return () => {
      if (callbackId) {
        msalInstance.removeEventCallback(
          callbackId);
      }
    };
  }, [refreshAccount]);

  const signIn =
    useCallback(async () => {
      if (!authenticationEnabled) {
        return;
      }

      setLoading(true);
      setError(null);

      try {
        await msalInstance.loginRedirect(
          loginRequest);
      } catch (caughtError) {
        setLoading(false);

        setError(
          caughtError instanceof Error
            ? caughtError.message
            : "Microsoft sign-in failed.",
        );
      }
    }, []);

  const signOut =
    useCallback(async () => {
      if (!authenticationEnabled) {
        return;
      }

      setLoading(true);
      setError(null);

      const currentAccount =
        msalInstance
          .getActiveAccount() ??
        account;

      try {
        await msalInstance.logoutRedirect({
          account:
            currentAccount ??
            undefined,
        });
      } catch (caughtError) {
        setLoading(false);

        setError(
          caughtError instanceof Error
            ? caughtError.message
            : "Microsoft sign-out failed.",
        );
      }
    }, [account]);

  const getAccessToken =
    useCallback(
      async (): Promise<
        string | null
      > => {
        if (!authenticationEnabled) {
          return null;
        }

        const currentAccount =
          msalInstance
            .getActiveAccount() ??
          account ??
          msalInstance
            .getAllAccounts()[0] ??
          null;

        if (!currentAccount) {
          return null;
        }

        try {
          const result =
            await msalInstance
              .acquireTokenSilent({
                account:
                  currentAccount,

                scopes: [
                  apiScope,
                ],
              });

          return result.accessToken;
        } catch (caughtError) {
          if (
            caughtError instanceof
              InteractionRequiredAuthError
          ) {
            await msalInstance
              .acquireTokenRedirect({
                account:
                  currentAccount,

                scopes: [
                  apiScope,
                ],
              });

            return null;
          }

          throw caughtError;
        }
      },
      [account],
    );

  const value =
    useMemo<AuthContextValue>(
      () => ({
        enabled:
          authenticationEnabled,

        authenticated:
          !authenticationEnabled ||
          account !== null,

        loading,

        account,

        error,

        signIn,

        signOut,

        getAccessToken,
      }),
      [
        account,
        error,
        getAccessToken,
        loading,
        signIn,
        signOut,
      ],
    );

  return (
    <AuthContext.Provider
      value={value}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function AuthProvider({
  children,
}: PropsWithChildren) {
  if (!authenticationEnabled) {
    return (
      <AuthStateProvider>
        {children}
      </AuthStateProvider>
    );
  }

  return (
    <MsalProvider
      instance={msalInstance}
    >
      <AuthStateProvider>
        {children}
      </AuthStateProvider>
    </MsalProvider>
  );
}