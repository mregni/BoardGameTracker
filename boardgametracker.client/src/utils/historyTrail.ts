import type { RouterHistory } from "@tanstack/react-router";

const trails = new WeakMap<RouterHistory, Map<number, string>>();

export const normalizePathname = (pathname: string) => (pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname);

const indexOf = (history: RouterHistory) => history.location.state.__TSR_index;

export const trackHistory = (history: RouterHistory) => {
	const entries = new Map<number, string>();
	trails.set(history, entries);
	const record = () => {
		entries.set(indexOf(history), normalizePathname(history.location.pathname));
	};
	record();
	return history.subscribe(record);
};

export const previousPathname = (history: RouterHistory): string | undefined =>
	trails.get(history)?.get(indexOf(history) - 1);
