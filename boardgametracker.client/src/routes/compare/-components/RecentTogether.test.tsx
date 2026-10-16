import type { i18n as I18n } from "i18next";
import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import {
	createCompareI18n,
	games,
	playerOne,
	playerTwo,
	reggi,
	sessionRow,
	translationFor,
} from "../-test/compareTestSetup";
import { RecentTogether } from "./RecentTogether";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		params,
		children,
		className,
	}: {
		params: { sessionId: number };
		children: ReactNode;
		className?: string;
	}) => (
		<a href={`/sessions/update/${params.sessionId}`} className={className}>
			{children}
		</a>
	),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id: number) => (id === reggi.id ? reggi : null) }),
}));

const sessions = [
	sessionRow({ sessionId: 7, gameId: 11, outcome: "playerTwo", playerOneScore: 91, playerTwoScore: 98, minutes: 80 }),
	sessionRow({ sessionId: 6, gameId: 10, outcome: "playerOne", playerOneScore: 77, playerTwoScore: 70, minutes: 25 }),
	sessionRow({ sessionId: 5, gameId: 11, outcome: "other", otherWinnerId: reggi.id, minutes: 40 }),
	sessionRow({ sessionId: 4, gameId: 14, outcome: "both", minutes: 50 }),
	sessionRow({ sessionId: 3, gameId: 12, outcome: "other", minutes: 170 }),
	sessionRow({ sessionId: 2, gameId: 12, outcome: "other" }),
];

const renderRecent = (canOpen: boolean) =>
	render(
		<RecentTogether
			sessions={sessions}
			playerOne={playerOne}
			playerTwo={playerTwo}
			dateFormat="dd-MM-yyyy"
			uiLanguage="en-US"
			canOpen={canOpen}
		/>,
	);

describe("RecentTogether", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("shows the five latest games with both scores, the winner and the duration", () => {
		renderRecent(false);

		const rows = screen.getAllByRole("listitem");
		expect(rows).toHaveLength(5);
		expect(rows[0]).toHaveTextContent("16-10-2026");
		expect(rows[0]).toHaveTextContent("Carcassonne · 91 – 98");
		expect(rows[0]).toHaveTextContent("Kathleen");
		expect(rows[0]).toHaveTextContent("1h 20m");
		expect(rows[1]).toHaveTextContent("Azul · 77 – 70");
		expect(rows[1]).toHaveTextContent("Mikhaël");
		expect(rows[2]).toHaveTextContent("reggi");
		expect(rows[3]).toHaveTextContent("Both won");
		expect(rows[4]).toHaveTextContent("No winner");
		expect(rows[4]).toHaveTextContent("2h 50m");
		expect(screen.queryByRole("link")).toBeNull();
	});

	it("shows the co-op result for games on the same team", () => {
		render(
			<RecentTogether
				sessions={[
					sessionRow({ sessionId: 9, gameId: 14, outcome: "sameTeam", teamWon: true }),
					sessionRow({ sessionId: 8, gameId: 14, outcome: "sameTeam", teamWon: false }),
				]}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat="dd-MM-yyyy"
				uiLanguage="en-US"
				canOpen={false}
			/>,
		);

		const [won, lost] = screen.getAllByRole("listitem");
		expect(won).toHaveTextContent("Pandemic · same team");
		expect(won).toHaveTextContent("Co-op · won");
		expect(lost).toHaveTextContent("Co-op · lost");
	});

	it("links each game to its session for people who can edit", () => {
		renderRecent(true);

		expect(screen.getAllByRole("link")[0]).toHaveAttribute("href", "/sessions/update/7");
	});

	it("renders nothing without games together", () => {
		const { container } = render(
			<RecentTogether
				sessions={[]}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat="dd-MM-yyyy"
				uiLanguage="en-US"
				canOpen
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
