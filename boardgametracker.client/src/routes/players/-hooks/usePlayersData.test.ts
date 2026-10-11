import { describe, expect, it } from "vitest";
import type { PlayerOverview } from "@/models";
import { sortByActivity } from "./usePlayersData";

const player = (id: number, name: string, playCount: number, winCount = 0): PlayerOverview => ({
	id,
	name,
	image: null,
	playCount,
	winCount,
});

describe("sortByActivity", () => {
	it("puts the players with the most sessions first", () => {
		const players = [player(1, "Kathleen", 6), player(2, "reggi", 10), player(3, "dfgh", 0), player(4, "Mikhael", 8)];

		expect(sortByActivity(players).map((x) => x.name)).toEqual(["reggi", "Mikhael", "Kathleen", "dfgh"]);
	});

	it("sorts players with the same session count by name", () => {
		const players = [player(1, "Zoe", 2), player(2, "Anna", 2), player(3, "Bram", 0), player(4, "Aaron", 0)];

		expect(sortByActivity(players).map((x) => x.name)).toEqual(["Anna", "Zoe", "Aaron", "Bram"]);
	});

	it("does not mutate the original list", () => {
		const players = [player(1, "Anna", 1), player(2, "Bram", 3)];

		sortByActivity(players);

		expect(players.map((x) => x.name)).toEqual(["Anna", "Bram"]);
	});
});
