import { de, enUS, es, fr, it, type Locale, nl } from "date-fns/locale";

const locales = { enUS, nl, fr, de, es, it } satisfies Record<string, Locale>;

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

type DateOrder = "dmy" | "mdy" | "ymd" | "ydm" | "myd" | "dym";

const ORDER_FALLBACKS: Record<DateOrder, string[]> = {
	dmy: ["en-GB", "nl-NL", "de-DE"],
	mdy: ["en-US"],
	ymd: ["en-CA", "sv-SE"],
	ydm: ["en-CA"],
	myd: ["en-US"],
	dym: ["en-GB"],
};

const REGION_CANDIDATES = ["US", "GB", "CA", "NL", "BE", "ES", "MX", "DE", "SE", "AU", "IE", "ZA"];

const ORDER_PROBE = new Date(2000, 0, 2);

const getFormatOrder = (dateFormat: string): DateOrder | null => {
	const positions = (["d", "m", "y"] as const)
		.map((token) => ({ token, index: dateFormat.toLowerCase().indexOf(token) }))
		.filter((entry) => entry.index >= 0)
		.sort((a, b) => a.index - b.index);

	if (positions.length !== 3) {
		return null;
	}

	return positions.map((entry) => entry.token).join("") as DateOrder;
};

const getLocaleOrder = (locale: string): string | null => {
	try {
		return new Intl.DateTimeFormat(locale, { year: "numeric", month: "2-digit", day: "2-digit" })
			.formatToParts(ORDER_PROBE)
			.filter((part) => part.type === "day" || part.type === "month" || part.type === "year")
			.map((part) => part.type[0])
			.join("");
	} catch {
		return null;
	}
};

const getBrowserLocale = (): string =>
	typeof navigator !== "undefined" && navigator.language ? navigator.language : "en-US";

export const getDatePickerLocale = (dateFormat?: string | null, uiLanguage?: string | null): string => {
	const language = uiLanguage?.trim() || getBrowserLocale();
	const order = dateFormat ? getFormatOrder(dateFormat) : null;

	if (!order) {
		return language;
	}

	const base = language.split("-")[0].toLowerCase();
	const candidates = [language, ...REGION_CANDIDATES.map((region) => `${base}-${region}`), ...ORDER_FALLBACKS[order]];

	return candidates.find((candidate) => getLocaleOrder(candidate) === order) ?? language;
};

export const getDateSeparator = (dateFormat?: string | null): string | null => {
	if (!dateFormat) {
		return null;
	}

	const match = /[dmy]+([^a-z]+)[dmy]+/i.exec(dateFormat);
	const separator = match?.[1].trim();
	return separator ? separator : null;
};

export const shouldForceLeadingZeros = (dateFormat?: string | null): boolean =>
	!!dateFormat && /dd|mm/i.test(dateFormat);
