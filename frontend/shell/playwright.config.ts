import {
  defineConfig,
  devices,
} from "@playwright/test";
import {
  dirname,
  resolve,
} from "node:path";
import {
  fileURLToPath,
} from "node:url";

const currentFilePath =
  fileURLToPath(
    import.meta.url);

const currentDirectory =
  dirname(
    currentFilePath);

const repositoryRoot =
  resolve(
    currentDirectory,
    "../..",
  );

const apiProjectPath =
  resolve(
    repositoryRoot,
    "services",
    "AgentOpsNexus.AgentApi",
    "AgentOpsNexus.AgentApi.csproj",
  );

const e2eDatabasePath =
  resolve(
    repositoryRoot,
    "services",
    "AgentOpsNexus.AgentApi",
    "App_Data",
    `agentops-e2e-${process.pid}.db`,
  );

const apiUrl =
  [
    "http",
    "://",
    "127.0.0.1",
    ":7001",
  ].join("");

const frontendUrl =
  [
    "http",
    "://",
    "127.0.0.1",
    ":5173",
  ].join("");

export default defineConfig({
  testDir:
    "./e2e",

  fullyParallel:
    false,

  workers:
    1,

  forbidOnly:
    Boolean(
      process.env.CI),

  retries:
    process.env.CI
      ? 2
      : 0,

  timeout:
    90_000,

  expect: {
    timeout:
      15_000,
  },

  reporter: [
    [
      "list",
    ],
    [
      "html",
      {
        open:
          "never",

        outputFolder:
          "playwright-report",
      },
    ],
  ],

  use: {
    baseURL:
      frontendUrl,

    trace:
      "retain-on-failure",

    screenshot:
      "only-on-failure",

    video:
      "retain-on-failure",

    actionTimeout:
      15_000,

    navigationTimeout:
      20_000,
  },

  projects: [
    {
      name:
        "chromium",

      use: {
        ...devices[
          "Desktop Chrome"
        ],
      },
    },
  ],

  webServer: [
    {
      command: [
        "dotnet",
        "run",
        "--project",
        `"${apiProjectPath}"`,
        "--no-launch-profile",
        "--urls",
        apiUrl,
      ].join(" "),

      url:
        `${apiUrl}/health`,

      env: {
        ...process.env,

        ASPNETCORE_ENVIRONMENT:
          "Testing",

        Authentication__Enabled:
          "false",

        Agent__Provider:
          "Mock",

        Agent__TimeoutSeconds:
          "30",

        ConnectionStrings__AgentOpsDatabase:
          `Data Source=${e2eDatabasePath}`,
      },

      timeout:
        120_000,

      reuseExistingServer:
        false,

      stdout:
        "pipe",

      stderr:
        "pipe",
    },
    {
      command:
        "npm run dev -- " +
        "--host 127.0.0.1 " +
        "--mode e2e",

      url:
        frontendUrl,

      env: {
        ...process.env,

        VITE_AUTH_ENABLED:
          "false",
      },

      timeout:
        120_000,

      reuseExistingServer:
        false,

      stdout:
        "pipe",

      stderr:
        "pipe",
    },
  ],
});