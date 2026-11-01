import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Session } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { params: { locationId: number }; context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	sessions: [] as Session[],
	locationError: null as unknown,
	navigate: vi.fn(),
	deleteLocationCall: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ locationId: 3 }) };
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn() } }),
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: { sessionId: number } }) => (
		<a href={params ? to.replace("$sessionId", String(params.sessionId)) : to}>{children}</a>
	),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/services/locationService", () => ({
	deleteLocationCall: (id: number) => mocks.deleteLocationCall(id),
	updateLocationCall: vi.fn(),
	addLocationCall: vi.fn(),
}));

vi.mock("@/services/queries/locations", () => ({
	getLocation: (id: number) => ({
		queryKey: ["locations", id],
		queryFn: () =>
			mocks.locationError
				? Promise.reject(mocks.locationError)
				: Promise.resolve({
						id,
						name: "Café De Spelerij Gent-Sint-Pieters",
						playCount: 12,
						lastPlayed: "2026-10-20T20:00:00",
					}),
	}),
	getLocationStatistics: (id: number) => ({
		queryKey: ["locations", id, "statistics"],
		queryFn: () => Promise.resolve({ distinctGameCount: 6, distinctPlayerCount: 5, totalPlayedTime: 460 }),
	}),
	getLocationSessions: (id: number) => ({
		queryKey: ["locations", id, "sessions"],
		queryFn: () => Promise.resolve(mocks.sessions),
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({
		queryKey: ["games"],
		queryFn: () => Promise.resolve([{ id: 1, title: "Carcassonne", image: null }]),
	}),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({
		queryKey: ["players"],
		queryFn: () => Promise.resolve([{ id: 7, name: "Mikhael", image: null, email: null, badges: [] }]),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "dd-MM-yyyy", uiLanguage: "en-US" }),
	}),
}));

const session = (id: number, day: number): Session => ({
	id,
	comment: "",
	gameId: 1,
	start: new Date(2026, 9, day, 20, 0),
	end: new Date(2026, 9, day, 20, 40),
	minutes: 40,
	playerSessions: [
		{ sessionId: id, playerId: 7, won: true, firstPlay: false, score: null },
		{ sessionId: id, playerId: 8, won: false, firstPlay: false, score: null },
	],
	expansions: [],
	locationId: 3,
	gameNightId: null,
	isCoop: false,
	teamWon: null,
	teamScore: null,
});

mocks.sessions = Array.from({ length: 12 }, (_, index) => session(index + 1, 20 - index));

await import("./$locationId");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Location detail page", () => {
	beforeEach(() => {
		mocks.navigate.mockReset().mockResolvedValue(undefined);
		mocks.deleteLocationCall.mockReset().mockResolvedValue(undefined);
		mocks.locationError = null;
	});

	it("prefetches the location, its statistics, sessions, games and players in the loader", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ params: { locationId: 3 }, context: { queryClient } });

		const keys = queryClient.query.mock.calls.map(([options]) => options.queryKey);
		expect(keys).toEqual(
			expect.arrayContaining([
				["locations", 3],
				["locations", 3, "statistics"],
				["locations", 3, "sessions"],
				["games"],
				["players"],
			]),
		);
	});

	it("goes back to the locations overview", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: /detail.back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations", replace: true });
	});

	it("opens the edit dialog with the location name", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "edit-named" }));

		expect(await screen.findByDisplayValue("Café De Spelerij Gent-Sint-Pieters")).toBeInTheDocument();
	});

	it("warns about the linked sessions and deletes the location after confirming", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "delete.named" }));
		expect(await screen.findByText(/delete.description-with-sessions/)).toBeInTheDocument();

		const confirm = screen.getAllByRole("button", { name: "delete.button" }).at(-1);
		await user.click(confirm as HTMLElement);

		await waitFor(() => expect(mocks.deleteLocationCall).toHaveBeenCalledWith(3));
		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations" }));
	});
	it("shows the location name on one line with a tooltip", async () => {
		renderPage();

		const heading = await screen.findByRole("heading", { level: 1, name: "Café De Spelerij Gent-Sint-Pieters" });
		expect(heading).toHaveClass("truncate");
		expect(heading).toHaveAttribute("title", "Café De Spelerij Gent-Sint-Pieters");
	});

	it("shows the statistics cards", async () => {
		renderPage();

		expect(await screen.findByText("detail.games")).toBeInTheDocument();
		expect(screen.getAllByText("detail.sessions").length).toBeGreaterThan(0);
		expect(screen.getByText("detail.players")).toBeInTheDocument();
		expect(screen.getByText("detail.play-time")).toBeInTheDocument();
		expect(screen.getByText("12")).toBeInTheDocument();
		expect(screen.getByText("6")).toBeInTheDocument();
		expect(screen.getByText("5")).toBeInTheDocument();
	});

	it("shows the five newest sessions and expands to all of them", async () => {
		const user = userEvent.setup();
		renderPage();
		const sessionLinks = () =>
			screen.getAllByRole("link").filter((link) => link.getAttribute("href")?.startsWith("/sessions/update/"));

		await screen.findByText("detail.recent-sessions");
		await waitFor(() => expect(sessionLinks()).toHaveLength(5));
		expect(sessionLinks()[0]).toHaveAttribute("href", "/sessions/update/1");
		expect(sessionLinks()[0]).toHaveTextContent("20-10-2026");

		await user.click(screen.getByRole("button", { name: "detail.show-all" }));

		expect(sessionLinks()).toHaveLength(12);
		expect(sessionLinks()[11]).toHaveTextContent("09-10-2026");
	});

	it("leads with the most played game and the top winner at this location", async () => {
		renderPage();

		expect(await screen.findByText("detail.most-played")).toBeInTheDocument();
		expect(screen.getByText("detail.top-winners")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /Carcassonne\s*detail.session-count/ })).toHaveAttribute(
			"href",
			"/games/1",
		);
		expect(screen.getByRole("link", { name: /Mikhael\s*detail.win-count/ })).toHaveAttribute("href", "/players/7");
	});

	it("shows a not-found state with a way back instead of spinning forever", async () => {
		mocks.locationError = { kind: "client", status: 404, message: "Not found" };
		const user = userEvent.setup();
		renderPage();

		expect(await screen.findByText("detail.not-found.title")).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "detail.back" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations", replace: true });
	});

	it("shows the edit and delete buttons for writers", async () => {
		renderPage();

		expect(await screen.findByRole("button", { name: "edit-named" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "delete.named" })).toBeInTheDocument();
	});
});
