import { isApiError } from "@/models";
import i18n from "@/utils/i18n";

const REASON_PREFIX = "error.";

const SERVER_TEXT_KEYS: Record<string, string> = {
	"Incorrect password.": "error:auth.incorrect-password",
	"The requested resource was not found.": "error:not-found",
	"The resource was modified by another request. Please retry.": "error:conflict",
	"Invalid BGG API key. Please check your API key in settings.": "error:bgg.invalid-api-key",
};

const NO_REASON_TEXTS = new Set([
	"An unexpected error occurred",
	"Bad Request",
	"Conflict",
	"Forbidden",
	"Invalid request.",
	"Not Found",
	"One or more validation errors occurred.",
	"The request contains invalid data.",
	"The request references data that does not exist or conflicts with existing data.",
	"Too Many Requests",
	"Unauthorized",
]);

export const translateApiError = (message: string | undefined, fallbackKey = "error:something-went-wrong"): string => {
	const fallback = i18n.t(fallbackKey);
	if (!message || NO_REASON_TEXTS.has(message)) {
		return fallback;
	}

	if (message.startsWith(REASON_PREFIX)) {
		return i18n.t(`error:${message.slice(REASON_PREFIX.length)}`, { defaultValue: fallback });
	}

	return message in SERVER_TEXT_KEYS ? i18n.t(SERVER_TEXT_KEYS[message]) : message;
};

export const translateUrlError = (value: string | undefined, fallbackKey = "error:auth.oidc-failed"): string => {
	const key = value?.startsWith(REASON_PREFIX) ? `error:${value.slice(REASON_PREFIX.length)}` : null;
	return key && i18n.exists(key) ? i18n.t(key) : i18n.t(fallbackKey);
};

export const apiErrorMessage = (error: unknown, fallbackKey: string): string =>
	isApiError(error) && error.kind === "client" ? translateApiError(error.message, fallbackKey) : i18n.t(fallbackKey);
