import type { Page } from "@playwright/test";
import { differenceInCalendarDays } from "date-fns";
import type { components } from "../../src/models/api.generated";
import { displayDate } from "./format";
import { linkedPlayer, locations } from "./seedData";
import { type Api, expect, spaced } from "./session";

type GameNight = components["schemas"]["GameNightDto"];

const TIME_ZONE = "Europe/Brussels";
const inviteRequestTurn = spaced(2_500);

export const paceInviteRequests = (page: Page) =>
	page.route(/\/api\/(gamenight\/link|manual\/gamenight)\//, async (route) => {
		await inviteRequestTurn();
		await route.fallback();
	});

export const fullDate = (date: string | Date) => {
	const weekday = new Intl.DateTimeFormat("en-GB", { timeZone: TIME_ZONE, weekday: "long" }).format(new Date(date));
	return `${weekday} ${displayDate(new Date(date))}`;
};

export const clockTime = (date: string | Date) =>
	new Intl.DateTimeFormat("en-GB", { timeZone: TIME_ZONE, hour: "2-digit", minute: "2-digit" }).format(new Date(date));

export const daysUntil = (date: string | Date) => differenceInCalendarDays(new Date(date), new Date());

export const gameNightsFrom = async (api: Api): Promise<GameNight[]> =>
	(await api.call("GET", "/api/gamenight")).json();

export const gameNightByTitle = async (api: Api, title: string) => {
	const night = (await gameNightsFrom(api)).find((candidate) => candidate.title === title);
	expect(night, title).toBeTruthy();
	return night as GameNight;
};

export const playerIdOf = async (api: Api, name: string): Promise<number> => {
	const players: { id: number; name: string }[] = await (await api.call("GET", "/api/player")).json();
	const player = players.find((candidate) => candidate.name === name);
	expect(player, name).toBeTruthy();
	return player?.id ?? 0;
};

export interface NewNight {
	title: string;
	startDate: Date;
	invited: string[];
	notes?: string;
	suggestedGameIds?: number[];
}

export const createNight = async (api: Api, night: NewNight): Promise<GameNight> => {
	const allLocations: { id: number; name: string }[] = await (await api.call("GET", "/api/location")).json();
	const invitedPlayerIds = [];
	for (const name of night.invited) invitedPlayerIds.push(await playerIdOf(api, name));
	const created = await api.call("POST", "/api/gamenight", {
		title: night.title,
		notes: night.notes ?? "",
		startDate: night.startDate.toISOString(),
		hostId: await playerIdOf(api, linkedPlayer),
		locationId: allLocations.find((location) => location.name === locations[0])?.id,
		suggestedGameIds: night.suggestedGameIds ?? [],
		invitedPlayerIds,
	});
	expect(created.ok(), await created.text()).toBe(true);
	const { id } = await created.json();
	return gameNightOf(api, id);
};

export const gameNightOf = async (api: Api, id: number): Promise<GameNight> => {
	const detail = await (await api.call("GET", `/api/gamenight/${id}`)).json();
	return detail.gameNight;
};

export const rsvpStateOf = async (api: Api, id: number, name: string) => {
	const night = await gameNightOf(api, id);
	return night.invitedPlayers.find((rsvp) => rsvp.player.name === name)?.state;
};

export const inviteUrl = (linkId: string) => `/rsvp?linkId=${linkId}`;
