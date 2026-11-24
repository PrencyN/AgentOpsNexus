import {
  StrictMode,
} from "react";
import {
  createRoot,
} from "react-dom/client";
import App from "./App";
import {
  AuthProvider,
} from "./auth/AuthProvider";
import {
  ProtectedShell,
} from "./auth/ProtectedShell";
import {
  authenticationEnabled,
} from "./auth/authConfig";
import {
  initializeMsal,
} from "./auth/msalInstance";
import "./index.css";

async function bootstrap():
  Promise<void> {
  if (authenticationEnabled) {
    await initializeMsal();
  }

  const rootElement =
    document.getElementById(
      "root");

  if (!rootElement) {
    throw new Error(
      "Application root element was not found.",
    );
  }

  createRoot(rootElement).render(
    <StrictMode>
      <AuthProvider>
        <ProtectedShell>
          <App />
        </ProtectedShell>
      </AuthProvider>
    </StrictMode>,
  );
}

void bootstrap();
