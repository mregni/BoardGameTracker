import { describe, expect, it } from "vitest";
import type { Game, Player, Session } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { SessionWinnersCell } from "./SessionWinnersCell";

const players = [
	{ id: 1, name: "Kathleen", image: null },
	{ id: 2, name: "Mikhaël", image: null },
] as unknown as Player[];
const games = [{ id: 3, title: "Azul", image: null, hasScoring: false }] as unknown as Game[];

const session = (overrides: Partial<Session>) =>
	({
		gameId: 3,
		isCoop: false,
		teamWon: null,
		playerSessions: [
			{ sessionId: 1, playerId: 1, won: true, firstPlay: false, score: null },
			{ sessionId: 1, playerId: 2, won: false, firstPlay: false, score: null },
		],
		...overrides,
	}) as Session;

describe("SessionWinnersCell", () => {
	it("shows the winners of a competitive session", () => {
		renderWithTheme(<SessionWinnersCell session={session({})} players={players} games={games} />);

		expect(screen.getByTitle("Kathleen")).toBeInTheDocument();
		expect(screen.queryByTitle("Mikhaël")).not.toBeInTheDocument();
		expect(screen.queryByText(/coop\./)).not.toBeInTheDocument();
	});

	it("shows the team result instead of the winners for a co-op session", () => {
		renderWithTheme(
			<SessionWinnersCell session={session({ isCoop: true, teamWon: false })} players={players} game={games[0]} />,
		);

		expect(screen.getByText("coop.lost")).toBeInTheDocument();
		expect(screen.queryByTitle("Kathleen")).not.toBeInTheDocument();
	});
});
