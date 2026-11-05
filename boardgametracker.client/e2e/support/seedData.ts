import { subDays, subMinutes, subMonths } from "date-fns";
import type { components } from "../../src/models/api.generated";

type GameState = components["schemas"]["GameState"];

const now = new Date();

const titles = [
	"Azul",
	"Carcassonne",
	"Wingspan",
	"Catan",
	"Ticket to Ride",
	"Pandemic",
	"Terraforming Mars",
	"Splendor",
	"Dominion",
	"7 Wonders",
	"Patchwork",
	"Everdell",
	"Scythe",
	"Root",
	"Brass Birmingham",
	"Agricola",
	"Puerto Rico",
	"Concordia",
	"Great Western Trail",
	"Spirit Island",
	"Gloomhaven",
	"Arkham Horror",
	"Twilight Struggle",
	"Power Grid",
	"Castles of Burgundy",
	"Lords of Waterdeep",
	"Viticulture",
	"Cascadia",
	"Dune Imperium",
	"Ark Nova",
	"Lost Ruins of Arnak",
	"Quacks of Quedlinburg",
	"Sagrada",
	"Kingdomino",
	"Mysterium",
	"Codenames",
	"Dixit",
	"Hive",
	"Jaipur",
	"Santorini",
];

const PLAYED_GAMES = 15;
const OWNED_GAMES = 28;
const WANTED_GAMES = 6;
const PREVIOUSLY_OWNED_GAMES = 4;
const THIS_MONTH_SESSIONS = 3;

const stateOf = (index: number): GameState => {
	if (index < OWNED_GAMES) return "owned";
	if (index < OWNED_GAMES + WANTED_GAMES) return "wanted";
	if (index < OWNED_GAMES + WANTED_GAMES + PREVIOUSLY_OWNED_GAMES) return "previouslyOwned";
	return "forTrade";
};

export const games = titles.map((title, index) => {
	const state = stateOf(index);
	return {
		title,
		state,
		buyingPrice: state === "wanted" ? null : 20 + index * 2,
		description: `${title} is part of the seeded collection.`,
		yearPublished: 1985 + index,
		minPlayers: 1 + (index % 2),
		maxPlayers: 4 + (index % 3),
		minPlayTime: 30,
		maxPlayTime: 60 + (index % 4) * 30,
		minAge: 8 + (index % 5),
		rating: 5 + (index % 50) / 10,
		weight: 1 + (index % 40) / 10,
		hasScoring: true,
		additionDate: subDays(now, 600 - index * 15).toISOString(),
	};
});

export const players = ["Anna", "Bram", "Cato", "Dries", "Elise", "Finn", "Greet", "Hanne"];

export const linkedPlayer = players[0];

export const locations = ["Clubhouse", "Cafe", "Library"];

export const expansions = [
	{ game: titles[0], title: "Azul: Crystal Mosaic" },
	{ game: titles[0], title: "Azul: Glazed Pavilion" },
	{ game: titles[1], title: "Carcassonne: Inns and Cathedrals" },
];

const playedGameIndexes = Array.from({ length: PLAYED_GAMES }, (_, round) =>
	Array.from({ length: PLAYED_GAMES - round }, (_, index) => index),
).flat();

const startOf = (sessionIndex: number) =>
	sessionIndex < THIS_MONTH_SESSIONS
		? subMinutes(now, 5 + sessionIndex * 10)
		: subDays(now, 35 + (sessionIndex - THIS_MONTH_SESSIONS) * 5);

export const sessions = playedGameIndexes.map((gameIndex, sessionIndex) => {
	const seats = sessionIndex % 3 === 0 ? 4 : 3;
	const scores = Array.from({ length: seats }, (_, seat) => {
		const playerIndex = (sessionIndex + seat) % players.length;
		const edge = (players.length - playerIndex) * 4;
		return { player: players[playerIndex], score: 40 + edge + (((sessionIndex + 1) * (playerIndex + 3) * 7) % 40) };
	});
	const winner = scores.reduce((best, score) => (score.score > best.score ? score : best), scores[0]);
	return {
		game: titles[gameIndex],
		start: startOf(sessionIndex),
		minutes: 30 + (sessionIndex % 5) * 15,
		location: locations[sessionIndex % locations.length],
		scores,
		winner: winner.player,
	};
});

export const gameNights = [
	{
		title: "Strategy night",
		startDate: subDays(now, -3),
		location: locations[0],
		suggestedGames: [titles[15], titles[16], titles[17]],
		rsvps: { Anna: "accepted", Bram: "accepted", Cato: "maybe", Dries: "declined", Elise: "pending" },
	},
	{
		title: "Family afternoon",
		startDate: subDays(now, -10),
		location: locations[1],
		suggestedGames: [titles[10]],
		rsvps: { Anna: "accepted", Finn: "pending" },
	},
];

const playCounts = (since: Date) => {
	const counts = new Map<string, number>();
	for (const session of sessions) {
		if (session.start >= since) counts.set(session.game, (counts.get(session.game) ?? 0) + 1);
	}
	return [...counts.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]));
};

export const standingsOf = (subset: typeof sessions) =>
	players
		.map((name) => {
			const played = subset.filter((session) => session.scores.some((score) => score.player === name));
			const wins = played.filter((session) => session.winner === name).length;
			return { name, sessions: played.length, wins, winRate: wins / played.length };
		})
		.filter((standing) => standing.sessions > 0)
		.sort((a, b) => b.wins - a.wins || b.winRate - a.winRate || b.sessions - a.sessions);

const shameGames = games.filter(
	(game) =>
		game.state === "owned" &&
		!sessions.some((session) => session.game === game.title && session.start >= subMonths(now, 6)),
);

const ownedPrices = games.filter((game) => game.state === "owned").map((game) => game.buyingPrice ?? 0);
const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);
const thisMonth = sessions.slice(0, THIS_MONTH_SESSIONS);
const playedThisMonth = thisMonth.filter((session) => session.scores.some((score) => score.player === linkedPlayer));

export const expected = {
	gameCount: games.length,
	expansionCount: expansions.length,
	playerCount: players.length,
	sessionCount: sessions.length,
	sessionsThisMonth: thisMonth.length,
	playersThisMonth: new Set(thisMonth.flatMap((session) => session.scores.map((score) => score.player))).size,
	linkedPlayedThisMonth: playedThisMonth.length,
	linkedWonThisMonth: playedThisMonth.filter((session) => session.winner === linkedPlayer).length,
	lastSessions: sessions.slice(0, 5),
	topPlayers: standingsOf(sessions).slice(0, 3),
	mostPlayedAllTime: playCounts(new Date(0)).slice(0, 4),
	mostPlayedLast12Months: playCounts(subMonths(now, 12)).slice(0, 4),
	mostPlayedThisYear: playCounts(new Date(now.getFullYear(), 0, 1)).slice(0, 4),
	ownedCount: OWNED_GAMES,
	wantedCount: WANTED_GAMES,
	previouslyOwnedCount: PREVIOUSLY_OWNED_GAMES,
	forTradeCount: games.length - OWNED_GAMES - WANTED_GAMES - PREVIOUSLY_OWNED_GAMES,
	ownedValue: sum(ownedPrices),
	shameCount: shameGames.length,
	shameValue: sum(shameGames.map((game) => game.buyingPrice ?? 0)),
	recentlyAdded: titles.slice(-3).reverse(),
};
