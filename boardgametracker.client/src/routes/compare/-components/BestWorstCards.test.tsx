import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@/test/test-utils";
import {
	compareResult,
	createCompareI18n,
	gameRow,
	games,
	playerOne,
	playerTwo,
	translationFor,
} from "../-test/compareTestSetup";
import { BestWorstCards } from "./BestWorstCards";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

const compare = compareResult({
	games: [
		gameRow({ gameId: 10, sessionCount: 11, playerOneWins: 4, playerTwoWins: 7 }),
		gameRow({ gameId: 11, sessionCount: 9, playerOneWins: 3, playerTwoWins: 4 }),
		gameRow({ gameId: 12, sessionCount: 7, playerOneWins: 5, playerTwoWins: 1 }),
		gameRow({ gameId: 13, sessionCount: 6, playerOneWins: 4, playerTwoWins: 2 }),
	],
	bestGames: { playerOne: [12, 13], playerTwo: [10, 11] },
});

describe("BestWorstCards", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("shows each player's strongest games with their record against the other", () => {
		render(<BestWorstCards compare={compare} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(screen.getByRole("region", { name: "Strongest games" })).toBeInTheDocument();
		const games = (side: string) =>
			within(screen.getByTestId(`best-worst-${side}`))
				.getAllByRole("listitem")
				.map((item) => item.textContent?.slice(1));
		expect(games("playerOne")).toEqual(["Brass: Birmingham5 – 1", "Age of Inventors4 – 2"]);
		expect(games("playerTwo")).toEqual(["Azul7 – 4", "Carcassonne4 – 3"]);
		expect(screen.getByTestId("best-worst-playerOne")).toHaveTextContent(/^Mikhaël/);
		expect(screen.getByText("Only games played together at least 3 times.")).toBeInTheDocument();
	});

	it("says so when a player has no strongest games yet", () => {
		render(
			<BestWorstCards
				compare={{ ...compare, bestGames: { playerOne: [12], playerTwo: [] } }}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(within(screen.getByTestId("best-worst-playerTwo")).getByText("Nothing yet")).toBeInTheDocument();
	});

	it("is hidden when no game was played often enough", () => {
		const { container } = render(
			<BestWorstCards
				compare={{ ...compare, bestGames: { playerOne: [], playerTwo: [] } }}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
