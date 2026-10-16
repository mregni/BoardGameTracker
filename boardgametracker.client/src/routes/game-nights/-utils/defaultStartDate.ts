import { addDays, isAfter, set } from "date-fns";

export const defaultGameNightStart = (now: Date = new Date()): Date => {
	const tonight = set(now, { hours: 19, minutes: 30, seconds: 0, milliseconds: 0 });
	return isAfter(tonight, now) ? tonight : addDays(tonight, 1);
};
