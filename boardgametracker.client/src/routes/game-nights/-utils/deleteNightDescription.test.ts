import type { TFunction } from "i18next";
import { describe, expect, it } from "vitest";
import type { GameNight } from "@/models";
import { deleteNightDescription } from "./deleteNightDescription";

const t = ((key: string, options?: { count?: number }) =>
	options?.count === undefined ? key : `${key}:${options.count}`) as unknown as TFunction;

const night = (runningGames: number, gamesPlayed: number) =>
	({ title: "Strategy night", summary: { runningGames, gamesPlayed } }) as unknown as GameNight;

describe("deleteNightDescription", () => {
	it("lists the running games, the sessions that stay and the RSVPs", () => {
		expect(deleteNightDescription(t, night(2, 1))).toBe(
			"game-nights:delete.description game-nights:delete.running:2 game-nights:delete.sessions:1 game-nights:delete.rsvps",
		);
	});

	it("only mentions the RSVPs for a night without games", () => {
		expect(deleteNightDescription(t, night(0, 0))).toBe("game-nights:delete.description game-nights:delete.rsvps");
	});

	it("is empty without a night", () => {
		expect(deleteNightDescription(t, null)).toBe("");
	});
});
