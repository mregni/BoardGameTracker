import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	routes: {} as Record<string, { component: FC }>,
	params: {
		"/games/$gameId_/update": { gameId: 3 },
		"/sessions/update_/$sessionId": { sessionId: 11 },
		"/sessions/new_/$gameId": { gameId: 3 },
	} as Record<string, Record<string, number>>,
	navigate: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: (path: string) => (config: { component: FC }) => {
		mocks.routes[path] = config;
		return { ...config, useParams: () => mocks.params[path] };
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn(), canGoBack: () => false } }),
	Navigate: () => null,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({}) }),
}));

vi.mock("@/services/queries/games", () => ({
	getGamePrice: (id: number) => ({ queryKey: ["price", id], queryFn: () => Promise.resolve(null) }),
	getGame: vi.fn(),
	getGames: vi.fn(),
}));

vi.mock("@/routes/games/-components/GameForm", () => ({
	GameForm: ({ title, backTo }: { title: string; backTo: string }) => (
		<div data-testid="game-form" data-title={title} data-back-to={backTo} />
	),
}));

vi.mock("@/routes/sessions/-components/SessionForm", () => ({
	SessionForm: ({
		title,
		backTo,
		isBackTarget,
	}: {
		title: string;
		backTo: string;
		isBackTarget?: (pathname: string) => boolean;
	}) => (
		<div
			data-testid="session-form"
			data-title={title}
			data-back-to={backTo}
			data-back-targets={["/games/3/sessions", "/players/1/sessions", "/locations/2", "/game-nights", "/games/3", "/"]
				.filter((pathname) => isBackTarget?.(pathname))
				.join(",")}
		/>
	),
}));

vi.mock("@/routes/games/-hooks/useUpdateGame", () => ({
	useUpdateGame: () => ({ game: { id: 3, title: "Carcassonne", image: null }, updateGame: vi.fn(), isLoading: false }),
}));

vi.mock("@/routes/games/-hooks/useNewGame", () => ({
	useNewGame: () => ({ saveGame: vi.fn(), isLoading: false }),
}));

vi.mock("@/routes/sessions/-hooks/useUpdateSessionData", () => ({
	useUpdateSessionData: () => ({
		game: { id: 3 },
		session: { id: 11, gameId: 3, minutes: 30, start: new Date(), playerSessions: [], expansions: [] },
		updateSession: vi.fn(),
		isPending: false,
		isLoading: false,
	}),
}));

vi.mock("@/routes/sessions/-hooks/useNewSessionData", () => ({
	useNewSessionData: () => ({ isPending: false, isLoading: false, saveSession: vi.fn(), games: [{ id: 3 }] }),
}));

vi.mock("@/routes/sessions/-hooks/useNewSessionWithGameData", () => ({
	useNewSessionWithGameData: () => ({
		game: { id: 3, maxPlayTime: 45 },
		isLoading: false,
		isPending: false,
		saveSession: vi.fn(),
	}),
}));

vi.mock("@/routes/players/-hooks/usePlayerModal", () => ({
	usePlayerModal: () => ({ savePlayer: vi.fn(), uploadImage: vi.fn(), isLoading: false }),
}));

await import("@/routes/games/$gameId_.update");
await import("@/routes/games/new");
await import("@/routes/sessions/update_.$sessionId");
await import("@/routes/sessions/new");
await import("@/routes/sessions/new_.$gameId");
await import("@/routes/players/new");

const renderRoute = (path: string) => {
	const Component = mocks.routes[path].component;
	renderWithProviders(<Component />);
};

describe("form page headers", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it.each([
		["/games/$gameId_/update", "game-form", "update.title", "/games/3"],
		["/games/new", "game-form", "new.manual.title", "/games/add"],
		["/sessions/update_/$sessionId", "session-form", "title-update", "/games/3/sessions"],
		["/sessions/new", "session-form", "title-new", "/"],
		["/sessions/new_/$gameId", "session-form", "title-new", "/games/3"],
	])("%s gives the form a title and a back target", async (path, testId, title, backTo) => {
		renderRoute(path);

		const form = await screen.findByTestId(testId);
		expect(form).toHaveAttribute("data-title", title);
		expect(form).toHaveAttribute("data-back-to", backTo);
	});

	it("edit session goes back to the session list it came from", async () => {
		renderRoute("/sessions/update_/$sessionId");

		expect(await screen.findByTestId("session-form")).toHaveAttribute(
			"data-back-targets",
			"/games/3/sessions,/players/1/sessions,/locations/2,/game-nights",
		);
	});

	it("new player shows a page title and a back link to the players", async () => {
		const user = userEvent.setup();
		renderRoute("/players/new");

		expect(screen.getByRole("heading", { name: "new.title" })).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: /common:back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/players", replace: true });
	});
});
