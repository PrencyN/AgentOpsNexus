import {
  InteractionRequiredAuthError,
} from "@azure/msal-browser";
import {
  apiScope,
  authenticationEnabled,
} from "./authConfig";
import {
  msalInstance,
} from "./msalInstance";

export async function
  getApiAccessToken():
  Promise<string | null> {
  if (!authenticationEnabled) {
    return null;
  }

  const account =
    msalInstance.getActiveAccount() ??
    msalInstance.getAllAccounts()[0] ??
    null;

  if (!account) {
    return null;
  }

  if (
    !msalInstance.getActiveAccount()
  ) {
    msalInstance.setActiveAccount(
      account);
  }

  try {
    const result =
      await msalInstance
        .acquireTokenSilent({
          account,
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
          account,
          scopes: [
            apiScope,
          ],
        });

      return null;
    }

    throw caughtError;
  }
}