import { describe, expect, it } from "vitest";
import type { Game } from "@/models";
import { toGameSelectItems } from "./gameSelectItems";

const game = (id: number, title: string) => ({ id, title, image: null }) as unknown as Game;
const games = [game(1, "Azul"), game(2, "Brass"), game(3, "Carcassonne")];

describe("toGameSelectItems", () => {
	it("lists the games without groups when none are suggested", () => {
		expect(toGameSelectItems(games, [], "Tonight", "All")).toEqual([
			{ value: 1, label: "Azul", image: null, group: undefined },
			{ value: 2, label: "Brass", image: null, group: undefined },
			{ value: 3, label: "Carcassonne", image: null, group: undefined },
		]);
	});

	it("puts the suggested games first in their own group", () => {
		const items = toGameSelectItems(games, [3, 1], "Tonight", "All");

		expect(items.map((x) => [x.label, x.group])).toEqual([
			["Carcassonne", "Tonight"],
			["Azul", "Tonight"],
			["Brass", "All"],
		]);
	});
});
