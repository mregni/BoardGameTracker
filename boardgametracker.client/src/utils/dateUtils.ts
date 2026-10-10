import {
	isValid as dateFnsIsValid,
	differenceInDays,
	format,
	formatDistanceToNow,
	formatDuration,
	parseISO,
} from "date-fns";

import { getDateFnsLocale } from "./localeUtils";

type DateInput = Date | string | undefined;

export const DEFAULT_DATE_FORMAT = "dd-MM-yyyy";
export const DEFAULT_TIME_FORMAT = "HH:mm";

const FORMAT_PROBE = new Date(2000, 0, 15, 13, 45, 30);

const convertDateFormat = (userFormat: string): string => {
	return userFormat.replace(/Y/g, "y").replace(/D/g, "d");
};

const canFormat = (pattern: string): boolean => {
	if (!pattern.trim()) {
		return false;
	}

	try {
		format(FORMAT_PROBE, pattern);
		return true;
	} catch {
		return false;
	}
};

export const normalizeDateFnsFormat = (pattern: string | null | undefined, fallback: string): string => {
	if (!pattern) {
		return fallback;
	}

	if (canFormat(pattern)) {
		return pattern;
	}

	const converted = convertDateFormat(pattern);
	return canFormat(converted) ? converted : fallback;
};

export const toInputDate = (date: DateInput, fallbackToToday = true): string => {
	if (!date && fallbackToToday) {
		return format(new Date(), "yyyy-MM-dd");
	}

	if (!date) {
		return "";
	}

	const dateObj = typeof date === "string" ? parseISO(date) : date;

	if (!dateFnsIsValid(dateObj)) {
		return fallbackToToday ? format(new Date(), "yyyy-MM-dd") : "";
	}

	return format(dateObj, "yyyy-MM-dd");
};

export const toInputDateTime = (date: Date | string | undefined, fallbackToNow = true): string => {
	if (!date && fallbackToNow) {
		return format(new Date(), "yyyy-MM-dd'T'HH:mm");
	}

	if (!date) {
		return "";
	}

	const dateObj = typeof date === "string" ? parseISO(date) : date;

	if (!dateFnsIsValid(dateObj)) {
		return fallbackToNow ? format(new Date(), "yyyy-MM-dd'T'HH:mm") : "";
	}

	return format(dateObj, "yyyy-MM-dd'T'HH:mm");
};

export const toDisplay = (date: Date | string | undefined | null, dateFormat: string, uiLanguage: string): string => {
	if (!date) {
		return "";
	}

	const dateObj = typeof date === "string" ? parseISO(date) : date;

	if (!dateFnsIsValid(dateObj)) {
		return "";
	}

	const locale = getDateFnsLocale(uiLanguage);
	const dateFnsFormat = convertDateFormat(dateFormat);
	return format(dateObj, dateFnsFormat, { locale });
};

export const toDisplayDateTime = (
	date: Date | string | undefined,
	dateFormat: string,
	timeFormat: string,
	uiLanguage: string,
): string => {
	if (!date) {
		return "";
	}

	const dateObj = typeof date === "string" ? parseISO(date) : date;

	if (!dateFnsIsValid(dateObj)) {
		return "";
	}

	const locale = getDateFnsLocale(uiLanguage);
	const dateFnsDateFormat = convertDateFormat(dateFormat);
	const combinedFormat = `${dateFnsDateFormat} ${timeFormat}`;

	return format(dateObj, combinedFormat, { locale });
};

export const toRelative = (
	date: Date | string | undefined,
	uiLanguage: string,
	options?: { includeSeconds?: boolean },
): string => {
	if (!date) {
		return "";
	}

	const dateObj = typeof date === "string" ? parseISO(date) : date;

	if (!dateFnsIsValid(dateObj)) {
		return "";
	}

	const locale = getDateFnsLocale(uiLanguage);
	return formatDistanceToNow(dateObj, {
		locale,
		includeSeconds: options?.includeSeconds,
		addSuffix: true,
	});
};

export const isValidDate = (date: unknown): boolean => {
	if (!date) {
		return false;
	}

	if (date instanceof Date) {
		return dateFnsIsValid(date);
	}

	if (typeof date === "string") {
		const parsed = parseISO(date);
		return dateFnsIsValid(parsed);
	}

	return false;
};

export const safeParseDate = (date: Date | string | undefined): Date | undefined => {
	if (!date) {
		return undefined;
	}

	if (date instanceof Date) {
		return dateFnsIsValid(date) ? date : undefined;
	}

	const parsed = parseISO(date);
	return dateFnsIsValid(parsed) ? parsed : undefined;
};

export const minutesToDuration = (
	totalMinutes: number,
): { weeks: number; days: number; hours: number; minutes: number } => {
	const MINUTES_PER_WEEK = 7 * 24 * 60;
	const MINUTES_PER_DAY = 24 * 60;
	const MINUTES_PER_HOUR = 60;

	const weeks = Math.floor(totalMinutes / MINUTES_PER_WEEK);
	const remainingAfterWeeks = totalMinutes % MINUTES_PER_WEEK;

	const days = Math.floor(remainingAfterWeeks / MINUTES_PER_DAY);
	const remainingAfterDays = remainingAfterWeeks % MINUTES_PER_DAY;

	const hours = Math.floor(remainingAfterDays / MINUTES_PER_HOUR);
	const minutes = Math.round(remainingAfterDays % MINUTES_PER_HOUR);

	return { weeks, days, hours, minutes };
};

export const formatMinutesToDuration = (
	minutes: number | null,
	formatUnits: Array<"months" | "weeks" | "days" | "hours" | "minutes" | "seconds">,
	uiLanguage?: string,
): string | null => {
	if (!minutes) return null;

	const duration = minutesToDuration(minutes);
	const locale = uiLanguage ? getDateFnsLocale(uiLanguage) : undefined;

	return formatDuration(duration, {
		format: formatUnits,
		locale,
	});
};

export const getDaysSincePurchase = (date: Date | null): number => {
	if (!date || !dateFnsIsValid(date)) {
		return 0;
	}

	return differenceInDays(new Date(), date);
};
