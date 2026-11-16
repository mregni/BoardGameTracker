import { fireEvent } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { type Game, GameState } from "@/models";
import { renderWithProviders, screen } from "@/test/test-utils";
import { NeedsAPlaceCard } from "./NeedsAPlaceCard";

vi.mock("@/components/BgtStorage/StoragePicker", () => ({ StoragePicker: () => null }));

const game = (id: number, title: string) => ({ id, title, image: null, state: GameState.Owned }) as unknown as Game;
const wingspan = game(1, "Wingspan");
const root = game(2, "Root");
const azul = game(3, "Azul");

const card = (games: Game[]) => (
	<NeedsAPlaceCard
		games={games}
		checked={new Set()}
		flashId={null}
		onFlashEnd={vi.fn()}
		canWrite
		onToggle={vi.fn()}
		onMove={vi.fn()}
	/>
);

describe("NeedsAPlaceCard", () => {
	it("keeps a moved game in place until its row has folded away", () => {
		const { rerender } = renderWithProviders(card([wingspan, root, azul]));

		rerender(card([wingspan, azul]));

		const rows = screen.getAllByRole("listitem", { hidden: true });
		expect(rows.map((row) => row.textContent?.slice(1))).toEqual(["Wingspan", "Root", "Azul"]);
		expect(rows[1]).toHaveAttribute("inert");
		expect(screen.getByText("2")).toBeInTheDocument();

		fireEvent.animationEnd(rows[1]);

		expect(screen.getAllByRole("listitem").map((row) => row.textContent?.slice(1))).toEqual(["Wingspan", "Azul"]);
	});

	it("shows the done state once the last row has gone", () => {
		const { rerender } = renderWithProviders(card([root]));

		rerender(card([]));
		expect(screen.queryByText("inbox.done")).not.toBeInTheDocument();

		fireEvent.animationEnd(screen.getByRole("listitem", { hidden: true }));
		expect(screen.getByRole("status")).toHaveTextContent("inbox.done");
		expect(screen.getByRole("status")).toHaveTextContent("inbox.put-away");
	});

	it("stays quiet when nothing needed a place to begin with", () => {
		renderWithProviders(card([]));

		expect(screen.getByText("inbox.done")).toBeInTheDocument();
		expect(screen.queryByRole("status")).not.toBeInTheDocument();
	});
});
