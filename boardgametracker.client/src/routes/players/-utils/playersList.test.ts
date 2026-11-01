import { describe, expect, it } from "vitest";
import type { PlayerOverview } from "@/models";
import { filterPlayers, parsePlayersSearch, sortPlayers } from "./playersList";

const player = (id: number, name: string, playCount: number, winCount: number, lastPlayed: string | null = null) =>
	({ id, name, image: null, playCount, winCount, lastPlayed }) satisfies PlayerOverview;

const anna = player(1, "Anna", 10, 4, "2026-10-01T18:00:00Z");
const ben = player(2, "Ben", 5, 4, "2026-10-20T18:00:00Z");
const carl = player(3, "Carl", 8, 4, "2026-09-01T18:00:00Z");
const dora = player(4, "Dora", 0, 0);
const eve = player(5, "eve", 2, 1, "2026-10-25T18:00:00Z");
const players = [anna, ben, carl, dora, eve];

describe("sortPlayers", () => {
	const names = (sort: Parameters<typeof sortPlayers>[1]) => sortPlayers(players, sort).map((x) => x.name);

	it("sorts by most recent play, name and sessions", () => {
		expect(names("recent")).toEqual(["eve", "Ben", "Anna", "Carl", "Dora"]);
		expect(names("name")).toEqual(["Anna", "Ben", "Carl", "Dora", "eve"]);
		expect(names("sessions")).toEqual(["Anna", "Carl", "Ben", "eve", "Dora"]);
	});
});

describe("filterPlayers", () => {
	it("matches names case-insensitively and ignores a blank query", () => {
		expect(filterPlayers(players, " EV ").map((x) => x.name)).toEqual(["eve"]);
		expect(filterPlayers(players, "  ")).toBe(players);
	});
});

describe("parsePlayersSearch", () => {
	it("keeps a known sort and a non-blank query, and drops the default sort", () => {
		expect(parsePlayersSearch({ q: "an", sort: "sessions" })).toEqual({ q: "an", sort: "sessions" });
		expect(parsePlayersSearch({ q: " ", sort: "recent" })).toEqual({});
		expect(parsePlayersSearch({ sort: "wins", q: 3 })).toEqual({});
	});
});
