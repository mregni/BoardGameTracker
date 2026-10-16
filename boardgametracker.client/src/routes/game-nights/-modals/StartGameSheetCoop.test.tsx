import { describe, expect, it, vi } from "vitest";
import type { Game } from "@/models";
import { renderWithProviders, screen } from "@/test/test-utils";
import { StartGameSheet } from "./StartGameSheet";

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

const games = [
	{ id: 1, title: "Pandemic", image: null, hasCoopMode: true, coopByDefault: true },
	{ id: 2, title: "Gloomhaven", image: null, hasCoopMode: true, coopByDefault: false },
	{ id: 3, title: "Azul", image: null, hasCoopMode: false, coopByDefault: false },
] as unknown as Game[];

const renderSheet = (initialGameId: number) =>
	renderWithProviders(
		<StartGameSheet
			close={vi.fn()}
			games={games}
			players={[]}
			nightGameIds={[]}
			attendeeIds={[]}
			initialGameId={initialGameId}
			initialPlayerIds={[]}
			isPending={false}
			onStart={vi.fn()}
		/>,
	);

describe("StartGameSheet co-op", () => {
	it("tells a co-op game is finished with a team result", () => {
		renderSheet(1);

		expect(screen.getByText("play.start.coop-default")).toBeInTheDocument();
	});

	it("tells co-op can be switched on for a game that can be played co-op", () => {
		renderSheet(2);

		expect(screen.getByText("play.start.coop-optional")).toBeInTheDocument();
	});

	it("says nothing about co-op for other games", () => {
		renderSheet(3);

		expect(screen.queryByText(/play\.start\.coop/)).not.toBeInTheDocument();
	});
});
