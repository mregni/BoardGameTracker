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
		scoredSessionCount: 4,
		lastOutcome: "playerTwo",
		lastPlayed: new Date(2026, 9, 13),
	}),
	gameRow({ gameId: 11, sessionCount: 9, playerOneWins: 3, playerTwoWins: 4, otherWins: 2, lastOutcome: "other" }),
	gameRow({ gameId: 12, sessionCount: 7, playerOneWins: 5, playerTwoWins: 1, otherWins: 1, lastOutcome: "playerOne" }),
	gameRow({ gameId: 13, sessionCount: 6, playerOneWins: 4, playerTwoWins: 2, lastOutcome: "both" }),
	gameRow({ gameId: 14, sessionCount: 3, playerOneWins: 3, playerTwoWins: 3 }),
	gameRow({ gameId: 15, sessionCount: 1, playerTwoWins: 1 }),
];

const renderCard = () => render(<PerGameCard games={rows} playerOne={playerOne} playerTwo={playerTwo} />);

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

		await user.click(screen.getByRole("button", { name: "Biggest difference" }));

		expect(screen.getByRole("button", { name: "Biggest difference" })).toHaveAttribute("aria-pressed", "true");
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

	it("shows the scores of a single session without calling them an average", () => {
		render(
			<PerGameCard
				games={[
					gameRow({
						gameId: 10,
						sessionCount: 1,
						playerOneWins: 1,
						playerOneAverageScore: 6,
						playerTwoAverageScore: 10,
						scoredSessionCount: 1,
					}),
				]}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(screen.getByTestId("per-game-row")).toHaveTextContent("1 session · 6 – 10");
		expect(screen.getByTestId("per-game-row")).not.toHaveTextContent("avg");
	});

	it("leaves out an average of only two scored sessions", () => {
		render(
			<PerGameCard
				games={[
					gameRow({
						gameId: 10,
						sessionCount: 3,
						playerOneAverageScore: 6,
						playerTwoAverageScore: 10,
						scoredSessionCount: 2,
					}),
				]}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		const row = screen.getByTestId("per-game-row");
		expect(row.querySelector("small")).toHaveTextContent(/^3 sessions$/);
	});

	it("shows counts, averages and games not won by either", () => {
		renderCard();
		const [azul, carcassonne, brass] = screen.getAllByTestId("per-game-row");

		expect(azul).toHaveTextContent("11 sessions · avg 71 – 78.3");
		expect(
			within(azul).getByRole("img", { name: "Azul: Mikhaël 4, Kathleen 7, not won by either 0" }),
		).toBeInTheDocument();
		expect(carcassonne).toHaveTextContent("2 not won by either");
		expect(brass).toHaveTextContent("1 not won by either");
		expect(brass).toHaveTextContent("7 sessions · no scores");
	});

	it("shows a co-op game as same team with a hatched bar and the co-op result", () => {
		render(
			<PerGameCard
				games={[
					gameRow({
						gameId: 14,
						sessionCount: 3,
						sameTeamCount: 3,
						sameTeamWins: 2,
						lastOutcome: "sameTeam",
						lastTeamWon: true,
					}),
					gameRow({ gameId: 10, sessionCount: 5, sameTeamCount: 2, playerOneWins: 2, playerTwoWins: 1 }),
				]}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		const [mixed, pandemic] = screen.getAllByTestId("per-game-row");
		expect(pandemic).toHaveTextContent("3 sessions · co-op");
		expect(pandemic).toHaveTextContent("Same team · won 2 of 3");
		const segments = within(pandemic)
			.getAllByTestId("progress-segment")
			.filter((s) => s.dataset.visible === "true");
		expect(segments.map((segment) => segment.dataset.pattern)).toEqual(["hatch"]);
		expect(within(pandemic).getByRole("img", { name: "Pandemic: Same team · won 2 of 3" })).toBeInTheDocument();
		expect(mixed).toHaveTextContent("+2 same team");
	});

	it("hides the sort and show-all controls it does not need", () => {
		render(<PerGameCard games={rows.slice(0, 2)} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(screen.queryByRole("button", { name: /Show all/ })).toBeNull();
	});

	it("renders nothing without games", () => {
		const { container } = render(<PerGameCard games={[]} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(container).toBeEmptyDOMElement();
	});
});
