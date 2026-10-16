import { describe, expect, it, vi } from "vitest";
import { render, screen, userEvent, within } from "@/test/test-utils";
import { NightAttendanceCard } from "./NightAttendanceCard";
import { NightGamesCard } from "./NightGamesCard";
import { NightRecordsCard } from "./NightRecordsCard";
import { NightStatsTiles } from "./NightStatsTiles";
import { NightStreaksCard } from "./NightStreaksCard";
import { NightWinnerHistoryCard } from "./NightWinnerHistoryCard";
import { NightWinnersCard } from "./NightWinnersCard";
import { buildStatistics, lookup } from "./statsFixtures";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, params }: { children: React.ReactNode; params: { gameNightId: number } }) => (
		<a href={`/game-nights/${params.gameNightId}`}>{children}</a>
	),
}));

const statistics = buildStatistics();

describe("NightStatsTiles", () => {
	it("shows the four tiles with a per night hint", () => {
		render(<NightStatsTiles statistics={statistics} uiLanguage="en-us" />);

		expect(screen.getByText("stats.tiles.nights")).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
		expect(screen.getByText("stats.tiles.every-week")).toBeInTheDocument();
		expect(screen.getByText("21")).toBeInTheDocument();
		expect(screen.getByText("22 hours 30 minutes")).toBeInTheDocument();
		expect(screen.getByText("4.3")).toBeInTheDocument();
		expect(screen.getByText("stats.tiles.table-hint")).toBeInTheDocument();
		expect(screen.getAllByText("stats.tiles.per-night")).toHaveLength(2);
	});

	it("counts in days when nights are less than a week apart, and has no rhythm for a single night", () => {
		const { rerender } = render(
			<NightStatsTiles statistics={{ ...statistics, averageDaysBetweenNights: 3.4 }} uiLanguage="en-us" />,
		);
		expect(screen.getByText("stats.tiles.every-day")).toBeInTheDocument();

		rerender(
			<NightStatsTiles
				statistics={{ ...statistics, nights: 0, totalMinutes: 0, averageDaysBetweenNights: null }}
				uiLanguage="en-us"
			/>,
		);
		expect(screen.queryByText(/stats.tiles.every/)).not.toBeInTheDocument();
		expect(screen.getAllByText("0").length).toBeGreaterThan(0);
	});
});

describe("NightWinnersCard", () => {
	it("ranks the winners with the share of their nights as a bar", () => {
		render(<NightWinnersCard winners={statistics.winners} tiedNights={1} lookup={lookup} />);

		expect(screen.getByText("Kathleen")).toBeInTheDocument();
		expect(screen.getByText("Mikhael")).toBeInTheDocument();
		const [bar] = screen.getAllByRole("progressbar", { name: "stats.winners.bar" });
		expect(bar).toHaveAttribute("value", "3");
		expect(bar).toHaveAttribute("max", "6");
		expect(screen.getByText("1")).toHaveClass("text-gold");
		expect(screen.getByText(/stats.winners.ties/)).toBeInTheDocument();
	});

	it("says nobody won yet and skips the tie note when there is none", () => {
		const { rerender } = render(<NightWinnersCard winners={statistics.winners} tiedNights={0} lookup={lookup} />);
		expect(screen.queryByText(/stats.winners.ties/)).not.toBeInTheDocument();

		rerender(<NightWinnersCard winners={[]} tiedNights={0} lookup={lookup} />);
		expect(screen.getByText("stats.winners.empty")).toBeInTheDocument();
	});
});

describe("NightWinnerHistoryCard", () => {
	it("lists the newest nights with their winners, a streak pill and links", () => {
		render(<NightWinnerHistoryCard history={statistics.history} lookup={lookup} />);

		const items = screen.getAllByRole("listitem");
		expect(items).toHaveLength(5);
		expect(within(items[0]).getByText("Strategy night")).toBeInTheDocument();
		expect(within(items[0]).getByText("stats.history.streak")).toBeInTheDocument();
		expect(within(items[0]).getByRole("link")).toHaveAttribute("href", "/game-nights/31");
		expect(within(items[1]).getByText("Kathleen")).toBeInTheDocument();
		expect(within(items[1]).getByText("reggi")).toBeInTheDocument();
		expect(within(items[1]).queryByText("stats.history.streak")).not.toBeInTheDocument();
		expect(within(items[2]).getByText("stats.history.no-winner")).toBeInTheDocument();
		expect(within(items[3]).getByText("stats.history.no-sessions")).toBeInTheDocument();
	});

	it("shares a night won through co-op with the whole team and marks a co-op only night", () => {
		const [first, second] = statistics.history;
		render(
			<NightWinnerHistoryCard
				history={[
					{ ...first, winnerIds: [1, 2, 3], coopOnly: true, sharedThroughCoop: true },
					{ ...second, coopOnly: false, sharedThroughCoop: false },
				]}
				lookup={lookup}
			/>,
		);

		const items = screen.getAllByRole("listitem");
		expect(within(items[0]).getByText("stats.history.coop-shared")).toBeInTheDocument();
		expect(within(items[0]).queryByText("Kathleen")).not.toBeInTheDocument();
		expect(within(items[0]).queryByText("stats.history.streak")).not.toBeInTheDocument();
		expect(within(items[0]).getByText("stats.history.games · stats.history.coop-only · Thuis")).toBeInTheDocument();
		expect(within(items[1]).queryByText("stats.history.coop-shared")).not.toBeInTheDocument();
		expect(within(items[1]).getByText("stats.history.games · Thuis")).toBeInTheDocument();
	});

	it("shows every night on demand and folds them back", async () => {
		render(<NightWinnerHistoryCard history={statistics.history} lookup={lookup} />);

		await userEvent.click(screen.getByRole("button", { name: "stats.history.all" }));
		expect(screen.getAllByRole("listitem")).toHaveLength(6);

		await userEvent.click(screen.getByRole("button", { name: "stats.history.less" }));
		expect(screen.getAllByRole("listitem")).toHaveLength(5);
	});

	it("has no toggle for a short history and an empty message without nights", () => {
		const { rerender } = render(<NightWinnerHistoryCard history={statistics.history.slice(0, 2)} lookup={lookup} />);
		expect(screen.queryByRole("button")).not.toBeInTheDocument();

		rerender(<NightWinnerHistoryCard history={[]} lookup={lookup} />);
		expect(screen.getByText("stats.winners.empty")).toBeInTheDocument();
	});
});

describe("NightGamesCard", () => {
	it("ranks the games played at nights and nudges with planned but never played", () => {
		render(
			<NightGamesCard games={statistics.games} plannedNeverPlayed={statistics.plannedNeverPlayed} lookup={lookup} />,
		);

		expect(screen.getByText("Azul")).toBeInTheDocument();
		expect(screen.getByText("stats.games.nights · stats.games.planned")).toBeInTheDocument();
		expect(screen.getByText("stats.games.nights")).toBeInTheDocument();
		const bars = screen.getAllByRole("progressbar");
		expect(bars[0]).toHaveAttribute("value", "6");
		expect(bars[1]).toHaveAttribute("max", "6");
		expect(screen.getByText("stats.games.never-played")).toBeInTheDocument();
		expect(screen.getByText("Everdell")).toBeInTheDocument();
	});

	it("says nothing was played yet", () => {
		render(<NightGamesCard games={[]} plannedNeverPlayed={[]} lookup={lookup} />);

		expect(screen.getByText("stats.games.empty")).toBeInTheDocument();
		expect(screen.queryByText("stats.games.never-played")).not.toBeInTheDocument();
	});
});

describe("NightStreaksCard", () => {
	it("tells the current winner streak with the record, the attendance and the game streak", () => {
		render(<NightStreaksCard streaks={statistics.streaks} lookup={lookup} />);

		expect(screen.getByText("stats.streaks.winner-current")).toBeInTheDocument();
		expect(screen.getByText("stats.streaks.winner-current-hint · stats.streaks.winner-record")).toBeInTheDocument();
		expect(screen.getByText("stats.streaks.attendance")).toBeInTheDocument();
		expect(screen.getByText("stats.streaks.game-longest")).toBeInTheDocument();
	});

	it("leaves the record out when the current streak is the record and marks a game streak still going", () => {
		const current = statistics.streaks.currentWinner;
		render(
			<NightStreaksCard
				streaks={{
					currentWinner: current,
					longestWinner: current,
					currentAttendance: null,
					longestGame: { gameId: 10, length: 2, from: new Date(), to: new Date(), isCurrent: true },
				}}
				lookup={lookup}
			/>,
		);

		expect(screen.getByText("stats.streaks.winner-current-hint")).toBeInTheDocument();
		expect(screen.queryByText("stats.streaks.attendance")).not.toBeInTheDocument();
		expect(screen.getByText("stats.streaks.game-current")).toBeInTheDocument();
	});

	it("falls back on the longest winner streak, and says when there are no streaks", () => {
		const { rerender } = render(
			<NightStreaksCard
				streaks={{ ...statistics.streaks, currentWinner: null, currentAttendance: null, longestGame: null }}
				lookup={lookup}
			/>,
		);
		expect(screen.getByText("stats.streaks.winner-longest")).toBeInTheDocument();
		expect(screen.getByText("stats.streaks.winner-longest-hint")).toBeInTheDocument();

		rerender(
			<NightStreaksCard
				streaks={{ currentWinner: null, longestWinner: null, currentAttendance: null, longestGame: null }}
				lookup={lookup}
			/>,
		);
		expect(screen.getByText("stats.streaks.empty")).toBeInTheDocument();
	});

	it("shows other streaks when nobody won two nights in a row", () => {
		render(
			<NightStreaksCard
				streaks={{ ...statistics.streaks, currentWinner: null, longestWinner: null }}
				lookup={lookup}
			/>,
		);

		expect(screen.queryByText(/stats.streaks.winner/)).not.toBeInTheDocument();
		expect(screen.getByText("stats.streaks.attendance")).toBeInTheDocument();
	});
});

describe("NightAttendanceCard", () => {
	const renderCard = () =>
		render(
			<NightAttendanceCard
				attendance={statistics.attendance}
				recentNights={statistics.recentNights}
				nights={statistics.nights}
				lookup={lookup}
			/>,
		);

	it("lists the nights each player attended, never RSVP answers, with the dots of the first player", () => {
		renderCard();

		const rows = screen.getAllByRole("button", { name: "stats.attendance.select" });
		expect(rows).toHaveLength(2);
		expect(rows[0]).toHaveAttribute("aria-pressed", "true");
		expect(within(rows[0]).getByText("stats.attendance.hosted")).toBeInTheDocument();
		expect(within(rows[1]).queryByText("stats.attendance.hosted")).not.toBeInTheDocument();
		expect(within(rows[0]).getByRole("progressbar")).toHaveAttribute("max", "7");
		expect(screen.getAllByTestId("progress-dot").map((x) => x.dataset.state)).toEqual(["on", "on"]);
		expect(screen.queryByText(/declined|coming/i)).not.toBeInTheDocument();
	});

	it("shows the dots of the player that was picked", async () => {
		renderCard();

		await userEvent.click(screen.getAllByRole("button", { name: "stats.attendance.select" })[1]);

		expect(screen.getAllByRole("button", { name: "stats.attendance.select" })[1]).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		expect(screen.getAllByTestId("progress-dot").map((x) => x.dataset.state)).toEqual(["off", "on"]);
	});

	it("says nobody was at the table yet", () => {
		render(<NightAttendanceCard attendance={[]} recentNights={[]} nights={2} lookup={lookup} />);

		expect(screen.getByText("stats.attendance.empty")).toBeInTheDocument();
	});
});

describe("NightRecordsCard", () => {
	it("shows the four records with the night they belong to", () => {
		render(<NightRecordsCard records={statistics.records} lookup={lookup} />);

		expect(screen.getByText("5 hours 40 minutes")).toBeInTheDocument();
		expect(screen.getByText("stats.records.night · stats.records.games")).toBeInTheDocument();
		expect(screen.getByText("stats.records.night · stats.records.mostly")).toBeInTheDocument();
		expect(screen.getByText("6")).toBeInTheDocument();
		expect(screen.getByText("02:10")).toBeInTheDocument();
		expect(screen.getByText("stats.records.finish")).toBeInTheDocument();
	});

	it("leaves out a top game that is unknown and says when there are no records", () => {
		const { rerender } = render(
			<NightRecordsCard
				records={{
					longestNight: null,
					biggestTable: null,
					latestFinish: null,
					mostGames: { ...statistics.records.mostGames!, gameId: null },
				}}
				lookup={lookup}
			/>,
		);
		expect(screen.getByText("stats.records.night")).toBeInTheDocument();
		expect(screen.queryByText("stats.records.latest-finish")).not.toBeInTheDocument();

		rerender(
			<NightRecordsCard
				records={{ longestNight: null, mostGames: null, biggestTable: null, latestFinish: null }}
				lookup={lookup}
			/>,
		);
		expect(screen.getByText("stats.records.empty")).toBeInTheDocument();
	});
});
