export const MIN_SESSIONS_FOR_AVERAGE = 3;

export type NumberValue = number | null | undefined;

export const EMPTY_NUMBER = "-";

export const EMPTY_PRICE = "–";

const DEFAULT_MAX_FRACTION_DIGITS = 3;

const formatterCache = new Map<string, Intl.NumberFormat>();

const isMissing = (value: NumberValue): value is null | undefined => typeof value !== "number" || Number.isNaN(value);

const createFormatter = (locale: string | undefined, options: Intl.NumberFormatOptions): Intl.NumberFormat => {
	try {
		return new Intl.NumberFormat(locale, options);
	} catch {
		return new Intl.NumberFormat("en-US", options);
	}
};

const getFormatter = (locale: string | null | undefined, options: Intl.NumberFormatOptions): Intl.NumberFormat => {
	const resolved = locale?.trim() || undefined;
	const key = `${resolved ?? ""}|${JSON.stringify(options)}`;
	let formatter = formatterCache.get(key);
	if (!formatter) {
		formatter = createFormatter(resolved, options);
		formatterCache.set(key, formatter);
	}
	return formatter;
};

const roundsToZero = (value: number, maximumFractionDigits: number): boolean =>
	Math.round(Math.abs(value) * 10 ** maximumFractionDigits) === 0;

export const formatNumber = (
	value: NumberValue,
	locale?: string | null,
	options: Intl.NumberFormatOptions = {},
): string => {
	if (isMissing(value)) {
		return EMPTY_NUMBER;
	}

	const maximumFractionDigits =
		options.maximumFractionDigits ?? (options.style === "percent" ? 0 : DEFAULT_MAX_FRACTION_DIGITS);
	const scaled = options.style === "percent" ? value * 100 : value;

	return getFormatter(locale, options).format(roundsToZero(scaled, maximumFractionDigits) ? 0 : value);
};

export const formatDecimal = (
	value: NumberValue,
	locale?: string | null,
	maximumFractionDigits = 1,
	minimumFractionDigits = 0,
): string => formatNumber(value, locale, { maximumFractionDigits, minimumFractionDigits });

export const formatInteger = (value: NumberValue, locale?: string | null): string =>
	formatNumber(value, locale, { maximumFractionDigits: 0 });

export const formatPercent = (fraction: NumberValue, locale?: string | null, maximumFractionDigits = 0): string =>
	formatNumber(fraction, locale, { style: "percent", maximumFractionDigits });

export const formatRatio = (value: number, total: number, locale?: string | null, maximumFractionDigits = 0): string =>
	formatPercent(total === 0 ? 0 : value / total, locale, maximumFractionDigits);

const SYMBOL_CURRENCIES: Record<string, string> = {
	"€": "EUR",
	$: "USD",
	"£": "GBP",
	"¥": "JPY",
	"₹": "INR",
	"₩": "KRW",
	"₺": "TRY",
	"₽": "RUB",
	R$: "BRL",
	zł: "PLN",
};

export const toIsoCurrency = (currency: string | null | undefined): string | null => {
	const value = currency?.trim() ?? "";
	const mapped = SYMBOL_CURRENCIES[value];
	if (mapped) {
		return mapped;
	}
	return /^[A-Za-z]{3}$/.test(value) ? value.toUpperCase() : null;
};

export const formatPriceOrEmpty = (
	amount: NumberValue,
	currency: string | null | undefined,
	locale?: string | null,
): string => (isMissing(amount) || amount === 0 ? EMPTY_PRICE : formatPrice(amount, currency, locale));

export const formatPrice = (
	amount: NumberValue,
	currency: string | null | undefined,
	locale?: string | null,
): string => {
	if (isMissing(amount)) {
		return EMPTY_NUMBER;
	}

	const fractionDigits = Number.isInteger(Math.round(amount * 100) / 100) ? 0 : 2;
	const digits = { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits };

	const isoCode = toIsoCurrency(currency);
	if (isoCode) {
		return formatNumber(amount, locale, {
			...digits,
			style: "currency",
			currency: isoCode,
			currencyDisplay: "narrowSymbol",
		});
	}

	const formatted = formatNumber(amount, locale, digits);
	const prefix = currency?.trim() ?? "";
	if (prefix.length === 0) {
		return formatted;
	}

	return prefix.length > 1 ? `${prefix} ${formatted}` : `${prefix}${formatted}`;
};

export const formatFileSize = (bytes: number, locale?: string | null): string => {
	if (bytes < 1024) {
		return `${formatInteger(bytes, locale)} B`;
	}

	const kilobytes = bytes / 1024;
	if (kilobytes < 1024) {
		return `${formatDecimal(kilobytes, locale, 1, 1)} KB`;
	}

	return `${formatDecimal(kilobytes / 1024, locale, 1, 1)} MB`;
};

export const formatDecimalInput = (value: NumberValue, locale?: string | null): string =>
	isMissing(value) ? "" : formatNumber(value, locale, { useGrouping: false, maximumFractionDigits: 10 });

const localeDecimalSeparator = (locale: string) =>
	new Intl.NumberFormat(locale).formatToParts(1.1).find((part) => part.type === "decimal")?.value;

export const parseDecimal = (input: string | null | undefined, locale?: string | null): number | null => {
	const compact = (input ?? "").replace(/[\s  ']/g, "");
	if (compact === "") {
		return null;
	}

	const lastComma = compact.lastIndexOf(",");
	const lastDot = compact.lastIndexOf(".");
	const lastIndex = Math.max(lastComma, lastDot);
	const isGrouping =
		(lastComma < 0 || lastDot < 0) &&
		!!locale &&
		compact[lastIndex] !== localeDecimalSeparator(locale) &&
		/^[+-]?\d{1,3}(,\d{3})+$/.test(compact.replaceAll(".", ","));
	const decimalIndex = isGrouping ? -1 : lastIndex;
	const normalized =
		decimalIndex < 0
			? compact.replace(/[.,]/g, "")
			: `${compact.slice(0, decimalIndex).replace(/[.,]/g, "")}.${compact.slice(decimalIndex + 1)}`;

	if (!/^[+-]?(\d+\.?\d*|\.\d+)$/.test(normalized)) {
		return null;
	}

	const parsed = Number(normalized);
	return Number.isFinite(parsed) ? parsed : null;
};

export const getIntegerTicks = (maxValue: number, maxTicks = 5): number[] => {
	const top = Math.max(1, Math.ceil(maxValue));
	const step = Math.max(1, Math.ceil(top / maxTicks));
	const ticks: number[] = [];
	for (let tick = 0; tick < top + step; tick += step) {
		ticks.push(tick);
	}
	return ticks;
};
