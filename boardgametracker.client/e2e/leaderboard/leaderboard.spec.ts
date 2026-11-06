import type { Page } from "@playwright/test";
import { addDays, startOfYear, subDays } from "date-fns";
import { expect, test } from "../support/fixtures";
import { pick } from "../support/gamesPage";
import { expectFilters, expectLeaderboard, gameTitles, pinnedYear, pinYearStart } from "../support/leaderboardPage";
import { coopGame, games, linkedPlayer, neverPlayedGame, sessions } from "../support/seedData";
import { between, brusselsDay, gameId, leaderboard, playerId } from "../support/standings";

const LEADERBOARD_ROUTE = "**/api/leaderboard?*";
const allSessions = leaderboard(sessions, 5, "wins");
const [leader] = allSessions.ranked;
const sessionsOf = (title: string) => sessions.filter((session) => session.game === title);
const mostSessionsIn = (title: string) =>
	Math.max(0, ...leaderboard(sessionsOf(title), 1, "wins").entries.map((entry) => entry.sessions));
const scarceTitle = games.find((game) => [3, 4].includes(mostSessionsIn(game.title)))?.title ?? "";

const choosePeriod = (page: Page, period: string) => page.locator("label", { hasText: period }).click();

const loneSession =
	sessions.filter((session) => session.scores.length === 3 && session.scores[0].score !== null).at(-1) ?? sessions[0];
const loneFrom = brusselsDay(subDays(loneSession.start, 1));
const loneTo = brusselsDay(addDays(loneSession.start, 1));
const recentDay = brusselsDay(addDays(sessions[4].start, 1));

test("shows everything on the default filters", async ({ page }) => {
	await page.goto("/leaderboard");

	await expectFilters(page, { period: "All time", game: "All games", min: "5 sessions", rank: "Wins" });
	await expectLeaderboard(page, allSessions);
	await expect(page).toHaveURL("/leaderboard");
});

test("lists the columns of the ranking", async ({ page }) => {
	await page.goto("/leaderboard");
	const header = page.getByRole("region", { name: "Ranking", exact: true }).locator("[aria-hidden=true]").first();

	await expect(header).toHaveText("#PlayerWinsSessionsWin ratePodiumsTime");
});

test("leaves the podium column out when nobody reached a podium", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${loneFrom}&to=${loneTo}&min=3`);
	const header = page.getByRole("region", { name: "Ranking", exact: true }).locator("[aria-hidden=true]").first();

	await expect(header).toHaveText("#PlayerWinsSessionsWin rateTime");
	await expectLeaderboard(page, leaderboard(between(loneFrom, loneTo), 3, "wins"));
});

test("puts the winner in the middle of the podium", async ({ page }) => {
	await page.goto("/leaderboard");
	const winner = page.getByRole("region", { name: "Podium" }).getByRole("link", { name: `Place 1, ${leader.name},` });
	const second = page.getByRole("region", { name: "Podium" }).getByRole("link", { name: /^Place 2, / });
	const third = page.getByRole("region", { name: "Podium" }).getByRole("link", { name: /^Place 3, / });

	const [left, middle, right] = await Promise.all(
		[second, winner, third].map(async (link) => (await link.boundingBox())?.x ?? 0),
	);

	expect(left).toBeLessThan(middle);
	expect(middle).toBeLessThan(right);
});

test("changes the period with the radio buttons and clears it again", async ({ page }) => {
	await pinYearStart(page);
	await page.goto("/leaderboard");

	await choosePeriod(page, "This year");
	await expect(page).toHaveURL("/leaderboard?range=year");
	await expectLeaderboard(page, leaderboard(pinnedYear, 5, "wins"));

	await choosePeriod(page, "Last 12 months");
	await expect(page).toHaveURL("/leaderboard?range=12m");
	await expect(page.getByRole("radio", { name: "Last 12 months" })).toBeChecked();

	await choosePeriod(page, "All time");
	await expect(page).toHaveURL("/leaderboard");
	await expectLeaderboard(page, allSessions);
});

test("changes the minimum and leaves the default out of the url", async ({ page }) => {
	await page.goto("/leaderboard");

	await pick(page, "Min. sessions", "10 sessions");
	await expect(page).toHaveURL("/leaderboard?min=10");
	await expectLeaderboard(page, leaderboard(sessions, 10, "wins"));

	await pick(page, "Min. sessions", "5 sessions");
	await expect(page).toHaveURL("/leaderboard");
});

test("offers the four minimums", async ({ page }) => {
	await page.goto("/leaderboard");

	await page.getByRole("combobox", { name: "Min. sessions" }).click();

	await expect(page.getByRole("option")).toHaveText(["1 session", "3 sessions", "5 sessions", "10 sessions"]);
});

test("changes the ranking metric and leaves the default out of the url", async ({ page }) => {
	await page.goto("/leaderboard");

	await pick(page, "Rank by", "Win rate");
	await expect(page).toHaveURL("/leaderboard?rank=winRate");
	await expectLeaderboard(page, leaderboard(sessions, 5, "winRate"));

	await pick(page, "Rank by", "Sessions");
	await expect(page).toHaveURL("/leaderboard?rank=sessions");
	await expectLeaderboard(page, leaderboard(sessions, 5, "sessions"));

	await pick(page, "Rank by", "Wins");
	await expect(page).toHaveURL("/leaderboard");
});

test("offers every game by title after all games", async ({ page }) => {
	await page.goto("/leaderboard");

	await page.getByRole("combobox", { name: "Game" }).click();

	await expect(page.getByRole("option")).toHaveText(gameTitles);
});

test("finds a game with the search box and filters on it", async ({ page }) => {
	await page.goto("/leaderboard");

	await page.getByRole("combobox", { name: "Game" }).click();
	await page.getByRole("textbox", { name: "Search..." }).fill(coopGame.title.slice(0, 5));
	await page.getByRole("option").filter({ hasText: coopGame.title }).click();

	await expect(page).toHaveURL(`/leaderboard?game=${gameId(coopGame.title)}`);
	await expect(page.getByRole("combobox", { name: "Game" })).toHaveText(coopGame.title);

	await pick(page, "Game", "All games");

	await expect(page).toHaveURL("/leaderboard");
	await expectLeaderboard(page, allSessions);
});

test("combines every filter and keeps them after a reload", async ({ page }) => {
	const [game] = games;
	const subset = sessions.filter(
		(session) => session.game === game.title && brusselsDay(session.start) >= brusselsDay(startOfYear(new Date())),
	);
	await page.goto(`/leaderboard?range=year&game=${gameId(game.title)}&min=1&rank=sessions`);

	await page.reload();

	await expectFilters(page, { period: "This year", game: game.title, min: "1 session", rank: "Sessions" });
	await expectLeaderboard(page, leaderboard(subset, 1, "sessions"), game.title);
});

test("ignores filter values it does not know", async ({ page }) => {
	await page.goto("/leaderboard?range=decade&min=7&rank=luck&game=abc&from=yesterday");

	await expectFilters(page, { period: "All time", game: "All games", min: "5 sessions", rank: "Wins" });
	await expectLeaderboard(page, allSessions);
});

test("shows no date fields until custom is chosen", async ({ page }) => {
	await page.goto("/leaderboard");
	await expect(page.getByRole("group", { name: "From" })).toHaveCount(0);

	await choosePeriod(page, "Custom");

	await expect(page).toHaveURL("/leaderboard?range=custom");
	await expect(page.getByRole("group", { name: "From" })).toBeVisible();
	await expect(page.getByRole("group", { name: "To" })).toBeVisible();
	await expectLeaderboard(page, allSessions);
});

test("types a custom period into the date fields", async ({ page }) => {
	const subset = between(loneFrom, recentDay);
	const [year, month, day] = loneFrom.split("-");
	await page.goto("/leaderboard?range=custom");
	await page.waitForLoadState("networkidle");

	await page.getByRole("spinbutton", { name: "day, From" }).pressSequentially(day);
	await page.getByRole("spinbutton", { name: "month, From" }).pressSequentially(month);
	await page.getByRole("spinbutton", { name: "year, From" }).pressSequentially(year);

	await expect(page).toHaveURL(`/leaderboard?range=custom&from=${loneFrom}`);

	const [toYear, toMonth, toDay] = recentDay.split("-");
	await page.getByRole("spinbutton", { name: "day, To" }).pressSequentially(toDay);
	await page.getByRole("spinbutton", { name: "month, To" }).pressSequentially(toMonth);
	await page.getByRole("spinbutton", { name: "year, To" }).pressSequentially(toYear);

	await expect(page).toHaveURL(`/leaderboard?range=custom&from=${loneFrom}&to=${recentDay}`);
	await expectLeaderboard(page, leaderboard(subset, 5, "wins"));
});

test("rounds a win rate only once", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${loneFrom}`);

	test.fail(true, "BUG: the leaderboard rounds twice, so 33 wins of 52 sessions (63.46%) show as 64% instead of 63%");
	await expectLeaderboard(page, leaderboard(between(loneFrom), 5, "wins"));
});

test("clears a custom date", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${loneFrom}`);

	await page.getByRole("group", { name: "From" }).getByRole("button", { name: "Clear date" }).click();

	await expect(page).toHaveURL("/leaderboard?range=custom");
	await expectLeaderboard(page, allSessions);
});

test("forgets the custom dates when another period is chosen", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${loneFrom}&to=${recentDay}`);

	await choosePeriod(page, "All time");

	await expect(page).toHaveURL("/leaderboard");
	await expect(page.getByRole("group", { name: "From" })).toHaveCount(0);
});

test("counts sessions up to the end date", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&to=${loneTo}`);

	await expectLeaderboard(page, leaderboard(between(undefined, loneTo), 5, "wins"));
});

test("swaps a period that ends before it starts", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${recentDay}&to=${loneFrom}`);

	await expectLeaderboard(page, leaderboard(between(loneFrom, recentDay), 5, "wins"));
});

test("explains that nobody reached the minimum and offers a lower one", async ({ page }) => {
	await page.goto(`/leaderboard?game=${gameId(scarceTitle)}&min=10`);

	await expectLeaderboard(page, leaderboard(sessionsOf(scarceTitle), 10, "wins"), scarceTitle);
	await page.getByRole("button", { name: "Lower the minimum to 3" }).click();

	await expect(page).toHaveURL(`/leaderboard?game=${gameId(scarceTitle)}&min=3`);
	await expectLeaderboard(page, leaderboard(sessionsOf(scarceTitle), 3, "wins"), scarceTitle);
});

test("drops the minimum from the url when lowering to the default", async ({ page }) => {
	await page.goto(`/leaderboard?game=${gameId(coopGame.title)}&min=10`);

	await page.getByRole("button", { name: "Lower the minimum to 5" }).click();

	await expect(page).toHaveURL(`/leaderboard?game=${gameId(coopGame.title)}`);
	await expectLeaderboard(page, leaderboard(sessionsOf(coopGame.title), 5, "wins"), coopGame.title);
});

test("offers the lowest minimum that reaches somebody", async ({ page }) => {
	await page.goto(`/leaderboard?range=custom&from=${loneFrom}&to=${loneTo}&min=10`);

	await expect(page.getByRole("button", { name: /^Lower the minimum to/ })).toHaveText("Lower the minimum to 1");
});

test("says when a game was never played and resets the filters", async ({ page }) => {
	await page.goto(`/leaderboard?game=${gameId(neverPlayedGame.title)}&rank=sessions&min=3`);

	await expect(page.getByRole("heading", { name: "No sessions match these filters" })).toBeVisible();
	await expect(page.getByText("Pick another period or game.")).toBeVisible();
	await expect(page.getByRole("region", { name: "Records" })).toHaveCount(0);

	await page.getByRole("button", { name: "Reset filters" }).click();

	await expect(page).toHaveURL("/leaderboard");
	await expectLeaderboard(page, allSessions);
});

test("says when nothing happened in a period", async ({ page }) => {
	await page.goto("/leaderboard?range=custom&from=2001-01-01&to=2001-12-31");

	await expect(page.getByRole("heading", { name: "No sessions match these filters" })).toBeVisible();
	await expect(page.getByRole("group", { name: "From" })).toBeVisible();
});

test("treats a game that does not exist as an empty filter", async ({ page }) => {
	await page.goto("/leaderboard?game=9999");

	await expect(page.getByRole("heading", { name: "No sessions match these filters" })).toBeVisible();
});

test("explains the page before anyone played", async ({ page }) => {
	await page.route(LEADERBOARD_ROUTE, (route) =>
		route.fulfill({
			json: {
				players: [],
				mostPlays: null,
				mostWins: null,
				bestWinRate: null,
				mostTimePlayed: null,
				minimumSessions: 5,
				minimumPlayersForPodium: 4,
				rankBy: "wins",
				showPodium: false,
				rankedCount: 0,
				winCount: 0,
				coopWinCount: 0,
			},
		}),
	);

	await page.goto("/leaderboard");

	await expect(page.getByText("No plays recorded yet")).toBeVisible();
	await expect(page.getByText("Log a few sessions and the leaderboard fills itself.")).toBeVisible();
	await expect(page.getByRole("radio")).toHaveCount(0);
});

test("explains when the leaderboard cannot be loaded", async ({ page }) => {
	await page.route(LEADERBOARD_ROUTE, (route) => route.abort());

	await page.goto("/leaderboard");

	test.fail(true, "BUG: /leaderboard keeps spinning forever when the leaderboard request fails");
	await expect(page.getByText("Couldn't load this page")).toBeVisible({ timeout: 3000 });
});

test("opens a player from the podium", async ({ page }) => {
	await page.goto("/leaderboard");

	await page
		.getByRole("region", { name: "Podium" })
		.getByRole("link", { name: `Place 1, ${leader.name},` })
		.click();

	await expect(page).toHaveURL(`/players/${playerId(leader.name)}`);
	await expect(page.getByRole("heading", { name: leader.name, level: 1 })).toBeVisible();
});

test("opens a player from the ranking", async ({ page }) => {
	const fourth = allSessions.ranked[3];
	await page.goto("/leaderboard");

	await page.getByTestId("leaderboard-row").first().click();

	await expect(page).toHaveURL(`/players/${playerId(fourth.name)}`);
	await expect(page.getByRole("heading", { name: fourth.name, level: 1 })).toBeVisible();
});

test("opens a player from a record", async ({ page }) => {
	const { mostTime } = allSessions;
	await page.goto("/leaderboard");

	await page.getByRole("link", { name: /^Most time: / }).click();

	await expect(page).toHaveURL(`/players/${playerId(mostTime?.name ?? "")}`);
});

test("is reachable from the menu", async ({ page }) => {
	await page.goto("/");

	await page.getByRole("link", { name: "Leaderboard", exact: true }).first().click();

	await expect(page).toHaveURL("/leaderboard");
	await expect(page.getByRole("heading", { name: "Leaderboard", level: 1 })).toBeVisible();
});

test("is reachable from the rank on a player page", async ({ page }) => {
	const rank = allSessions.ranked.findIndex((entry) => entry.name === linkedPlayer) + 1;
	await page.goto(`/players/${playerId(linkedPlayer)}`);

	await page.getByRole("link", { name: `#${rank} of ${allSessions.ranked.length} on the leaderboard` }).click();

	await expect(page).toHaveURL("/leaderboard");
});

test("shows a reader the same ranking", async ({ readerPage }) => {
	await readerPage.goto("/leaderboard");

	await expectLeaderboard(readerPage, allSessions);
});
