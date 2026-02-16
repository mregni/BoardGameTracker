export type ApiErrorKind = "network" | "timeout" | "server" | "client" | "unknown";

export interface ApiError {
	kind: ApiErrorKind;
	status: number | null;
	message: string;
	url: string | undefined;
}

export function isApiError(error: unknown): error is ApiError {
	return typeof error === "object" && error !== null && "kind" in error && "status" in error && "message" in error;
}
