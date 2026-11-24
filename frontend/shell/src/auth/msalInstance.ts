import {
  EventType,
  PublicClientApplication,
  type AccountInfo,
  type AuthenticationResult,
} from "@azure/msal-browser";
import {
  authenticationEnabled,
  msalConfiguration,
} from "./authConfig";

export const msalInstance =
  new PublicClientApplication(
    msalConfiguration,
  );

function selectAccount(
  result?: AuthenticationResult | null,
): AccountInfo | null {
  if (result?.account) {
    return result.account;
  }

  const activeAccount =
    msalInstance.getActiveAccount();

  if (activeAccount) {
    return activeAccount;
  }

  return (
    msalInstance.getAllAccounts()[0] ??
    null
  );
}

export async function
  initializeMsal(): Promise<void> {
  if (!authenticationEnabled) {
    return;
  }

  await msalInstance.initialize();

  const redirectResult =
    await msalInstance
      .handleRedirectPromise();

  const account =
    selectAccount(
      redirectResult);

  if (account) {
    msalInstance.setActiveAccount(
      account);
  }

  msalInstance.addEventCallback(
    (event) => {
      if (
        event.eventType !==
          EventType.LOGIN_SUCCESS ||
        !event.payload
      ) {
        return;
      }

      const result =
        event.payload as
          AuthenticationResult;

      if (result.account) {
        msalInstance
          .setActiveAccount(
            result.account);
      }
    },
  );
}
