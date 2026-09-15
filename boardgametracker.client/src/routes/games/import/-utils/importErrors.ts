import { isApiError } from "@/models";

export type ImportErrorKind = "rate-limited" | "preparing" | "timeout" | "unauthorized" | "unknown";

export const classifyImportError = (error: unknown): ImportErrorKind => {
	if (!isApiError(error)) {
		return "unknown";
	}

	if (error.kind === "timeout") {
		return "timeout";
	}

	switch (error.status) {
		case 429:
			return "rate-limited";
		case 504:
			return "preparing";
		case 400:
		case 401:
		case 503:
			return "unauthorized";
		default:
			return "unknown";
	}
};
