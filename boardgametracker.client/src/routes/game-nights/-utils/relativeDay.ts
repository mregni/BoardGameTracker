import { differenceInCalendarDays, differenceInCalendarMonths } from "date-fns";

export interface RelativeDay {
	key: "today" | "tomorrow" | "days" | "weeks" | "months";
	count: number;
}

export const relativeDay = (date: Date | string, now: Date = new Date()): RelativeDay => {
	const target = new Date(date);
	const days = Math.max(0, differenceInCalendarDays(target, now));

	if (days === 0) return { key: "today", count: 0 };
	if (days === 1) return { key: "tomorrow", count: 1 };
	if (days < 14) return { key: "days", count: days };
	if (days < 60) return { key: "weeks", count: Math.floor(days / 7) };
	return { key: "months", count: Math.max(2, differenceInCalendarMonths(target, now)) };
};
