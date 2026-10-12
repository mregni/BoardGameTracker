import { differenceInCalendarDays, differenceInCalendarMonths } from "date-fns";

const unitFor = (target: Date, now: Date): [number, Intl.RelativeTimeFormatUnit] => {
	const days = Math.max(0, differenceInCalendarDays(target, now));
	if (days < 14) return [days, "day"];
	if (days < 60) return [Math.floor(days / 7), "week"];
	return [Math.max(2, differenceInCalendarMonths(target, now)), "month"];
};

export const relativeDay = (date: Date | string, uiLanguage: string, now: Date = new Date()): string => {
	const [value, unit] = unitFor(new Date(date), now);
	return new Intl.RelativeTimeFormat(uiLanguage, { numeric: "auto" }).format(value, unit);
};
