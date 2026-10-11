import type { FC } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Session } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	sessions: [] as Session[],
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ locationId: 3 }) };
	},
	useNavigate: () => vi.fn(),
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

vi.mock("@/services/queries/locations", () => ({
	getLocation: (id: number) => ({
		queryKey: ["locations", id],
		queryFn: () => Promise.resolve({ id, name: "Café De Spelerij Gent-Sint-Pieters", playCount: 12 }),
	}),
	getLocationStatistics: (id: number) => ({
		queryKey: ["locations", id, "statistics"],
		queryFn: () =>
			Promise.resolve({ sessionCount: 12, distinctGameCount: 6, distinctPlayerCount: 5, totalPlayedTime: 460 }),
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
});

mocks.sessions = Array.from({ length: 12 }, (_, index) => session(index + 1, 20 - index));

await import("./$locationId");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Location detail page", () => {
	it("shows the location name on one line with a tooltip", async () => {
		renderPage();

		const heading = await screen.findByRole("heading", { name: "Café De Spelerij Gent-Sint-Pieters" });
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

	it("lists the newest ten sessions with game, winner and a link to the session", async () => {
		renderPage();

		expect(await screen.findAllByText("Carcassonne")).toHaveLength(10);
		expect(screen.getAllByText("Mikhael")).toHaveLength(10);
		expect(screen.getByText("20-10-2026")).toBeInTheDocument();
		expect(screen.queryByText("09-10-2026")).not.toBeInTheDocument();
		expect(screen.getAllByRole("link")[0]).toHaveAttribute("href", "/sessions/update/1");
	});

	it("shows every session after clicking show all", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "detail.show-all" }));

		expect(screen.getAllByText("Carcassonne")).toHaveLength(12);
		expect(screen.getByRole("button", { name: "detail.show-less" })).toBeInTheDocument();
	});

	it("shows the edit and delete buttons for writers", async () => {
		renderPage();

		expect(await screen.findByRole("button", { name: "edit" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "delete.button" })).toBeInTheDocument();
	});
});
