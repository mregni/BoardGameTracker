import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { i18n as I18n } from "i18next";
import { vi } from "vitest";
import type { CompareGame, CompareResult, CompareSession, Game, Player } from "@/models";

export const COMPARE_LOCALES = ["base", "en-US", "nl-BE", "nl-NL", "es-ES"] as const;

const read = (lang: string, namespace: string) =>
	JSON.parse(readFileSync(resolve(process.cwd(), "public/locales", lang, `${namespace}.json`), "utf8"));

export const createCompareI18n = async (lang = "en-US"): Promise<I18n> => {
	const { createInstance } = await vi.importActual<typeof import("i18next")>("i18next");
	const instance = createInstance();
	const lng = lang === "base" ? "en-US" : lang;
	await instance.init({
		lng,
		ns: ["compare", "common"],
		defaultNS: "compare",
		resources: { [lng]: { compare: read(lang, "compare"), common: read(lang, "common") } },
		interpolation: { escapeValue: false },
	});
	return instance;
};

export const translationFor = (instance: I18n | null, namespace?: string | string[]) => {
	const resolved = Array.isArray(namespace) ? namespace[0] : namespace;
	return { t: instance!.getFixedT(null, resolved ?? "compare"), i18n: instance };
};

export const playerOne = { id: 1, name: "Mikhaël", image: null } as Player;
export const playerTwo = { id: 2, name: "Kathleen", image: null } as Player;
export const reggi = { id: 3, name: "reggi", image: null } as Player;

export const games: Record<number, Game> = {
	10: { id: 10, title: "Azul", image: null } as unknown as Game,
	11: { id: 11, title: "Carcassonne", image: null } as unknown as Game,
	12: { id: 12, title: "Brass: Birmingham", image: null } as unknown as Game,
	13: { id: 13, title: "Age of Inventors", image: null } as unknown as Game,
	14: { id: 14, title: "Pandemic", image: null } as unknown as Game,
	15: { id: 15, title: "Cascadia", image: null } as unknown as Game,
};

export const gameRow = (overrides: Partial<CompareGame> & { gameId: number }): CompareGame => ({
	sessionCount: 1,
	playerOneWins: 0,
	playerTwoWins: 0,
	otherWins: 0,
	sameTeamCount: 0,
	sameTeamWins: 0,
	lastTeamWon: null,
	playerOneAverageScore: null,
	playerTwoAverageScore: null,
	scoredSessionCount: 0,
	lastPlayed: new Date(2026, 9, 10),
	lastOutcome: "other",
	...overrides,
});

export const sessionRow = (overrides: Partial<CompareSession> & { sessionId: number }): CompareSession => ({
	gameId: 10,
	start: new Date(2026, 9, 16),
	minutes: 30,
	playerOneScore: null,
	playerTwoScore: null,
	outcome: "other",
	otherWinnerId: null,
	teamWon: null,
	...overrides,
});

export const compareResult = (overrides: Partial<CompareResult> = {}): CompareResult => ({
	winCount: { playerOne: 54, playerTwo: 48 },
	winPercentage: { playerOne: 0.38, playerTwo: 0.41 },
	sessionCounts: { playerOne: 142, playerTwo: 118 },
	totalDuration: { playerOne: 7860, playerTwo: 6240 },
	directWins: { playerOne: 23, playerTwo: 17 },
	wonByOthers: 6,
	sameTeam: { count: 0, won: 0, lost: 0 },
	totalSessionsTogether: 46,
	minutesPlayed: 2410,
	preferredGame: { gameId: 10, sessionCount: 11 },
	firstGameTogether: { gameId: 11, startDate: new Date(2025, 2, 12) },
	closestGame: { gameId: 12, playerId: 1, scoringDifference: 1 },
	streak: { side: "playerTwo", count: 2 },
	games: [
		gameRow({ gameId: 10, sessionCount: 11, playerOneWins: 4, playerTwoWins: 7, lastOutcome: "playerTwo" }),
		gameRow({
			gameId: 12,
			sessionCount: 7,
			playerOneWins: 5,
			playerTwoWins: 1,
			otherWins: 1,
			lastOutcome: "playerOne",
		}),
	],
	recentSessions: [
		sessionRow({ sessionId: 5, outcome: "playerTwo", playerOneScore: 91, playerTwoScore: 98, minutes: 80 }),
		sessionRow({ sessionId: 4, outcome: "playerOne" }),
	],
	bestGames: { playerOne: [12], playerTwo: [10] },
	...overrides,
});
