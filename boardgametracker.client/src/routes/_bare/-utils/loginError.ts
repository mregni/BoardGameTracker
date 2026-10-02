import { isApiError } from "@/models";

export const loginErrorKey = (error: unknown): string | null => {
	if (!isApiError(error)) {
		return null;
	}

	if (error.status === 429) {
		return "error:auth.too-many-requests";
	}

	if (error.kind === "network" || error.kind === "timeout" || error.kind === "server") {
		return `error:${error.kind}`;
	}

	return error.message.startsWith("error.") ? `error:${error.message.slice("error.".length)}` : null;
};
