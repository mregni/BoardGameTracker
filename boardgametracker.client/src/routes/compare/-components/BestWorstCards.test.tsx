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

	it("shows each player's best games and their worst, which are the other's best", () => {
		render(<BestWorstCards compare={compare} playerOne={playerOne} playerTwo={playerTwo} />);

		const one = screen.getByRole("region", { name: "Mikhaël against Kathleen" });
		const two = screen.getByRole("region", { name: "Kathleen against Mikhaël" });
		const [oneBest, oneWorst] = within(one).getAllByRole("list");
		const texts = (list: HTMLElement) =>
			within(list)
				.getAllByRole("listitem")
				.map((item) => item.textContent?.slice(1));
		expect(texts(oneBest)).toEqual(["Brass: Birmingham5 – 1", "Age of Inventors4 – 2"]);
		expect(texts(oneWorst)).toEqual(["Azul4 – 7", "Carcassonne3 – 4"]);
		const [twoBest] = within(two).getAllByRole("list");
		expect(within(twoBest).getAllByRole("listitem")[0]).toHaveTextContent("Azul7 – 4");
		expect(screen.getByText("Only games played together at least 3 times.")).toBeInTheDocument();
	});

	it("says so when a player has no best games yet", () => {
		render(
			<BestWorstCards
				compare={{ ...compare, bestGames: { playerOne: [12], playerTwo: [] } }}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(screen.getAllByText("Nothing yet")).toHaveLength(2);
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
