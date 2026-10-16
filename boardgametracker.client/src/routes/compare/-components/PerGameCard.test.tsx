import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen, userEvent, within } from "@/test/test-utils";
import { createCompareI18n, gameRow, games, playerOne, playerTwo, translationFor } from "../-test/compareTestSetup";
import { PerGameCard } from "./PerGameCard";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

const rows = [
	gameRow({
		gameId: 10,
		sessionCount: 11,
		playerOneWins: 4,
		playerTwoWins: 7,
		playerOneAverageScore: 71,
		playerTwoAverageScore: 78.25,
		lastOutcome: "playerTwo",
		lastPlayed: new Date(2026, 9, 13),
	}),
	gameRow({ gameId: 11, sessionCount: 9, playerOneWins: 3, playerTwoWins: 4, otherWins: 2, lastOutcome: "other" }),
	gameRow({ gameId: 12, sessionCount: 7, playerOneWins: 5, playerTwoWins: 1, otherWins: 1, lastOutcome: "playerOne" }),
	gameRow({ gameId: 13, sessionCount: 6, playerOneWins: 4, playerTwoWins: 2, lastOutcome: "both" }),
	gameRow({ gameId: 14, sessionCount: 3, playerOneWins: 3, playerTwoWins: 3 }),
	gameRow({ gameId: 15, sessionCount: 1, playerTwoWins: 1 }),
];

const renderCard = () =>
	render(
		<PerGameCard games={rows} playerOne={playerOne} playerTwo={playerTwo} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />,
	);

const titles = () => screen.getAllByTestId("per-game-row").map((row) => row.querySelector("strong")?.textContent);

describe("PerGameCard", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("lists the five most played games first", () => {
		renderCard();

		expect(screen.getAllByTestId("per-game-row")).toHaveLength(5);
		expect(titles()).toEqual(["Azul", "Carcassonne", "Brass: Birmingham", "Age of Inventors", "Pandemic"]);
		expect(screen.getByRole("button", { name: "Most played" })).toHaveAttribute("aria-pressed", "true");
	});

	it("sorts by the biggest lead", async () => {
		const user = userEvent.setup();
		renderCard();

		await user.click(screen.getByRole("button", { name: "Biggest lead" }));

		expect(screen.getByRole("button", { name: "Biggest lead" })).toHaveAttribute("aria-pressed", "true");
		expect(titles()).toEqual(["Brass: Birmingham", "Azul", "Age of Inventors", "Carcassonne", "Cascadia"]);
	});

	it("shows all games on request and fewer again", async () => {
		const user = userEvent.setup();
		renderCard();

		await user.click(screen.getByRole("button", { name: "Show all 6 games" }));
		expect(screen.getAllByTestId("per-game-row")).toHaveLength(6);

		await user.click(screen.getByRole("button", { name: "Show less" }));
		expect(screen.getAllByTestId("per-game-row")).toHaveLength(5);
	});

	it("shows counts, averages, other winners and the last result per game", () => {
		renderCard();
		const [azul, carcassonne, brass, inventors] = screen.getAllByTestId("per-game-row");

		expect(azul).toHaveTextContent("11 games · avg 71 – 78.3");
		expect(azul).toHaveTextContent("K · 13-10-2026");
		expect(within(azul).getByRole("img", { name: "Azul: Mikhaël 4, Kathleen 7, someone else 0" })).toBeInTheDocument();
		expect(carcassonne).toHaveTextContent("+2 others");
		expect(carcassonne).toHaveTextContent("– · 10-10-2026");
		expect(brass).toHaveTextContent("+1 other");
		expect(brass).toHaveTextContent("7 games · no scores");
		expect(brass).toHaveTextContent("M · ");
		expect(inventors).toHaveTextContent("= · ");
	});

	it("hides the sort and show-all controls it does not need", () => {
		render(
			<PerGameCard
				games={rows.slice(0, 2)}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat="dd-MM-yyyy"
				uiLanguage="en-US"
			/>,
		);

		expect(screen.queryByRole("button", { name: /Show all/ })).toBeNull();
	});

	it("renders nothing without games", () => {
		const { container } = render(
			<PerGameCard games={[]} playerOne={playerOne} playerTwo={playerTwo} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
