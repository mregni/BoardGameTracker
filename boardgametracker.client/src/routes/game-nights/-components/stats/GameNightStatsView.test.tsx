import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameNightStatisticsPeriod, type Settings } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";
import { GameNightStatsView } from "./GameNightStatsView";
import { buildStatistics, games, locations, players } from "./statsFixtures";

const mocks = vi.hoisted(() => ({
	getStatistics: vi.fn(),
}));

vi.mock("@/services/gameNightService", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/services/gameNightService")>()),
	getGameNightStatisticsCall: mocks.getStatistics,
}));

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, params }: { children: React.ReactNode; params: { gameNightId: number } }) => (
		<a href={`/game-nights/${params.gameNightId}`}>{children}</a>
	),
}));

const settings = { uiLanguage: "en-us", dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm" } as Settings;

const renderView = (onPeriodChange = vi.fn(), period = GameNightStatisticsPeriod.AllTime) =>
	renderWithProviders(
		<GameNightStatsView
			period={period}
			onPeriodChange={onPeriodChange}
			settings={settings}
			players={players}
			games={games}
			locations={locations}
		/>,
	);

describe("GameNightStatsView", () => {
	beforeEach(() => {
		mocks.getStatistics.mockReset();
	});

	it("loads the statistics of the chosen period and shows every card", async () => {
		mocks.getStatistics.mockResolvedValue(buildStatistics());

		renderView(vi.fn(), GameNightStatisticsPeriod.ThisYear);

		expect(await screen.findByText("stats.tiles.nights")).toBeInTheDocument();
		expect(mocks.getStatistics).toHaveBeenCalledWith(GameNightStatisticsPeriod.ThisYear);
		expect(screen.getByText("stats.logged")).toBeInTheDocument();
		for (const name of [
			"stats.winners.title",
			"stats.history.title",
			"stats.games.title",
			"stats.streaks.title",
			"stats.attendance.title",
			"stats.records.title",
		]) {
			expect(screen.getByRole("region", { name: new RegExp(name) })).toBeInTheDocument();
		}
	});

	it("marks the period and reports a new one", async () => {
		mocks.getStatistics.mockResolvedValue(buildStatistics());
		const onPeriodChange = vi.fn();

		renderView(onPeriodChange);

		const periods = screen.getByRole("group", { name: "common:period.label" });
		expect(within(periods).getByRole("button", { name: "common:period.all-time" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		await userEvent.click(within(periods).getByRole("button", { name: "common:period.last-12-months" }));

		expect(onPeriodChange).toHaveBeenCalledWith(GameNightStatisticsPeriod.Last12Months);
	});

	it("explains which nights count when none was held in the period", async () => {
		mocks.getStatistics.mockResolvedValue(
			buildStatistics({ totalNights: 0, nightsWithSessions: 0, nights: 0, winners: [], history: [] }),
		);

		renderView();

		expect(await screen.findByText("stats.empty.title")).toBeInTheDocument();
		expect(screen.getByText("stats.empty.description")).toBeInTheDocument();
		expect(screen.queryByText("stats.logged")).not.toBeInTheDocument();
		expect(screen.queryByText("stats.tiles.nights")).not.toBeInTheDocument();
	});

	it("still shows the logged hint when held nights have nothing to count", async () => {
		mocks.getStatistics.mockResolvedValue(buildStatistics({ totalNights: 3, nightsWithSessions: 0, nights: 0 }));

		renderView();

		expect(await screen.findByText("stats.empty.title")).toBeInTheDocument();
		expect(screen.getByText("stats.logged")).toBeInTheDocument();
	});
});
