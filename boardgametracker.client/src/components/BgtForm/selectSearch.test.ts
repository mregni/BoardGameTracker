import type { KeyboardEvent } from "react";
import { describe, expect, it, vi } from "vitest";
import { findSearchMatch, handleSelectSearchKeyDown } from "./selectSearch";

const games = [
	{ value: 1, label: "Agricola" },
	{ value: 12, label: "Carcassonne" },
	{ value: 14, label: "Catan" },
];

describe("findSearchMatch", () => {
	it("returns the first item whose label contains the search term, ignoring case", () => {
		expect(findSearchMatch(games, "CA")?.value).toBe(12);
		expect(findSearchMatch(games, "tan")?.value).toBe(14);
	});

	it("returns nothing for an empty or whitespace search", () => {
		expect(findSearchMatch(games, "")).toBeUndefined();
		expect(findSearchMatch(games, "   ")).toBeUndefined();
	});

	it("returns nothing when no label matches", () => {
		expect(findSearchMatch(games, "brass")).toBeUndefined();
	});
});

const createKeyEvent = (key: string, currentTarget: HTMLElement = document.createElement("input")) => {
	const event = { key, currentTarget, stopPropagation: vi.fn(), preventDefault: vi.fn() };
	return event as typeof event & KeyboardEvent<HTMLInputElement>;
};

describe("handleSelectSearchKeyDown", () => {
	it.each(["Escape", "Tab"])("lets %s reach the select", (key) => {
		const selectMatch = vi.fn();
		const event = createKeyEvent(key);

		handleSelectSearchKeyDown(event, selectMatch);

		expect(event.stopPropagation).not.toHaveBeenCalled();
		expect(event.preventDefault).not.toHaveBeenCalled();
		expect(selectMatch).not.toHaveBeenCalled();
	});

	it("selects the match on Enter", () => {
		const selectMatch = vi.fn();
		const event = createKeyEvent("Enter");

		handleSelectSearchKeyDown(event, selectMatch);

		expect(event.stopPropagation).toHaveBeenCalled();
		expect(event.preventDefault).toHaveBeenCalled();
		expect(selectMatch).toHaveBeenCalledOnce();
	});

	it("focuses the first option on ArrowDown", () => {
		const listbox = document.createElement("div");
		listbox.setAttribute("role", "listbox");
		const input = document.createElement("input");
		const option = document.createElement("div");
		option.setAttribute("role", "option");
		option.tabIndex = -1;
		listbox.append(input, option);
		document.body.append(listbox);
		const event = createKeyEvent("ArrowDown", input);

		handleSelectSearchKeyDown(event, vi.fn());

		expect(event.preventDefault).toHaveBeenCalled();
		expect(option).toHaveFocus();
		listbox.remove();
	});

	it("keeps typing keys inside the search box", () => {
		const selectMatch = vi.fn();
		const event = createKeyEvent("a");

		handleSelectSearchKeyDown(event, selectMatch);

		expect(event.stopPropagation).toHaveBeenCalled();
		expect(event.preventDefault).not.toHaveBeenCalled();
		expect(selectMatch).not.toHaveBeenCalled();
	});
});
