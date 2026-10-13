import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type CreateSession, type GameNightDetail, GameNightRsvpState, type RunningGame } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	beforeLoad: (args: { context: { queryClient: unknown } }) => Promise<void>;
	loader: (args: { params: { gameNightId: number }; context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	navigate: vi.fn(),
	startGame: vi.fn(),
	finishGame: vi.fn(),
	discardGame: vi.fn(),
	saveSession: vi.fn(),
	deleteGameNight: vi.fn(),
	invalidateGameNights: vi.fn(),
	onNewSessionSuccess: undefined as undefined | (() => void),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ gameNightId: 4 }) };
	},
	redirect: (options: unknown) => ({ redirect: options }),
	useNavigate: () => mocks.navigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/session">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateGameNights: mocks.invalidateGameNights }),
}));

const runningGame = { id: 12, gameNightId: 4, gameId: 7, start: new Date(), playerIds: [1] } as RunningGame;

const detail = {
	gameNight: {
		id: 4,
		title: "Strategy night",
		notes: "",
		startDate: new Date(),
		hostId: 1,
		host: { id: 1, name: "Mikhael", image: null },
		locationId: 5,
		location: { id: 5, name: "Thuis" },
		linkId: "abc",
		suggestedGames: [],
		invitedPlayers: [
			{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: { id: 1, name: "Mikhael" } },
		],
		summary: { gamesPlayed: 0, totalMinutes: 0, mostWins: 0, winnerIds: [] },
	},
	sessions: [],
	runningGames: [runningGame],
} as unknown as GameNightDetail;

vi.mock("./-hooks/useGameNightData", () => ({
	useGameNightData: () => ({
		settings: { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-us" },
		players: [{ id: 1, name: "Mikhael", image: null }],
		games: [{ id: 7, title: "Azul", image: null }],
		locations: [],
		isLoading: false,
		updateGameNight: vi.fn(),
		updateInvitedPlayers: vi.fn(),
		updateRsvp: vi.fn(),
		deleteGameNight: mocks.deleteGameNight,
		isUpdating: false,
	}),
}));

vi.mock("./-hooks/useGameNightDetail", () => ({
	useGameNightDetail: () => ({
		detail,
		isLoading: false,
		startGame: mocks.startGame,
		finishGame: mocks.finishGame,
		discardGame: mocks.discardGame,
		isPending: false,
	}),
}));

vi.mock("@/routes/sessions/-hooks/useNewSessionData", () => ({
	useNewSessionData: ({ onSuccess }: { onSuccess: () => void }) => {
		mocks.onNewSessionSuccess = onSuccess;
		return { saveSession: mocks.saveSession, isPending: false };
	},
}));

vi.mock("./-modals/EditGameNightModal", () => ({ EditGameNightModal: () => null }));
vi.mock("./-modals/ManageRSVPsModal", () => ({ ManageRSVPsModal: () => null }));

vi.mock("./-modals/StartGameSheet", () => ({
	StartGameSheet: ({ onStart }: { onStart: (command: unknown) => void }) => (
		<button type="button" onClick={() => onStart({ gameId: 7, playerIds: [1] })}>
			start-sheet
		</button>
	),
}));

const session = {
	gameId: 7,
	start: new Date(2026, 9, 16, 21, 0),
	minutes: 40,
	playerSessions: [],
} as unknown as CreateSession;

vi.mock("./-modals/PlaySessionSheet", () => ({
	PlaySessionSheet: ({ onSave }: { onSave: (session: CreateSession, timesChanged: boolean) => void }) => (
		<>
			<button type="button" onClick={() => onSave(session, false)}>
				save-now
			</button>
			<button type="button" onClick={() => onSave(session, true)}>
				save-changed
			</button>
		</>
	),
}));

vi.mock("@/services/queries/gameNights", () => ({
	getGameNight: (id: number) => ({ queryKey: ["gameNights", id] }),
}));
vi.mock("@/services/queries/games", () => ({ getGames: () => ({ queryKey: ["games"] }) }));
vi.mock("@/services/queries/players", () => ({ getPlayers: () => ({ queryKey: ["players"] }) }));
vi.mock("@/services/queries/locations", () => ({ getLocations: () => ({ queryKey: ["locations"] }) }));
vi.mock("@/services/queries/settings", () => ({ getSettings: () => ({ queryKey: ["settings"] }) }));

await import("./$gameNightId");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Game night detail page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.startGame.mockResolvedValue(undefined);
		mocks.finishGame.mockResolvedValue(undefined);
		mocks.discardGame.mockResolvedValue(undefined);
		mocks.saveSession.mockResolvedValue(undefined);
		mocks.deleteGameNight.mockResolvedValue(undefined);
	});

	it("prefetches the night, players, games and locations", () => {
		const queryClient = { prefetchQuery: vi.fn() };

		mocks.captured.config.loader({ params: { gameNightId: 4 }, context: { queryClient } });

		expect(queryClient.prefetchQuery.mock.calls.map(([options]) => options.queryKey)).toEqual([
			["gameNights", 4],
			["players"],
			["games"],
			["locations"],
		]);
	});

	it("sends the visitor home when game nights are turned off", async () => {
		const queryClient = { ensureQueryData: vi.fn().mockResolvedValue({ gameNightsEnabled: false }) };

		await expect(mocks.captured.config.beforeLoad({ context: { queryClient } })).rejects.toEqual({
			redirect: { to: "/" },
		});
	});

	it("goes back to the game nights", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: /detail.back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/game-nights" });
	});

	it("starts a game from the start sheet", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: "play.start.button" }));
		await user.click(screen.getByRole("button", { name: "start-sheet" }));

		expect(mocks.startGame).toHaveBeenCalledWith({ gameId: 7, playerIds: [1] });
	});

	it("finishes the running game now, or with the changed times", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: "play.finish.button" }));
		await user.click(screen.getByRole("button", { name: "save-now" }));
		await user.click(screen.getByRole("button", { name: "save-changed" }));

		expect(mocks.finishGame).toHaveBeenNthCalledWith(1, {
			id: 12,
			command: { playerSessions: [], start: null, minutes: null },
		});
		expect(mocks.finishGame).toHaveBeenNthCalledWith(2, {
			id: 12,
			command: { playerSessions: [], start: session.start, minutes: 40 },
		});
	});

	it("logs a finished game as a session of the night and refreshes the night", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: "play.log.button" }));
		await user.click(screen.getByRole("button", { name: "save-changed" }));
		mocks.onNewSessionSuccess?.();

		expect(mocks.saveSession).toHaveBeenCalledWith(session);
		expect(mocks.invalidateGameNights).toHaveBeenCalled();
	});

	it("discards the running game after confirming", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: "play.discard.button" }));
		await user.click(await screen.findByRole("button", { name: "delete.button" }));

		await waitFor(() => expect(mocks.discardGame).toHaveBeenCalledWith(12));
	});

	it("deletes the night after confirming and goes back to the overview", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(screen.getByRole("button", { name: "delete.button" }));
		const buttons = await screen.findAllByRole("button", { name: "delete.button" });
		await user.click(buttons.at(-1) as HTMLElement);

		await waitFor(() => expect(mocks.deleteGameNight).toHaveBeenCalledWith(4));
		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/game-nights" }));
	});
});
