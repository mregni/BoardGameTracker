import type { APIRequestContext, Page } from "@playwright/test";
import { differenceInCalendarDays } from "date-fns";
import { credentials, expect } from "./fixtures";
import { displayDate } from "./format";
import { gameNights, games, linkedPlayer, locations, pastGameNight, players, sessions } from "./seedData";

type RsvpState = "accepted" | "maybe" | "pending" | "declined";

export const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
export const playerId = (name: string) => players.indexOf(name) + 1;
export const locationId = (name: string) => locations.indexOf(name) + 1;
export const pastNightId = gameNights.length + 1;

const TIME_ZONE = "Europe/Brussels";

const timeFormat = new Intl.DateTimeFormat("en-GB", {
	timeZone: TIME_ZONE,
	hour: "2-digit",
	minute: "2-digit",
	hourCycle: "h23",
});

const dayParts = new Intl.DateTimeFormat("en-US", {
	timeZone: TIME_ZONE,
	weekday: "short",
	day: "numeric",
	month: "short",
});

const relativeFormat = new Intl.RelativeTimeFormat("en-US", { numeric: "auto" });

export const displayTime = (date: Date) => timeFormat.format(date);

export const displayDateTime = (date: Date) => `${displayDate(date)} ${displayTime(date)}`;

export const dateBox = (date: Date) => {
	const part = (type: Intl.DateTimeFormatPartTypes) => dayParts.formatToParts(date).find((x) => x.type === type)?.value;
	return `${part("weekday")}${part("day")}${part("month")}`;
};

export const relativeDay = (date: Date) => relativeFormat.format(differenceInCalendarDays(date, new Date()), "day");

const answers: Record<RsvpState, { label: string; order: number }> = {
	accepted: { label: "Coming", order: 0 },
	maybe: { label: "Maybe", order: 1 },
	pending: { label: "No answer", order: 2 },
	declined: { label: "Not coming", order: 3 },
};

export const rsvpsOf = (night: (typeof gameNights)[number]) =>
	Object.entries(night.rsvps)
		.map(([name, state]) => ({ name, state: state as RsvpState, label: answers[state as RsvpState].label }))
		.sort((a, b) => answers[a.state].order - answers[b.state].order);

export const namesWith = (night: (typeof gameNights)[number], state: RsvpState) =>
	rsvpsOf(night)
		.filter((rsvp) => rsvp.state === state)
		.map((rsvp) => rsvp.name);

export const rsvpSummary = (night: (typeof gameNights)[number]) =>
	[
		[namesWith(night, "accepted").length, "coming"],
		[namesWith(night, "maybe").length, "maybe"],
		[namesWith(night, "pending").length, "no answer"],
		[namesWith(night, "declined").length, "not coming"],
	]
		.filter(([count]) => count !== 0)
		.map(([count, label]) => `${count} ${label}`)
		.join(" · ");

const playersOf = (subset: typeof sessions) => subset.flatMap((session) => session.scores.map((x) => x.player));

export const seasonOpener = (() => {
	const played = sessions
		.filter((session) => session.gameNight === pastGameNight.title)
		.sort((a, b) => b.start.getTime() - a.start.getTime());
	const attendees = players.filter((name) => name === linkedPlayer || playersOf(played).includes(name));
	const wins = (name: string) => played.filter((session) => session.winner === name).length;
	const mostWins = Math.max(...players.map(wins));
	return {
		played,
		attendees,
		mostWins,
		winners: players.filter((name) => wins(name) === mostWins),
		minutes: played.reduce((total, session) => total + session.minutes, 0),
	};
})();

export const patchResponse = <T>(page: Page, url: string, change: (body: T) => unknown) =>
	page.route(url, async (route) => {
		const response = await route.fetch();
		if (!response.ok()) {
			await route.fulfill({ response });
			return;
		}
		await route.fulfill({ response, json: change(await response.json()) });
	});

export const openNight = async (page: Page, title: string, id: number) => {
	await page.goto(`/game-nights/${id}`);
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
};

export const startOfNight = async (request: APIRequestContext, id: number) =>
	new Date((await api(request, "get", `/api/gamenight/${id}`)).gameNight.startDate);

export const scoreLine = (session: (typeof sessions)[number]) =>
	[...session.scores]
		.sort((a, b) => (b.score ?? 0) - (a.score ?? 0))
		.map(({ player, score }) => `${player} ${score}`)
		.join(" · ");

let token: Promise<string> | null = null;

const authorization = async (request: APIRequestContext) => {
	token ??= request
		.post("/api/auth/login", { data: credentials })
		.then((response) => response.json())
		.then((body) => body.accessToken as string);
	return { Authorization: `Bearer ${await token}` };
};

export const api = async (
	request: APIRequestContext,
	method: "get" | "post" | "put" | "delete",
	path: string,
	data?: unknown,
) => {
	const response = await request[method](path, { data, headers: await authorization(request) });
	expect(response.ok(), `${method} ${path}: ${await response.text()}`).toBe(true);
	return response.status() === 204 ? null : response.json();
};

interface NightOptions {
	title: string;
	startDate: Date;
	location?: string;
	notes?: string;
	suggestedGames?: string[];
	rsvps?: Record<string, RsvpState>;
}

export const createNight = async (request: APIRequestContext, options: NightOptions) => {
	const { title, startDate, location = locations[1], notes = "", suggestedGames = [], rsvps = {} } = options;
	const night = await api(request, "post", "/api/gamenight", {
		title,
		notes,
		startDate: startDate.toISOString(),
		hostId: playerId(linkedPlayer),
		locationId: locationId(location),
		suggestedGameIds: suggestedGames.map(gameId),
		invitedPlayerIds: Object.keys(rsvps).map(playerId),
	});
	const { gameNight } = await api(request, "get", `/api/gamenight/${night.id}`);
	for (const [name, state] of Object.entries(rsvps)) {
		const id = playerId(name);
		const rsvp = gameNight.invitedPlayers.find((x: { playerId: number }) => x.playerId === id);
		await api(request, "put", "/api/gamenight/rsvp", { id: rsvp.id, gameNightId: night.id, playerId: id, state });
	}
	return night.id as number;
};

export const seededNightTitles = [...gameNights.map((night) => night.title), pastGameNight.title];

export const removeCreatedNights = async (request: APIRequestContext) => {
	const nights: { id: number; title: string }[] = await api(request, "get", "/api/gamenight");
	for (const night of nights.filter((x) => !seededNightTitles.includes(x.title))) {
		const detail = await api(request, "get", `/api/gamenight/${night.id}`);
		for (const session of detail.sessions) {
			await api(request, "delete", `/api/session/${session.id}`);
		}
		await api(request, "delete", `/api/gamenight/${night.id}`);
	}
};
