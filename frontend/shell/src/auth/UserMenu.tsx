import {
  Button,
} from "@fluentui/react-components";
import {
  SignOut20Regular,
} from "@fluentui/react-icons";
import {
  useAuth,
} from "./AuthContext";

export function UserMenu() {
  const {
    enabled,
    account,
    loading,
    signOut,
  } = useAuth();

  if (!enabled || !account) {
    return null;
  }

  const displayName =
    account.name ??
    account.username;

  return (
    <div className="user-menu">
      <div className="user-identity">
        <span className="user-name">
          {displayName}
        </span>

        <span className="user-account">
          {account.username}
        </span>
      </div>

      <Button
        appearance="subtle"
        icon={
          <SignOut20Regular />
        }
        disabled={loading}
        onClick={() =>
          void signOut()
        }
      >
        Sign out
      </Button>
    </div>
  );
}
