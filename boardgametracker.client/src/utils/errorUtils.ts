import { isApiError } from "@/models";
import i18n from "@/utils/i18n";

const REASON_PREFIX = "error.";

export const translateApiError = (message: string | undefined, fallbackKey = "error:something-went-wrong"): string => {
	const fallback = i18n.t(fallbackKey);
	if (!message) {
		return fallback;
	}

	if (message.startsWith(REASON_PREFIX)) {
		return i18n.t(`error:${message.slice(REASON_PREFIX.length)}`, { defaultValue: fallback });
	}

	return message;
};

export const translateUrlError = (value: string | undefined, fallbackKey = "error:auth.oidc-failed"): string => {
	const key = value?.startsWith(REASON_PREFIX) ? `error:${value.slice(REASON_PREFIX.length)}` : null;
	return key && i18n.exists(key) ? i18n.t(key) : i18n.t(fallbackKey);
};

export const apiErrorMessage = (error: unknown, fallbackKey: string): string =>
	isApiError(error) && error.kind === "client" ? translateApiError(error.message, fallbackKey) : i18n.t(fallbackKey);
