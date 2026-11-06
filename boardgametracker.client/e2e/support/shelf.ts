import type { Page } from "@playwright/test";
import { format, formatDistanceToNow, subMonths, subYears } from "date-fns";
import { displayDate } from "./format";
import { expected, gameNights, games, sessions } from "./seedData";

type SeedGame = (typeof games)[number];

const lastPlayOf = (game: SeedGame) =>
	sessions
		.filter((session) => session.game === game.title)
		.map((session) => session.start)
		.sort((a, b) => b.getTime() - a.getTime())[0];

export const shelfOf = (months: number) => {
	const cutoff = subMonths(new Date(), months);
	return games.filter(
		(game) => game.state === "owned" && new Date(game.additionDate) <= cutoff && !(lastPlayOf(game) >= cutoff),
	);
};

export const shelf = shelfOf(6);

export const playedWithin = (months: number) => {
	const cutoff = subMonths(new Date(), months);
	return games.filter((game) => game.state === "owned" && lastPlayOf(game) >= cutoff);
};

export const isNeverPlayed = (game: SeedGame) => !lastPlayOf(game);

export const nightOf = (game: SeedGame) => gameNights.find((night) => night.suggestedGames.includes(game.title));

export const money = (value: number) => `€${value.toLocaleString("en-US")}`;

export const priceOf = (shelfGames: SeedGame[]) =>
	shelfGames.reduce((total, game) => total + (game.buyingPrice ?? 0), 0);

export const inTitleOrder = (shelfGames: SeedGame[]) => [...shelfGames].sort((a, b) => a.title.localeCompare(b.title));

export const inOwnedOrder = (shelfGames: SeedGame[]) =>
	[...shelfGames].sort((a, b) => a.additionDate.localeCompare(b.additionDate));

export const inPriceOrder = (shelfGames: SeedGame[]) =>
	[...shelfGames].sort((a, b) => (b.buyingPrice ?? 0) - (a.buyingPrice ?? 0));

export const inUnplayedOrder = (shelfGames: SeedGame[]) => {
	const owned = inOwnedOrder(shelfGames);
	const played = owned
		.filter((game) => !isNeverPlayed(game))
		.sort((a, b) => lastPlayOf(a).getTime() - lastPlayOf(b).getTime());
	return [...owned.filter(isNeverPlayed), ...played];
};

export const ownedLongerThan = (shelfGames: SeedGame[], years: number) => {
	const cutoff = subYears(new Date(), years);
	return shelfGames.filter((game) => new Date(game.additionDate) <= cutoff);
};

const details = (game: SeedGame) => {
	const since = `owned since ${format(new Date(game.additionDate), "MMM yyyy")}`;
	const plays = sessions.filter((session) => session.game === game.title).length;
	if (plays > 0) return `${since} · ${plays} ${plays === 1 ? "play" : "plays"}`;
	const people = game.minPlayers === game.maxPlayers ? game.maxPlayers : `${game.minPlayers}–${game.maxPlayers}`;
	const time = game.minPlayTime === game.maxPlayTime ? game.maxPlayTime : `${game.minPlayTime}–${game.maxPlayTime}`;
	return `${since} · ${people} players · ${time} min`;
};

export const rowText = (game: SeedGame, nextNight: boolean) => {
	const last = lastPlayOf(game);
	const pill = last ? formatDistanceToNow(last, { addSuffix: true }) : "Never played";
	const night = nightOf(game);
	const plan = night ? `Planned · ${displayDate(night.startDate)}` : nextNight ? "Plan" : "";
	return `${game.title}${details(game)}Last played: ${pill}Paid${money(game.buyingPrice ?? 0)}${plan}`;
};

export const readerRowText = (game: SeedGame) => rowText(game, false);
export const adminRowText = (game: SeedGame) => `${rowText(game, true)}Log session`;

export const shelfRows = (page: Page) => page.getByRole("list", { name: "Games on the shelf" }).getByRole("listitem");

export const shelfRow = (page: Page, title: string) =>
	shelfRows(page).filter({ has: page.getByRole("link", { name: title, exact: true }) });

export const nextNight = gameNights[0];

export const summaryOf = (shelfGames: SeedGame[]) => {
	const oldest = inOwnedOrder(shelfGames.filter(isNeverPlayed))[0];
	return [
		`${shelfGames.length} of ${expected.ownedCount} owned games on the shelf`,
		`${shelfGames.filter(isNeverPlayed).length} never played`,
		`oldest: ${oldest.title}, since ${format(new Date(oldest.additionDate), "MMM yyyy")}`,
	].join(" · ");
};
