import { addDays, addHours } from "date-fns";
import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	validateSearch: { parse: (input: unknown) => unknown };
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	gameNights: [] as GameNight[],
	navigate: vi.fn(),
	search: {} as { view?: string; period?: string },
	statsView: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
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
vi.mock("./-components/stats/GameNightStatsView", () => ({
	GameNightStatsView: (props: { period: string; onPeriodChange: (period: string) => void }) => {
		mocks.statsView(props.period);
		return (
			<button type="button" onClick={() => props.onPeriodChange("thisYear")}>
				stats-view
			</button>
		);
	},
}));

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
		mocks.statsView.mockReset();
		mocks.search = {};
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

	it("accepts only the known views and periods in the address", () => {
		const { parse } = mocks.captured.config.validateSearch;

		expect(parse({ view: "stats", period: "last12Months" })).toEqual({ view: "stats", period: "last12Months" });
		expect(parse({})).toEqual({});
		expect(() => parse({ view: "chart" })).toThrow();
		expect(() => parse({ period: "forever" })).toThrow();
	});

	it("shows the nights by default and switches to the statistics in the address", async () => {
		mocks.gameNights = [night(1, "Last month", addDays(new Date(), -30))];

		renderPage();

		const [nightsButton] = screen.getAllByRole("button", { name: "view.nights" });
		expect(nightsButton).toHaveAttribute("aria-pressed", "true");
		expect(screen.queryByText("stats-view")).not.toBeInTheDocument();

		await userEvent.click(screen.getAllByRole("button", { name: "view.stats" })[0]);

		expect(mocks.navigate).toHaveBeenCalledWith(expect.objectContaining({ to: "/game-nights", replace: true }));
		const { search } = mocks.navigate.mock.calls[0][0] as { search: (previous: object) => object };
		expect(search({ period: "thisYear" })).toEqual({ period: "thisYear", view: "stats" });
	});

	it("shows the statistics for the period in the address and keeps the view when the period changes", async () => {
		mocks.gameNights = [night(1, "Last month", addDays(new Date(), -30))];
		mocks.search = { view: "stats", period: "last12Months" };

		renderPage();

		expect(screen.queryByRole("region", { name: "past" })).not.toBeInTheDocument();
		expect(mocks.statsView).toHaveBeenCalledWith("last12Months");
		expect(screen.getAllByRole("button", { name: "view.stats" })[0]).toHaveAttribute("aria-pressed", "true");

		await userEvent.click(screen.getByText("stats-view"));

		const { search } = mocks.navigate.mock.calls[0][0] as { search: (previous: object) => object };
		expect(search({ view: "stats" })).toEqual({ view: "stats", period: "thisYear" });
	});

	it("starts the statistics on all time", () => {
		mocks.gameNights = [night(1, "Last month", addDays(new Date(), -30))];
		mocks.search = { view: "stats" };

		renderPage();

		expect(mocks.statsView).toHaveBeenCalledWith("allTime");
	});
});
