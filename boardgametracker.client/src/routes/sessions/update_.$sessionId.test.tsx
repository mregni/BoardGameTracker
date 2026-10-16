import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Location, Player, Session } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
}

const deferred = <T,>() => {
	let resolve: (value: T) => void = () => {};
	const promise = new Promise<T>((res) => {
		resolve = res;
	});
	return { promise, resolve };
};

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	navigate: vi.fn(),
	updateSessionCall: vi.fn(),
	invalidateSession: vi.fn(),
	gamesList: null as unknown as Promise<unknown>,
	locationsList: null as unknown as Promise<unknown>,
	playersList: null as unknown as Promise<unknown>,
	gameNightId: null as number | null,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ sessionId: 9 }) };
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn() } }),
	Navigate: () => null,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateSession: mocks.invalidateSession }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
}));

vi.mock("@/services/sessionService", () => ({
	updateSessionCall: (session: Session) => mocks.updateSessionCall(session),
}));

const carcassonne = { id: 1, title: "Carcassonne", image: null, hasScoring: true, maxPlayTime: 45, expansions: [] };
const azul = { id: 4, title: "Azul", image: null, hasScoring: true, maxPlayTime: 40, expansions: [] };
const locations = [
	{ id: 2, name: "Home", playCount: 1 },
	{ id: 6, name: "Café De Spelerij", playCount: 3 },
];
const players = [
	{ id: 7, name: "Mikhael", image: null, email: null, badges: [] },
	{ id: 8, name: "Reggi", image: null, email: null, badges: [] },
	{ id: 9, name: "Nova", image: null, email: null, badges: [] },
];

const session: Session = {
	id: 9,
	comment: "Close game",
	gameId: 4,
	start: new Date(2026, 9, 10, 20, 15),
	end: new Date(2026, 9, 10, 21, 30),
	minutes: 75,
	locationId: 6,
	playerSessions: [
		{ sessionId: 9, playerId: 7, won: false, firstPlay: true, score: 31 },
		{ sessionId: 9, playerId: 8, won: true, firstPlay: false, score: 48 },
	],
	expansions: [],
	gameNightId: null,
};

vi.mock("@/services/queries/sessions", () => ({
	getSession: (id: number) => ({
		queryKey: ["sessions", id],
		queryFn: () => Promise.resolve({ ...session, gameNightId: mocks.gameNightId }),
	}),
	getSessionSuggestions: () => ({
		queryKey: ["sessions", "suggestions"],
		queryFn: () => Promise.resolve({ recentGameIds: [], frequentPlayerIds: [], lastLocationId: 2 }),
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGame: (id: number) => ({ queryKey: ["game", id], queryFn: () => Promise.resolve({ ...azul, id }) }),
	getGames: () => ({ queryKey: ["games"], queryFn: () => mocks.gamesList }),
}));

vi.mock("@/services/queries/locations", () => ({
	getLocations: () => ({ queryKey: ["locations"], queryFn: () => mocks.locationsList }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => mocks.playersList }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-US" }),
	}),
}));

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

await import("./update_.$sessionId");

const renderPage = async () => {
	const games = deferred<Game[]>();
	const locationList = deferred<Location[]>();
	const playerList = deferred<Player[]>();
	mocks.gamesList = games.promise;
	mocks.locationsList = locationList.promise;
	mocks.playersList = playerList.promise;

	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);

	await screen.findByRole("combobox", { name: "new.game.label" });
	games.resolve([carcassonne, azul] as unknown as Game[]);
	locationList.resolve(locations as unknown as Location[]);
	playerList.resolve(players as unknown as Player[]);
	await screen.findByText("Mikhael");
};

const segment = (group: string, name: RegExp) =>
	within(screen.getByRole("group", { name: group })).getByRole("spinbutton", { name }).textContent;

describe("Update session page", () => {
	beforeEach(() => {
		mocks.navigate.mockReset().mockResolvedValue(undefined);
		mocks.invalidateSession.mockReset().mockResolvedValue(undefined);
		mocks.updateSessionCall.mockReset().mockImplementation((value: Session) => Promise.resolve(value));
		mocks.gameNightId = null;
	});

	it("shows the game, location, players and times of the session once the lists load", async () => {
		await renderPage();

		await waitFor(() => expect(screen.getByRole("combobox", { name: "new.game.label" })).toHaveTextContent("Azul"));
		expect(screen.getByRole("combobox", { name: "new.location.label" })).toHaveTextContent("Café De Spelerij");
		expect(screen.queryByText("player-session:new.game.required")).not.toBeInTheDocument();
		expect(screen.getByText("Mikhael")).toBeInTheDocument();
		expect(screen.getByText("Reggi")).toBeInTheDocument();
		expect(screen.queryByText("Nova")).not.toBeInTheDocument();
		expect(segment("common:time", /hour/i)).toBe("20");
		expect(segment("common:time", /minute/i)).toBe("15");
		expect(segment("common:date", /day/i)).toBe("10");
		expect(segment("common:date", /month/i)).toBe("10");
		expect(segment("common:date", /year/i)).toBe("2026");
		expect(screen.getByLabelText("new.duration.label")).toHaveValue(75);
		expect(screen.getByDisplayValue("Close game")).toBeInTheDocument();
	});

	it("saves the session unchanged and returns to the game sessions", async () => {
		const user = userEvent.setup();
		await renderPage();
		await waitFor(() => expect(screen.getByRole("combobox", { name: "new.game.label" })).toHaveTextContent("Azul"));

		await user.click(screen.getByRole("button", { name: "save-update" }));

		await waitFor(() => expect(mocks.updateSessionCall).toHaveBeenCalledTimes(1));
		expect(mocks.updateSessionCall.mock.calls[0][0]).toEqual({
			id: 9,
			gameId: 4,
			locationId: 6,
			minutes: 75,
			comment: "Close game",
			start: new Date(2026, 9, 10, 20, 15),
			expansionIds: [],
			gameNightId: null,
			playerSessions: [
				{ playerId: 7, won: false, firstPlay: true, score: 31 },
				{ playerId: 8, won: true, firstPlay: false, score: 48 },
			],
		});
		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/4/sessions", replace: true }));
	});

	it("keeps the game night of the session and returns to the night", async () => {
		const user = userEvent.setup();
		mocks.gameNightId = 3;
		await renderPage();
		await waitFor(() => expect(screen.getByRole("combobox", { name: "new.game.label" })).toHaveTextContent("Azul"));

		await user.click(screen.getByRole("button", { name: "save-update" }));

		await waitFor(() => expect(mocks.updateSessionCall).toHaveBeenCalledTimes(1));
		expect(mocks.updateSessionCall.mock.calls[0][0]).toMatchObject({ id: 9, gameNightId: 3 });
		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/game-nights/3", replace: true }));
	});
});
