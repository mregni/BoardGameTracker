export const LANGUAGE_INDEPENDENT = "independent";

export const LANGUAGE_NONE = "none";

export const COMMON_LANGUAGE_CODES: string[] = [
	"en",
	"nl",
	"fr",
	"de",
	"es",
	"it",
	"pt",
	"pl",
	"sv",
	"da",
	"no",
	"fi",
	"cs",
	"sk",
	"hu",
	"ro",
	"el",
	"ru",
	"uk",
	"tr",
	"ja",
	"zh",
	"ko",
];

const displayNamesCache = new Map<string, Intl.DisplayNames>();

const getDisplayNames = (locale?: string): Intl.DisplayNames => {
	const key = locale ?? "";
	let displayNames = displayNamesCache.get(key);
	if (!displayNames) {
		displayNames = new Intl.DisplayNames(locale ? [locale] : undefined, { type: "language" });
		displayNamesCache.set(key, displayNames);
	}
	return displayNames;
};

export const getLanguageName = (code: string, locale?: string): string => {
	try {
		return getDisplayNames(locale).of(code) ?? code;
	} catch {
		return code;
	}
};
