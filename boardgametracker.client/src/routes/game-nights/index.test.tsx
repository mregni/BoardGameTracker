import { addDays, addHours } from "date-fns";
import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState } from "@/models";
import { renderWithProviders, screen, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	gameNights: [] as GameNight[],
	navigate: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	redirect: vi.fn(),
	useNavigate: () => mocks.navigate,
	Link: ({ children, params }: { children: React.ReactNode; params: { gameNightId: number } }) => (
		<a href={`/game-nights/${params.gameNightId}`}>{children}</a>
	),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: false }),
}));

vi.mock("./-hooks/useGameNightData", () => ({
	useGameNightData: () => ({
		gameNights: mocks.gameNights,
		settings: { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-us" },
		players: [{ id: 1, name: "Mikhael", image: null }],
		games: [],
		locations: [],
		isLoading: false,
		createGameNight: vi.fn(),
		deleteGameNight: vi.fn(),
		updateGameNight: vi.fn(),
		updateInvitedPlayers: vi.fn(),
		updateRsvp: vi.fn(),
		isCreating: false,
		isUpdating: false,
	}),
}));

vi.mock("./-modals/CreateGameNightModal", () => ({ CreateGameNightModal: () => null }));
vi.mock("./-modals/EditGameNightModal", () => ({ EditGameNightModal: () => null }));
vi.mock("./-modals/ManageRSVPsModal", () => ({ ManageRSVPsModal: () => null }));
vi.mock("./-components/GameNightActions", () => ({ GameNightActions: () => null }));

await import("./index");

const night = (id: number, title: string, startDate: Date, endedAt: Date | null = null) =>
	({
		id,
		title,
		notes: "",
		startDate,
		endedAt,
		hostId: 1,
		host: { id: 1, name: "Mikhael", image: null },
		locationId: 5,
		location: { id: 5, name: "Thuis" },
		linkId: "abc",
		suggestedGames: [],
		invitedPlayers: [
			{ id: 11, playerId: 1, gameNightId: id, state: GameNightRsvpState.Accepted, player: { id: 1, name: "Mikhael" } },
		],
		summary: { gamesPlayed: 0, totalMinutes: 0, mostWins: 0, winnerIds: [] },
	}) as unknown as GameNight;

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Game nights overview", () => {
	beforeEach(() => {
		mocks.navigate.mockReset();
	});

	it("keeps a night that is being played under upcoming and lists ended and old nights as past", () => {
		mocks.gameNights = [
			night(1, "Last month", addDays(new Date(), -30)),
			night(2, "Tonight", addHours(new Date(), -2)),
			night(3, "Next week", addDays(new Date(), 7)),
			night(4, "Ended early", addHours(new Date(), -3), addHours(new Date(), -1)),
		];

		renderPage();

		const upcoming = screen.getByRole("region", { name: "upcoming" });
		const past = screen.getByRole("region", { name: "past" });
		expect(
			within(upcoming)
				.getAllByRole("heading", { level: 3 })
				.map((x) => x.textContent),
		).toEqual(["Tonight", "Next week"]);
		expect(within(past).getByText("Ended early")).toBeInTheDocument();
		expect(within(past).getByText("Last month")).toBeInTheDocument();
		expect(within(upcoming).getAllByRole("button", { name: "card.open" })).toHaveLength(2);
	});

	it("says there is nothing upcoming when every night is past", () => {
		mocks.gameNights = [night(1, "Last month", addDays(new Date(), -30))];

		renderPage();

		expect(screen.getByText("empty.upcoming")).toBeInTheDocument();
	});
});
