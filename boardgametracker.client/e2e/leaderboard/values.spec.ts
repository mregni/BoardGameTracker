import { startOfYear, subMonths } from "date-fns";
import { test } from "../support/fixtures";
import { expectFilters, expectLeaderboard } from "../support/leaderboardPage";
import { games, sessions } from "../support/seedData";
import { between, brusselsDay, gameId, leaderboard, type RankBy } from "../support/standings";

const rankings: { rank: RankBy; label: string; query: string }[] = [
	{ rank: "wins", label: "Wins", query: "" },
	{ rank: "winRate", label: "Win rate", query: "rank=winRate" },
	{ rank: "sessions", label: "Sessions", query: "rank=sessions" },
];

const minimums = [1, 3, 5, 10];

const url = (...parts: string[]) => {
	const query = parts.filter(Boolean).join("&");
	return query ? `/leaderboard?${query}` : "/leaderboard";
};

const minimumQuery = (minimum: number) => (minimum === 5 ? "" : `min=${minimum}`);

const today = new Date();
const periods = [
	{ query: "range=year", label: "This year", subset: between(brusselsDay(startOfYear(today))) },
	{ query: "range=12m", label: "Last 12 months", subset: between(brusselsDay(subMonths(today, 12))) },
];

for (const { rank, label, query } of rankings) {
	for (const minimum of minimums) {
		test(`ranks all sessions by ${label} with at least ${minimum} sessions`, async ({ page }) => {
			await page.goto(url(query, minimumQuery(minimum)));

			await expectFilters(page, {
				period: "All time",
				game: "All games",
				min: `${minimum} session${minimum === 1 ? "" : "s"}`,
				rank: label,
			});
			await expectLeaderboard(page, leaderboard(sessions, minimum, rank));
		});
	}

	for (const period of periods) {
		test(`ranks ${period.label} by ${label}`, async ({ page }) => {
			await page.goto(url(period.query, query));

			await expectFilters(page, { period: period.label, game: "All games", min: "5 sessions", rank: label });
			await expectLeaderboard(page, leaderboard(period.subset, 5, rank));
		});
	}
}

const playedGames = games.filter((game) => sessions.some((session) => session.game === game.title));

for (const game of playedGames) {
	const subset = sessions.filter((session) => session.game === game.title);

	test(`ranks the sessions of ${game.title}`, async ({ page }) => {
		await page.goto(url(`game=${gameId(game.title)}`, "min=1"));

		await expectFilters(page, { period: "All time", game: game.title, min: "1 session", rank: "Wins" });
		await expectLeaderboard(page, leaderboard(subset, 1, "wins"), game.title);
	});
}
