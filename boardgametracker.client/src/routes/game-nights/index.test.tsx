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
	loadFailed: false,
	isLoading: false,
	retryLoad: vi.fn(),
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
		isLoading: mocks.isLoading,
		loadFailed: mocks.loadFailed,
		retryLoad: mocks.retryLoad,
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
vi.mock("./-components/EmailInvitesButton", () => ({ EmailInvitesButton: () => null }));
vi.mock("./-hooks/useInviteLink", () => ({ useInviteLink: () => ({ link: "", copied: false, copy: vi.fn() }) }));
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
		summary: {
			gamesPlayed: 0,
			totalMinutes: 0,
			mostWins: 0,
			winnerIds: [],
			runningGames: 0,
			attendeeIds: [1],
			attendanceRecorded: false,
			playedGameIds: [],
		},
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
		mocks.loadFailed = false;
		mocks.isLoading = false;
		mocks.retryLoad.mockReset();
	});

	it("shows the shape of the list while it loads", () => {
		mocks.isLoading = true;

		renderPage();

		expect(screen.getByTestId("game-nights-skeleton")).toHaveAttribute("aria-busy", "true");
		expect(screen.getByText("loading")).toHaveClass("sr-only");
	});

	it("lists a night that is being played under playing now, and ended and old nights as past", () => {
		mocks.gameNights = [
			night(1, "Last month", addDays(new Date(), -30)),
			night(2, "Tonight", addHours(new Date(), -2)),
			night(3, "Next week", addDays(new Date(), 7)),
			night(4, "Ended early", addHours(new Date(), -3), addHours(new Date(), -1)),
		];

		renderPage();

		const live = screen.getByRole("region", { name: "live" });
		const upcoming = screen.getByRole("region", { name: "upcoming" });
		const past = screen.getByRole("region", { name: "past" });
		expect(within(live).getByText("Tonight")).toBeInTheDocument();
		expect(within(live).getByText("card.live-since")).toBeInTheDocument();
		expect(
			within(upcoming)
				.getAllByRole("heading", { level: 3 })
				.map((x) => x.textContent),
		).toEqual(["Next week"]);
		expect(within(upcoming).queryByText("card.live-since")).not.toBeInTheDocument();
		expect(within(past).getByText("Ended early")).toBeInTheDocument();
		expect(within(past).getByText("Last month")).toBeInTheDocument();
		expect(within(live).queryByRole("button")).not.toBeInTheDocument();
		expect(within(upcoming).getByRole("link", { name: "Next week" })).toHaveAttribute("href", "/game-nights/3");
	});

	it("offers a retry instead of the empty page when the nights fail to load", async () => {
		mocks.gameNights = [];
		mocks.loadFailed = true;

		renderPage();

		expect(screen.getByText("load-error.title")).toBeInTheDocument();
		expect(screen.queryByText("empty.title")).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "load-error.retry" }));
		expect(mocks.retryLoad).toHaveBeenCalled();
	});

	it("groups past nights by month and keeps older ones behind a button", async () => {
		mocks.gameNights = Array.from({ length: 12 }, (_, index) =>
			night(index + 1, `Night ${index + 1}`, addDays(new Date(), -7 * (index + 1))),
		);

		renderPage();

		const past = screen.getByRole("region", { name: "past" });
		expect(within(past).getAllByRole("link")).toHaveLength(10);
		expect(within(past).getAllByRole("heading", { level: 3 }).length).toBeGreaterThan(1);
		await userEvent.click(within(past).getByRole("button", { name: "past-night.show-older" }));
		expect(within(past).getAllByRole("link")).toHaveLength(12);
		await userEvent.click(within(past).getByRole("button", { name: "past-night.show-fewer" }));
		expect(within(past).getAllByRole("link")).toHaveLength(10);
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
