import { differenceInCalendarDays, differenceInCalendarMonths } from "date-fns";

const unitFor = (target: Date, now: Date): [number, Intl.RelativeTimeFormatUnit] => {
	const days = differenceInCalendarDays(target, now);
	if (Math.abs(days) < 14) return [days, "day"];
	if (Math.abs(days) < 60) return [Math.trunc(days / 7), "week"];
	const months = differenceInCalendarMonths(target, now);
	return [Math.sign(days) * Math.max(2, Math.abs(months)), "month"];
};

export const relativeDay = (date: Date | string, uiLanguage: string, now: Date = new Date()): string => {
	const [value, unit] = unitFor(new Date(date), now);
	return new Intl.RelativeTimeFormat(uiLanguage, { numeric: "auto" }).format(value, unit);
};
