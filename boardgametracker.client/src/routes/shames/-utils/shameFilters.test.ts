import { describe, expect, it } from "vitest";
import type { Shame } from "@/models";
import {
	countByShow,
	DEFAULT_SHAME_FILTERS,
	filterShames,
	formatRange,
	lastPlayedTone,
	monthOptions,
	oldestNeverPlayed,
} from "./shameFilters";

const now = new Date(2026, 9, 16);

const shame = (overrides: Partial<Shame>): Shame => ({
	id: 1,
	title: "Game",
	image: null,
	additionDate: null,
	price: null,
	lastSessionDate: null,
	playCount: 0,
	minPlayers: null,
	maxPlayers: null,
	minPlayTime: null,
	maxPlayTime: null,
	plannedGameNightId: null,
	plannedFor: null,
	...overrides,
});

const root = shame({ id: 1, title: "Root", additionDate: new Date(2023, 7, 1) });
const everdell = shame({ id: 2, title: "Everdell", additionDate: new Date(2023, 11, 1), price: 52 });
const dune = shame({ id: 3, title: "Dune: Imperium", additionDate: new Date(2026, 0, 5), price: 48 });
const wingspan = shame({
	id: 4,
	title: "Wingspan",
	additionDate: new Date(2021, 1, 1),
	price: 55,
	lastSessionDate: new Date(2025, 10, 1),
	playCount: 6,
});
const terraforming = shame({
	id: 5,
	title: "Terraforming Mars",
	additionDate: new Date(2022, 5, 1),
	price: 45,
	lastSessionDate: new Date(2025, 2, 1),
	playCount: 2,
});
const all = [wingspan, dune, root, terraforming, everdell];

const titles = (shames: Shame[]) => shames.map((item) => item.title);

describe("shameFilters", () => {
	it("offers the setting and only the longer steps", () => {
		expect(monthOptions(6)).toEqual([6, 12, 24]);
		expect(monthOptions(9)).toEqual([9, 12, 24]);
		expect(monthOptions(12)).toEqual([12, 24]);
		expect(monthOptions(36)).toEqual([36]);
	});

	it("counts all, never played and played before", () => {
		expect(countByShow(all)).toEqual({ all: 5, never: 3, played: 2 });
	});

	it("sorts the longest unplayed first: never played by owned date, then the oldest last play", () => {
		expect(titles(filterShames(all, DEFAULT_SHAME_FILTERS, now))).toEqual([
			"Root",
			"Everdell",
			"Dune: Imperium",
			"Terraforming Mars",
			"Wingspan",
		]);
	});

	it("shows only never played or only played games", () => {
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, show: "never" }, now))).toEqual([
			"Root",
			"Everdell",
			"Dune: Imperium",
		]);
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, show: "played" }, now))).toEqual([
			"Terraforming Mars",
			"Wingspan",
		]);
	});

	it("keeps games owned for more than one or two years", () => {
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, owned: "1y" }, now))).toEqual([
			"Root",
			"Everdell",
			"Terraforming Mars",
			"Wingspan",
		]);
		expect(
			titles(
				filterShames(
					[...all, shame({ id: 11, title: "Azul", additionDate: new Date(2025, 0, 1) })],
					{
						...DEFAULT_SHAME_FILTERS,
						owned: "2y",
						sort: "title",
					},
					now,
				),
			),
		).toEqual(["Everdell", "Root", "Terraforming Mars", "Wingspan"]);
	});

	it("drops games without an owned date when an owned filter is set", () => {
		const unknown = shame({ id: 9, title: "Unknown" });
		expect(filterShames([unknown], { ...DEFAULT_SHAME_FILTERS, owned: "1y" }, now)).toEqual([]);
		expect(filterShames([unknown], DEFAULT_SHAME_FILTERS, now)).toEqual([unknown]);
	});

	it("sorts by price paid with unpriced games last", () => {
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, sort: "price" }, now))).toEqual([
			"Wingspan",
			"Everdell",
			"Dune: Imperium",
			"Terraforming Mars",
			"Root",
		]);
		const free = shame({ id: 10, title: "Another unpriced" });
		expect(titles(filterShames([root, free], { ...DEFAULT_SHAME_FILTERS, sort: "price" }, now))).toEqual([
			"Another unpriced",
			"Root",
		]);
	});

	it("sorts by owned since and by title", () => {
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, sort: "owned" }, now))).toEqual([
			"Wingspan",
			"Terraforming Mars",
			"Root",
			"Everdell",
			"Dune: Imperium",
		]);
		expect(titles(filterShames(all, { ...DEFAULT_SHAME_FILTERS, sort: "title" }, now))).toEqual([
			"Dune: Imperium",
			"Everdell",
			"Root",
			"Terraforming Mars",
			"Wingspan",
		]);
	});

	it("does not change the list it was given", () => {
		const input = [...all];
		filterShames(input, { ...DEFAULT_SHAME_FILTERS, sort: "title" }, now);
		expect(input).toEqual(all);
	});

	it("colours the last played pill: red never, amber over a year, grey otherwise", () => {
		expect(lastPlayedTone(root, now)).toBe("bad");
		expect(lastPlayedTone(terraforming, now)).toBe("warn");
		expect(lastPlayedTone(wingspan, now)).toBe("neutral");
	});

	it("finds the never played game owned the longest", () => {
		expect(oldestNeverPlayed(all)).toBe(root);
		expect(oldestNeverPlayed([wingspan])).toBeNull();
	});

	it("formats a range, a single value or nothing", () => {
		expect(formatRange(2, 4)).toBe("2–4");
		expect(formatRange(2, 2)).toBe("2");
		expect(formatRange(null, 5)).toBe("5");
		expect(formatRange(3, null)).toBe("3");
		expect(formatRange(null, null)).toBeNull();
	});
});
