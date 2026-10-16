import type { Game, GameNightStatistics, Location, Player } from "@/models";
import { GameNightStatisticsPeriod } from "@/models";
import type { StatsLookup } from "./StatsParts";

export const players = [
	{ id: 1, name: "Kathleen", image: null },
	{ id: 2, name: "Mikhael", image: null },
	{ id: 3, name: "reggi", image: null },
] as unknown as Player[];

export const games = [
	{ id: 10, title: "Azul", image: "azul.png" },
	{ id: 11, title: "Carcassonne", image: "carcassonne.png" },
	{ id: 12, title: "Everdell", image: "everdell.png" },
] as unknown as Game[];

export const locations = [{ id: 7, name: "Thuis" }] as unknown as Location[];

export const lookup: StatsLookup = {
	players: new Map(players.map((x) => [x.id, x])),
	games: new Map(games.map((x) => [x.id, x])),
	locations: new Map(locations.map((x) => [x.id, x])),
	uiLanguage: "en-us",
	dateFormat: "dd-MM-yyyy",
	timeFormat: "HH:mm",
};

const day = (value: string) => new Date(`${value}T19:00:00Z`);

export const buildStatistics = (overrides: Partial<GameNightStatistics> = {}): GameNightStatistics => ({
	period: GameNightStatisticsPeriod.AllTime,
	totalNights: 8,
	nightsWithSessions: 6,
	nights: 7,
	gamesPlayed: 21,
	totalMinutes: 1350,
	averageTable: 4.25,
	averageDaysBetweenNights: 14,
	tiedNights: 1,
	winners: [
		{ playerId: 1, rank: 1, nightsWon: 3, nightsAttended: 6, gamesWon: 8 },
		{ playerId: 2, rank: 2, nightsWon: 2, nightsAttended: 7, gamesWon: 5 },
	],
	history: [
		{
			gameNightId: 31,
			title: "Strategy night",
			startDate: day("2026-10-09"),
			locationId: 7,
			gamesPlayed: 4,
			mostWins: 3,
			winnerIds: [1],
			winnerStreak: 3,
		},
		{
			gameNightId: 30,
			title: "Brass and burgers",
			startDate: day("2026-10-02"),
			locationId: 7,
			gamesPlayed: 2,
			mostWins: 1,
			winnerIds: [1, 3],
			winnerStreak: 2,
		},
		{
			gameNightId: 29,
			title: "Lost night",
			startDate: day("2026-09-25"),
			locationId: 99,
			gamesPlayed: 2,
			mostWins: 0,
			winnerIds: [],
			winnerStreak: 0,
		},
		{
			gameNightId: 28,
			title: "Cancelled",
			startDate: day("2026-09-18"),
			locationId: 7,
			gamesPlayed: 0,
			mostWins: 0,
			winnerIds: [],
			winnerStreak: 0,
		},
		{
			gameNightId: 27,
			title: "Fifth night",
			startDate: day("2026-09-11"),
			locationId: 7,
			gamesPlayed: 1,
			mostWins: 1,
			winnerIds: [2],
			winnerStreak: 1,
		},
		{
			gameNightId: 26,
			title: "Sixth night",
			startDate: day("2026-09-04"),
			locationId: 7,
			gamesPlayed: 1,
			mostWins: 1,
			winnerIds: [2],
			winnerStreak: 1,
		},
	],
	games: [
		{ gameId: 10, timesPlayed: 6, nights: 4, timesPlanned: 2 },
		{ gameId: 11, timesPlayed: 3, nights: 3, timesPlanned: 0 },
	],
	plannedNeverPlayed: [{ gameId: 12, timesPlanned: 3 }],
	streaks: {
		currentWinner: { playerId: 1, length: 3, from: day("2026-09-25"), to: day("2026-10-09") },
		longestWinner: { playerId: 2, length: 4, from: day("2026-02-06"), to: day("2026-04-03") },
		currentAttendance: { playerId: 3, length: 5, from: day("2026-09-11"), to: day("2026-10-09") },
		longestGame: { gameId: 11, length: 3, from: day("2026-09-18"), to: day("2026-10-02"), isCurrent: false },
	},
	recentNights: [
		{ gameNightId: 30, title: "Brass and burgers", startDate: day("2026-10-02") },
		{ gameNightId: 31, title: "Strategy night", startDate: day("2026-10-09") },
	],
	attendance: [
		{ playerId: 2, nightsAttended: 7, nightsHosted: 3, recentNights: [true, true] },
		{ playerId: 3, nightsAttended: 5, nightsHosted: 0, recentNights: [false, true] },
	],
	records: {
		longestNight: {
			gameNightId: 31,
			title: "Strategy night",
			startDate: day("2026-10-09"),
			gamesPlayed: 4,
			value: 340,
			gameId: null,
			at: null,
		},
		mostGames: {
			gameNightId: 31,
			title: "Strategy night",
			startDate: day("2026-10-09"),
			gamesPlayed: 4,
			value: 4,
			gameId: 10,
			at: null,
		},
		biggestTable: {
			gameNightId: 30,
			title: "Brass and burgers",
			startDate: day("2026-10-02"),
			gamesPlayed: 2,
			value: 6,
			gameId: null,
			at: null,
		},
		latestFinish: {
			gameNightId: 30,
			title: "Brass and burgers",
			startDate: day("2026-10-02"),
			gamesPlayed: 2,
			value: 170,
			gameId: 11,
			at: new Date(2026, 9, 3, 2, 10),
		},
	},
	...overrides,
});
