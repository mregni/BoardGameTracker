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

export const apiErrorMessage = (error: unknown, fallbackKey: string): string =>
	isApiError(error) && error.kind === "client" ? translateApiError(error.message, fallbackKey) : i18n.t(fallbackKey);
