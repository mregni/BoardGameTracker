import { subYears } from "date-fns";
import type { Shame } from "@/models";
import { monthsSince } from "@/utils/dateUtils";

export const SHAME_SHOWS = ["all", "never", "played"] as const;
export const SHAME_OWNED = ["any", "1y", "2y"] as const;
export const SHAME_SORTS = ["unplayed", "price", "owned", "title"] as const;

export type ShameShow = (typeof SHAME_SHOWS)[number];
export type ShameOwned = (typeof SHAME_OWNED)[number];
export type ShameSort = (typeof SHAME_SORTS)[number];

export interface ShameFilterState {
	show: ShameShow;
	owned: ShameOwned;
	sort: ShameSort;
}

export const DEFAULT_SHAME_FILTERS: ShameFilterState = { show: "all", owned: "any", sort: "unplayed" };

export const MONTH_STEPS = [6, 12, 24];

export const LONG_UNPLAYED_MONTHS = 12;

export const monthOptions = (configuredMonths: number): number[] => {
	const longer = MONTH_STEPS.filter((months) => months > configuredMonths);
	return [configuredMonths, ...longer];
};

export const isNeverPlayed = (shame: Shame): boolean => shame.lastSessionDate === null;

export const countByShow = (shames: Shame[]): Record<ShameShow, number> => {
	const never = shames.filter(isNeverPlayed).length;
	return { all: shames.length, never, played: shames.length - never };
};

const ownedCutoff = (owned: ShameOwned, now: Date): Date | null => {
	switch (owned) {
		case "1y":
			return subYears(now, 1);
		case "2y":
			return subYears(now, 2);
		default:
			return null;
	}
};

const time = (date: Date | null): number | null => (date ? new Date(date).getTime() : null);

const ascendingNullsFirst = (a: number | null, b: number | null): number => {
	if (a === b) return 0;
	if (a === null) return -1;
	if (b === null) return 1;
	return a - b;
};

const byTitle = (a: Shame, b: Shame) => a.title.localeCompare(b.title);

const comparators: Record<ShameSort, (a: Shame, b: Shame) => number> = {
	unplayed: (a, b) =>
		ascendingNullsFirst(time(a.lastSessionDate), time(b.lastSessionDate)) ||
		ascendingNullsFirst(time(a.additionDate), time(b.additionDate)) ||
		byTitle(a, b),
	price: (a, b) => {
		if (a.price === b.price) return byTitle(a, b);
		if (a.price === null) return 1;
		if (b.price === null) return -1;
		return b.price - a.price;
	},
	owned: (a, b) => ascendingNullsFirst(time(a.additionDate), time(b.additionDate)) || byTitle(a, b),
	title: byTitle,
};

export const filterShames = (shames: Shame[], filters: ShameFilterState, now: Date = new Date()): Shame[] => {
	const cutoff = ownedCutoff(filters.owned, now);

	return shames
		.filter((shame) => {
			if (filters.show === "never" && !isNeverPlayed(shame)) return false;
			if (filters.show === "played" && isNeverPlayed(shame)) return false;
			if (cutoff === null) return true;
			return shame.additionDate !== null && new Date(shame.additionDate) <= cutoff;
		})
		.sort(comparators[filters.sort]);
};

export type LastPlayedTone = "bad" | "warn" | "neutral";

export const lastPlayedTone = (shame: Shame, now: Date = new Date()): LastPlayedTone => {
	if (isNeverPlayed(shame)) return "bad";
	return (monthsSince(shame.lastSessionDate, now) ?? 0) >= LONG_UNPLAYED_MONTHS ? "warn" : "neutral";
};

export const oldestNeverPlayed = (shames: Shame[]): Shame | null =>
	shames.filter(isNeverPlayed).sort(comparators.owned)[0] ?? null;
