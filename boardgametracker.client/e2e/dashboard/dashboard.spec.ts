import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { duration } from "../support/format";
import { expected, gameNights, linkedPlayer, games as seededGames, sessions } from "../support/seedData";

test.beforeEach(async ({ page }) => {
	await page.goto("/");
	await expect(page.getByRole("heading", { name: `Welcome back, ${linkedPlayer}`, level: 1 })).toBeVisible();
});

test("greets the linked player with their month", async ({ page }) => {
	const main = page.getByRole("main");
	const month = `This month you played ${expected.linkedPlayedThisMonth} of the group's ${expected.sessionsThisMonth} sessions and won ${expected.linkedWonThisMonth}.`;

	await expect(main.getByText(month)).toBeVisible();
	await expect(main.getByRole("link", { name: "Add game" })).toHaveAttribute("href", "/games/add");
	await expect(main.getByRole("link", { name: "Log session" })).toHaveAttribute("href", "/sessions/new");
});

test("shows the next game night with its answers and suggestions", async ({ page }) => {
	const card = page.getByRole("region", { name: "Next game night" });
	const [night] = gameNights;

	await expect(card.getByRole("heading", { name: night.title })).toBeVisible();
	await expect(card).toContainText("in 3 days");
	await expect(card).toContainText(`${night.location} · host ${linkedPlayer}`);
	await expect(card).toContainText("2 coming");
	await expect(card).toContainText("1 maybe");
	await expect(card).toContainText("1 no answer");
	await expect(card).toContainText("1 not coming");
	for (const game of night.suggestedGames) {
		await expect(card.getByRole("link", { name: game })).toBeVisible();
	}
	await expect(card.getByRole("link", { name: "Open night" })).toHaveAttribute("href", "/game-nights/1");
	await expect(card.getByRole("link", { name: "1 more planned" })).toHaveAttribute("href", "/game-nights");
});

test("adds up the collection and sessions in the tiles", async ({ page }) => {
	const main = page.getByRole("main");
	const games = `Games ${expected.gameCount} ${expected.expansionCount} expansions`;
	const players = `Players ${expected.playerCount} ${expected.playersThisMonth} played this month`;

	await expect(main.getByRole("link", { name: games })).toHaveAttribute("href", "/games");
	await expect(main.getByRole("link", { name: players })).toHaveAttribute("href", "/players");
	await expect(main.getByText(String(expected.sessionCount), { exact: true })).toBeVisible();
	await expect(main.getByText(`${expected.sessionsThisMonth} this month`, { exact: true })).toBeVisible();
	const totalMinutes = sessions.reduce((total, session) => total + session.minutes, 0);
	await expect(main.getByText(duration(totalMinutes), { exact: true })).toBeVisible();
	await expect(main.getByText(`avg ${duration(Math.round(totalMinutes / sessions.length))} a session`)).toBeVisible();
});

test("lists the last sessions newest first with their winner", async ({ page }) => {
	const rows = page.getByRole("region", { name: "Last sessions" }).getByRole("listitem");

	await expect(rows).toHaveCount(expected.lastSessions.length);
	for (const [index, session] of expected.lastSessions.entries()) {
		const row = rows.nth(index);
		await expect(row).toContainText(session.game);
		await expect(row).toContainText(`${session.scores.length} players`);
		await expect(row).toContainText(duration(session.minutes));
		if (session.isCoop) {
			await expect(row).toContainText(session.teamWon ? "Co-op · won" : "Co-op · lost");
		} else {
			await expect(row).toContainText(session.winner ?? "");
			await expect(row.getByRole("img", { name: "Won" })).toBeVisible();
		}
	}
	await expect(rows.first().getByRole("link")).toHaveAttribute("href", "/games/1/sessions");
});

test("ranks the top players by wins", async ({ page }) => {
	const card = page.getByRole("region", { name: "Top players" });
	const rows = card.getByRole("listitem");

	await expect(rows).toHaveCount(expected.topPlayers.length);
	for (const [index, player] of expected.topPlayers.entries()) {
		const row = rows.nth(index);
		await expect(row).toContainText(player.name);
		await expect(row).toContainText(`${player.sessions} sessions`);
		await expect(row).toContainText(`${player.wins} wins`);
		await expect(row).toContainText(`${Math.round(player.winRate * 100)}% won`);
	}
	await expect(card.getByRole("link", { name: "Leaderboard" })).toHaveAttribute("href", "/leaderboard");
});

test("shows the most played games per period", async ({ page }) => {
	const card = page.getByRole("region", { name: "Most played" });
	const rows = card.getByRole("listitem");

	const expectRows = async (games: [string, number][]) => {
		await expect(rows).toHaveCount(games.length);
		for (const [index, [, count]] of games.entries()) {
			await expect(rows.nth(index)).toContainText(`${count}×`);
		}
		await expect(rows.first()).toContainText(games[0][0]);
	};

	await expect(card.getByRole("button", { name: "All time" })).toHaveAttribute("aria-pressed", "true");
	await expectRows(expected.mostPlayedAllTime);

	await card.getByRole("button", { name: "This year" }).click();
	await expectRows(expected.mostPlayedThisYear);

	await card.getByRole("button", { name: "This month" }).click();
	await expectRows(expected.mostPlayedThisMonth);
});

test("sums the collection, what was paid for it and what was added last", async ({ page }) => {
	const card = page.getByRole("region", { name: "Collection" });
	const { gameCount, ownedCount, wantedCount, forTradeCount, previouslyOwnedCount, ownedValue } = expected;
	const summary = `${gameCount} games: Owned ${ownedCount}, Wanted ${wantedCount}, For trade ${forTradeCount}, Previously owned ${previouslyOwnedCount}`;

	await expect(card.getByRole("img", { name: summary })).toBeVisible();
	await expect(card.getByText(`€${ownedValue.toLocaleString("en-US")}`)).toBeVisible();
	await expect(card.getByText(`${ownedCount} of ${ownedCount} priced`)).toBeVisible();
	await expect(card.getByRole("link", { name: "Games", exact: true })).toHaveAttribute("href", "/games");
	for (const title of expected.recentlyAdded) {
		await expect(card.getByRole("link", { name: title }).first()).toBeVisible();
	}
});

test("nudges about the games gathering dust on the shelf", async ({ page }) => {
	const { shameCount, shameNeverPlayedCount, shameValue } = expected;
	const nudge = page.getByRole("link", { name: `${shameCount} games on the Shelf of Shame` });

	await expect(nudge).toHaveAttribute("href", "/shames");
	await expect(nudge).toContainText(`${shameNeverPlayedCount} never played`);
	await expect(nudge).toContainText(`€${shameValue.toLocaleString("en-US")} paid`);
});

test("shows the uploaded images", async ({ page }) => {
	await expectVisibleImagesToLoad(page);
});

test("opens the details behind the cards", async ({ page }) => {
	const [topGame] = expected.mostPlayedAllTime;
	const topGameId = seededGames.findIndex((game) => game.title === topGame[0]) + 1;
	const [firstPlayer] = expected.topPlayers;

	await page.getByRole("region", { name: "Most played" }).getByRole("link", { name: topGame[0] }).click();
	await expect(page).toHaveURL(`/games/${topGameId}`);

	await page.goto("/");
	await page.getByRole("region", { name: "Top players" }).getByRole("link", { name: firstPlayer.name }).click();
	await expect(page).toHaveURL(/\/players\/\d+$/);
	await expect(page.getByRole("heading", { name: firstPlayer.name, level: 1 })).toBeVisible();

	await page.goto("/");
	await page
		.getByRole("region", { name: "Collection" })
		.getByRole("link", { name: expected.recentlyAdded[0] })
		.first()
		.click();
	await expect(page).toHaveURL(`/games/${seededGames.length}`);
});
