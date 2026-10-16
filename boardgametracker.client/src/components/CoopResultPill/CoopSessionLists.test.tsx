import { describe, expect, it, vi } from "vitest";
import type { Game, Player, RecentActivity, Session } from "@/models";
import { LastSessionsCard } from "@/routes/-components/dashboard/LastSessionsCard";
import { PlayedSessionsList } from "@/routes/game-nights/-components/PlayedSessionsList";
import { LocationSessionsCard } from "@/routes/locations/-components/LocationSessionsCard";
import { PlayerSessionCardItem } from "@/routes/players/-components/PlayerSessionCardItem";
import { renderWithTheme, screen } from "@/test/test-utils";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, className }: { children: React.ReactNode; className?: string }) => (
		<a href="/" className={className}>
			{children}
		</a>
	),
	useNavigate: () => vi.fn(),
}));

const players = [
	{ id: 1, name: "Kathleen", image: null },
	{ id: 2, name: "Mikhaël", image: null },
] as unknown as Player[];
const games = [{ id: 3, title: "Pandemic", image: null }] as unknown as Game[];

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id: number) => players.find((player) => player.id === id) }),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games.find((game) => game.id === id) }),
}));

const coopSession = (id: number, teamWon: boolean): Session =>
	({
		id,
		gameId: 3,
		start: new Date(2026, 9, 5, 20, 0),
		minutes: 60,
		isCoop: true,
		teamWon,
		teamScore: null,
		playerSessions: [
			{ sessionId: id, playerId: 1, won: teamWon, firstPlay: false, score: null },
			{ sessionId: id, playerId: 2, won: teamWon, firstPlay: false, score: null },
		],
	}) as unknown as Session;

describe("co-op sessions in session lists", () => {
	it("shows the team result instead of the winners on the game night", () => {
		renderWithTheme(
			<PlayedSessionsList
				sessions={[coopSession(1, true), coopSession(2, false)]}
				games={games}
				players={players}
				uiLanguage="en-US"
				canOpen={false}
				emptyText="none"
			/>,
		);

		const [first, second] = screen.getAllByTestId("played-session");
		expect(first).toHaveTextContent("coop.won");
		expect(second).toHaveTextContent("coop.lost");
		expect(first.querySelector(".text-ok")).not.toBeNull();
	});

	it("shows the team result on the location page", () => {
		renderWithTheme(
			<LocationSessionsCard sessions={[coopSession(1, false)]} dateFormat="yy-MM-dd" uiLanguage="en-US" canOpen />,
		);

		const row = screen.getByRole("listitem");
		expect(row).toHaveTextContent("coop.lost");
		expect(row).not.toHaveTextContent("Kathleen");
	});

	it("shows the team result on the player page", () => {
		renderWithTheme(
			<PlayerSessionCardItem
				session={coopSession(1, true)}
				game={games[0]}
				dateFormat="yy-MM-dd"
				uiLanguage="en-US"
				playerId={1}
			/>,
		);

		expect(screen.getByText("coop.won")).toBeInTheDocument();
		expect(screen.queryByText("won")).not.toBeInTheDocument();
	});

	it("shows the team result in the last sessions", () => {
		const activity: RecentActivity = {
			id: 1,
			gameId: 3,
			gameTitle: "Pandemic",
			gameImage: null,
			start: new Date(2026, 9, 5, 20, 0),
			playerCount: 2,
			durationInMinutes: 60,
			winners: [],
			isCoop: true,
			teamWon: false,
		};

		renderWithTheme(<LastSessionsCard sessions={[activity]} dateFormat="yy-MM-dd" uiLanguage="en-US" />);

		expect(screen.getByText("Pandemic")).toBeInTheDocument();
		expect(screen.getByText("coop.lost")).toBeInTheDocument();
	});
});
