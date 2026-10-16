export type IntegrationKind = "bgg" | "changeDetection";

export type IntegrationConnectionStatus = "ok" | "notConfigured" | "unauthorized" | "unreachable" | "rateLimited";

export interface IntegrationStatus {
	integration: IntegrationKind;
	status: IntegrationConnectionStatus;
	checkedAt: string;
	messageKey: string;
}

export const isIntegrationFailure = (status: IntegrationStatus | undefined): status is IntegrationStatus =>
	status !== undefined && status.status !== "ok" && status.status !== "notConfigured";
