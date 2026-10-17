import { describe, expect, it } from "vitest";
import type { Game, Session } from "@/models";
import { lastPlayedOf } from "./lastPlayed";

const games = { 1: { id: 1, title: "Wingspan" }, 2: { id: 2, title: "Azul" } } as unknown as Record<number, Game>;
const gameById = (id: number) => games[id] ?? null;
const session = (gameId: number, start: Date) => ({ gameId, start }) as unknown as Session;

describe("lastPlayedOf", () => {
	it("picks the most recent session whatever the order", () => {
		const sessions = [
			session(1, new Date(2026, 9, 1)),
			session(2, new Date(2026, 9, 11)),
			session(1, new Date(2026, 8, 3)),
		];

		expect(lastPlayedOf(sessions, gameById)).toEqual({ start: new Date(2026, 9, 11), gameTitle: "Azul" });
	});

	it("leaves out the game title when the game is unknown", () => {
		expect(lastPlayedOf([session(9, new Date(2026, 9, 1))], gameById)).toEqual({
			start: new Date(2026, 9, 1),
			gameTitle: undefined,
		});
	});

	it("returns null without sessions", () => {
		expect(lastPlayedOf([], gameById)).toBeNull();
	});
});
