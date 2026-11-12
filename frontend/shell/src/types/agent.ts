export type EnvironmentName =
  | "Development"
  | "Test"
  | "Staging"
  | "Production";

export type Severity =
  | "Low"
  | "Medium"
  | "High"
  | "Critical";

export interface CreateAgentRequest {
  message: string;
  environment: EnvironmentName;
  severity: Severity;
  simulateFailure: boolean;
}

export interface RequestAcceptedResponse {
  requestId: string;
  status: string;
  createdAt: string;
  streamUrl: string;
}

export interface RequestDetails {
  requestId: string;
  message: string;
  environment: EnvironmentName;
  severity: Severity;
  status: string;
  response?: string;
  errorCode?: string;
  retryable: boolean;
  attemptNumber: number;
  retriedFromRequestId?: string;
  createdAt: string;
  completedAt?: string;
}

export interface AgentStreamEvent {
  type: string;
  message: string;
  timestamp: string;
}

export interface ApiError {
  title: string;
  detail: string;
  status: number;
  code?: string;
  retryable?: boolean;
  errors?: Record<string, string[]>;
}