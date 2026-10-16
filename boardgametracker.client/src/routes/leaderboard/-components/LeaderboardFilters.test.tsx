import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import type { LeaderboardSearch } from "../-utils/leaderboardSearch";
import { LeaderboardFilters } from "./LeaderboardFilters";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

const games = [
	{ id: 2, title: "Wingspan", image: "wingspan.webp" },
	{ id: 1, title: "Azul", image: null },
] as unknown as Game[];

const today = new Date(2026, 9, 16);

const renderFilters = (search: LeaderboardSearch = {}) => {
	const onChange = vi.fn();
	renderWithProviders(<LeaderboardFilters search={search} games={games} today={today} onChange={onChange} />);
	return onChange;
};

describe("LeaderboardFilters", () => {
	beforeAll(() => {
		Element.prototype.hasPointerCapture = vi.fn(() => false);
		Element.prototype.setPointerCapture = vi.fn();
		Element.prototype.releasePointerCapture = vi.fn();
		Element.prototype.scrollIntoView = vi.fn();
	});

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should mark all time as the default period and label this year with its number", () => {
		renderFilters();

		expect(screen.getByRole("group", { name: "filters.period" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "filters.all-time" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "2026" })).toHaveAttribute("aria-pressed", "false");
		expect(screen.queryByText("filters.from")).not.toBeInTheDocument();
	});

	it("should switch the period and clear custom dates", async () => {
		const onChange = renderFilters({ period: "custom", from: "2026-01-01", to: "2026-02-01" });

		await userEvent.click(screen.getByRole("button", { name: "filters.last-12-months" }));
		expect(onChange).toHaveBeenLastCalledWith({ period: "12m", from: undefined, to: undefined });

		await userEvent.click(screen.getByRole("button", { name: "filters.all-time" }));
		expect(onChange).toHaveBeenLastCalledWith({ period: undefined, from: undefined, to: undefined });
	});

	it("should keep the custom dates when choosing custom again", async () => {
		const onChange = renderFilters({ from: "2026-01-01", to: "2026-02-01" });

		await userEvent.click(screen.getByRole("button", { name: "filters.custom" }));

		expect(onChange).toHaveBeenCalledWith({ period: "custom", from: "2026-01-01", to: "2026-02-01" });
	});

	it("should show two date pickers for a custom period and clear a date", async () => {
		const onChange = renderFilters({ period: "custom", from: "2026-01-01", to: "2026-02-01" });

		expect(screen.getByRole("group", { name: "filters.from" })).toBeInTheDocument();
		expect(screen.getByRole("group", { name: "filters.to" })).toBeInTheDocument();

		await userEvent.click(screen.getAllByRole("button", { name: "common:clear-date" })[0]);
		expect(onChange).toHaveBeenCalledWith({ from: undefined });

		await userEvent.click(screen.getAllByRole("button", { name: "common:clear-date" })[1]);
		expect(onChange).toHaveBeenCalledWith({ to: undefined });
	});

	it("should list all games first and then the games by title", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters();

		expect(screen.getByRole("combobox", { name: "filters.game" })).toHaveTextContent("filters.all-games");
		await user.click(screen.getByRole("combobox", { name: "filters.game" }));
		const options = await screen.findAllByRole("option");
		expect(options.map((option) => option.textContent)).toEqual(["filters.all-games", "Azul", "Wingspan"]);

		await user.click(options[2]);
		expect(onChange).toHaveBeenCalledWith({ game: 2 });
	});

	it("should clear the game when all games is picked again", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters({ game: 2 });

		await user.click(screen.getByRole("combobox", { name: "filters.game" }));
		await user.click(await screen.findByRole("option", { name: "filters.all-games" }));

		expect(onChange).toHaveBeenCalledWith({ game: undefined });
	});

	it("should offer 1, 3, 5 and 10 sessions with 5 as the default", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters();

		await user.click(screen.getByRole("combobox", { name: "filters.min-sessions" }));
		expect((await screen.findAllByRole("option")).length).toBe(4);
		await user.click(screen.getAllByRole("option")[3]);
		expect(onChange).toHaveBeenCalledWith({ min: 10 });
	});

	it("should drop the minimum from the search when it goes back to the default", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters({ min: 10 });

		await user.click(screen.getByRole("combobox", { name: "filters.min-sessions" }));
		await user.click((await screen.findAllByRole("option"))[2]);

		expect(onChange).toHaveBeenCalledWith({ min: undefined });
	});

	it("should rank by wins by default and switch to win rate or sessions", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters();

		expect(screen.getByRole("combobox", { name: "filters.rank-by" })).toHaveTextContent("filters.rank-by-wins");
		await user.click(screen.getByRole("combobox", { name: "filters.rank-by" }));
		await user.click(await screen.findByRole("option", { name: "filters.rank-by-win-rate" }));
		expect(onChange).toHaveBeenCalledWith({ rank: "winRate" });
	});

	it("should drop the ranking from the search when it goes back to wins", async () => {
		const user = userEvent.setup();
		const onChange = renderFilters({ rank: "sessions" });

		await user.click(screen.getByRole("combobox", { name: "filters.rank-by" }));
		await user.click(await screen.findByRole("option", { name: "filters.rank-by-wins" }));

		expect(onChange).toHaveBeenCalledWith({ rank: undefined });
	});
});
