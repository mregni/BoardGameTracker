import { describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { ActiveFilterChips } from "./GamesFilters";

describe("ActiveFilterChips", () => {
	it("shows one removable chip per active filter, named with its field", async () => {
		const onRemove = vi.fn();
		renderWithProviders(<ActiveFilterChips search={{ q: "cat", weight: "heavy", players: 4 }} onRemove={onRemove} />);

		const chips = screen.getAllByRole("button");
		expect(chips).toHaveLength(2);
		expect(chips[0]).toHaveTextContent("games:filters.player-count: games:filters.player-count-value");
		expect(chips[1]).toHaveTextContent("games:filters.weight: games:filters.weight-heavy");

		await userEvent.click(chips[1]);
		expect(onRemove).toHaveBeenCalledWith("weight");
	});
});
