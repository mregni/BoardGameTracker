import { createRef } from "react";
import { describe, expect, it, vi } from "vitest";

import { fireEvent, renderWithTheme, screen } from "@/test/test-utils";
import { ActiveFilterChips, type GamesFilterSearch } from "./GamesFilters";

describe("ActiveFilterChips", () => {
	const setup = (search: GamesFilterSearch, extra: Partial<Parameters<typeof ActiveFilterChips>[0]> = {}) => {
		const focusRef = createRef<number | null>() as { current: number | null };
		focusRef.current = null;
		const props = { categories: ["Strategy"], search, onChange: vi.fn(), focusRef, ...extra };
		return { props, ...renderWithTheme(<ActiveFilterChips {...props} />) };
	};

	it("renders nothing without active filters", () => {
		setup({});
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("renders one chip per active filter, including show", () => {
		setup({ category: "Strategy", players: 2, age: "0-6", show: "wanted" });
		expect(screen.getAllByRole("button")).toHaveLength(4);
	});

	it("ignores search text and sort", () => {
		setup({ q: "cat", sort: "plays" });
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("clears only the clicked filter", () => {
		const { props } = setup({ players: 2, weight: "light" });

		fireEvent.click(screen.getAllByRole("button")[0]);

		expect(props.onChange).toHaveBeenCalledWith({ players: undefined });
	});

	it("moves focus to the next chip after removing one", () => {
		const { props, rerender } = setup({ players: 2, weight: "light" });

		fireEvent.click(screen.getAllByRole("button")[0]);
		rerender(<ActiveFilterChips {...props} search={{ weight: "light" }} />);

		expect(screen.getByRole("button")).toHaveFocus();
	});

	it("keeps the pending focus when another chip list takes over", () => {
		const { props, unmount } = setup({ players: 2, weight: "light" }, { counts: { players: 3, weight: 0 } });

		fireEvent.click(screen.getAllByRole("button")[0]);
		unmount();
		renderWithTheme(<ActiveFilterChips {...props} counts={undefined} search={{ weight: "light" }} />);

		expect(screen.getByRole("button")).toHaveFocus();
	});

	it("calls onEmpty after removing the last chip", () => {
		const onEmpty = vi.fn();
		const { props, rerender } = setup({ players: 2 }, { onEmpty });

		fireEvent.click(screen.getByRole("button"));
		rerender(<ActiveFilterChips {...props} search={{}} />);

		expect(onEmpty).toHaveBeenCalledOnce();
	});

	it("disables chips whose removal still matches no games", () => {
		setup({ players: 2, weight: "light" }, { counts: { players: 0, weight: 3 } });

		const [players, weight] = screen.getAllByRole("button");
		expect(players).toBeDisabled();
		expect(weight).toBeEnabled();
	});
});
