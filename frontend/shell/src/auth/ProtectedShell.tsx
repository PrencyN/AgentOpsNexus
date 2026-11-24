import {
  Button,
  Card,
  Spinner,
} from "@fluentui/react-components";
import type {
  PropsWithChildren,
} from "react";
import {
  useAuth,
} from "./AuthContext";

export function ProtectedShell({
  children,
}: PropsWithChildren) {
  const {
    enabled,
    authenticated,
    loading,
    error,
    signIn,
  } = useAuth();

  if (!enabled) {
    return <>{children}</>;
  }

  if (authenticated) {
    return <>{children}</>;
  }

  return (
    <main
      className="auth-page"
      aria-labelledby="auth-title"
    >
      <Card className="auth-card">
        <span className="product-mark">
          AN
        </span>

        <div>
          <span className="eyebrow">
            AGENTOPS NEXUS AI
          </span>

          <h1 id="auth-title">
            Sign in to investigate incidents
          </h1>
        </div>

        <p>
          Use the Microsoft Entra account
          assigned to AgentOps Nexus. Access
          to investigations is controlled by
          API roles and request ownership.
        </p>

        {error && (
          <div
            className="auth-error"
            role="alert"
          >
            {error}
          </div>
        )}

        <Button
          appearance="primary"
          size="large"
          disabled={loading}
          onClick={() =>
            void signIn()
          }
        >
          {loading
            ? "Opening Microsoft sign-in..."
            : "Sign in with Microsoft"}
        </Button>

        {loading && (
          <Spinner
            size="small"
            label="Preparing sign-in"
          />
        )}
      </Card>
    </main>
  );
}
