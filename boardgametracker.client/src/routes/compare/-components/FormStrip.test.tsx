import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { createCompareI18n, games, playerOne, playerTwo, sessionRow, translationFor } from "../-test/compareTestSetup";
import { FormStrip } from "./FormStrip";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

const newestFirst = [
	sessionRow({ sessionId: 6, gameId: 11, outcome: "playerTwo" }),
	sessionRow({ sessionId: 5, gameId: 10, outcome: "playerTwo" }),
	sessionRow({ sessionId: 4, gameId: 14, outcome: "both" }),
	sessionRow({ sessionId: 3, gameId: 11, outcome: "other" }),
	sessionRow({ sessionId: 2, gameId: 12, outcome: "playerOne" }),
];

describe("FormStrip", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("shows the last games oldest first with a letter for who won", () => {
		render(<FormStrip sessions={newestFirst} streak={null} playerOne={playerOne} playerTwo={playerTwo} />);

		const tiles = screen.getAllByTestId("form-tile");
		expect(tiles.map((tile) => tile.textContent)).toEqual(["M", "–", "+", "K", "K"]);
		expect(tiles.map((tile) => tile.dataset.outcome)).toEqual(["playerOne", "other", "both", "playerTwo", "playerTwo"]);
		expect(tiles[0]).toHaveAttribute("title", "Brass: Birmingham · Mikhaël");
		expect(tiles[1]).toHaveAttribute("title", "Carcassonne · neither won");
		expect(screen.getByText("Last 5 sessions together", { exact: false })).toBeInTheDocument();
	});

	it("reads the strip out in order for screen readers", () => {
		render(<FormStrip sessions={newestFirst} streak={null} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(
			screen.getByRole("img", {
				name: "Last sessions together, oldest first: Mikhaël, neither won, both won, Kathleen, Kathleen",
			}),
		).toBeInTheDocument();
	});

	it("shows the streak pill and explains each letter in its own tooltip", () => {
		render(
			<FormStrip
				sessions={newestFirst}
				streak={{ side: "playerTwo", count: 2 }}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(screen.getByText("Kathleen 2 in a row")).toBeInTheDocument();
		expect(screen.getAllByTestId("form-tile").every((tile) => tile.getAttribute("title")?.includes(" · "))).toBe(true);
		expect(screen.queryByText(/who won/)).not.toBeInTheDocument();
	});

	it("shows a hatched = for games on the same co-op team", () => {
		render(
			<FormStrip
				sessions={[
					sessionRow({ sessionId: 9, gameId: 14, outcome: "sameTeam", teamWon: true }),
					sessionRow({ sessionId: 8, gameId: 14, outcome: "sameTeam", teamWon: false }),
					sessionRow({ sessionId: 7, outcome: "playerOne" }),
				]}
				streak={null}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		const tiles = screen.getAllByTestId("form-tile");
		expect(tiles.map((tile) => tile.textContent)).toEqual(["M", "=", "="]);
		expect(tiles[2]).toHaveClass("bg-hatch");
		expect(tiles[2]).toHaveAttribute("title", "Pandemic · same team, won");
		expect(tiles[1]).toHaveAttribute("title", "Pandemic · same team, lost");
	});

	it("shows a player's picture instead of the initial when there is one", () => {
		render(
			<FormStrip
				sessions={[sessionRow({ sessionId: 1, outcome: "playerOne" })]}
				streak={{ side: "playerOne", count: 3 }}
				playerOne={{ ...playerOne, image: "/images/mikhael.png" }}
				playerTwo={playerTwo}
			/>,
		);

		const [tile] = screen.getAllByTestId("form-tile");
		expect(tile.textContent).toBe("");
		expect(tile.querySelector("img")).toHaveAttribute("src", "/images/mikhael.png");
		expect(screen.getByText("Mikhaël 3 in a row")).toBeInTheDocument();
		expect(screen.getByText("Last session together", { exact: false })).toBeInTheDocument();
	});

	it("renders nothing without games together", () => {
		const { container } = render(<FormStrip sessions={[]} streak={null} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(container).toBeEmptyDOMElement();
	});
});
