import type { Locale } from "date-fns/locale";
import * as locales from "date-fns/locale";

export const getDateFnsLocaleKey = (languageCode: string): keyof typeof locales => {
	const mapping: Record<string, keyof typeof locales> = {
		"en-us": "enUS",
		en: "enUS",
		english: "enUS",
		"nl-nl": "nl",
		"nl-be": "nl",
		nl: "nl",
		dutch: "nl",
		"fr-fr": "fr",
		fr: "fr",
		"de-de": "de",
		de: "de",
		"es-es": "es",
		es: "es",
		"it-it": "it",
		it: "it",
	};
	return mapping[languageCode.toLowerCase()] ?? "enUS";
};

export const getDateFnsLocale = (languageCode: string): Locale => {
	const localeKey = getDateFnsLocaleKey(languageCode);
	return locales[localeKey];
};

/**
 * Resolves a BCP-47 locale tag suitable for react-aria's date components.
 *
 * Precedence:
 *   1. If `dateFormat` matches a known format, return a locale that produces that format
 *      (e.g. `dd-MM-yyyy` → `nl-NL`, `yyyy-MM-dd` → `sv-SE`).
 *   2. Otherwise fall back to the browser's locale (`navigator.language`), defaulting to
 *      `en-US` on environments where it is unavailable (SSR/tests).
 */
export const getDatePickerLocale = (dateFormat?: string | null): string => {
	const browserLocale = typeof navigator !== "undefined" && navigator.language ? navigator.language : "en-US";

	if (!dateFormat) {
		return browserLocale;
	}

	const normalized = dateFormat.toLowerCase().trim();
	const mapping: Record<string, string> = {
		"yyyy-mm-dd": "sv-SE",
		"dd-mm-yyyy": "nl-NL",
		"dd/mm/yyyy": "en-GB",
		"dd.mm.yyyy": "de-DE",
		"mm/dd/yyyy": "en-US",
	};

	return mapping[normalized] ?? browserLocale;
};
