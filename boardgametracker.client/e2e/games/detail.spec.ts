import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { gameLink } from "../support/gamesPage";
import { expansions, mostPlayedGame, neverPlayedGame, sessions, standingsOf } from "../support/seedData";

test.describe("detail of a played game", () => {
	const played = sessions.filter((session) => session.game === mostPlayedGame.title);
	const scores = played.flatMap((session) =>
		session.scores.map((score) => ({ ...score, won: score.player === session.winner })),
	);
	const values = scores.map((score) => score.score);
	const winning = scores.filter((score) => score.won).map((score) => score.score);
	const losing = scores.filter((score) => !score.won).map((score) => score.score);

	test.beforeEach(async ({ page }) => {
		await page.goto("/games/1");
		await expect(page.getByRole("heading", { name: mostPlayedGame.title, level: 1 })).toBeVisible();
	});

	test("shows the header with its actions", async ({ page }) => {
		const main = page.getByRole("main");
		const gameExpansions = expansions.filter((expansion) => expansion.game === mostPlayedGame.title);

		await expect(main.getByText("Owned", { exact: true })).toBeVisible();
		await expect(main.getByText(mostPlayedGame.description)).toBeVisible();
		await expect(main.getByRole("button", { name: "Log session" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Edit" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Delete" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Manuals (0)" })).toBeVisible();
		await expect(main.getByRole("button", { name: `Expansions (${gameExpansions.length})` })).toBeVisible();
	});

	test("sums the plays in the statistics", async ({ page }) => {
		const hours = played.reduce((total, session) => total + session.minutes, 0) / 60;
		const region = page.getByRole("region", {
			name: new RegExp(`^Played ${played.length} times · ${hours}h in total`),
		});

		await expect(region).toContainText("Price / play");
		await expect(region).toContainText(`€${((mostPlayedGame.buyingPrice ?? 0) / played.length).toFixed(2)}`);
	});

	test("lists the recent sessions newest first", async ({ page }) => {
		const main = page.getByRole("main");
		const rows = main.getByRole("list").first().getByRole("listitem");

		await expect(rows).toHaveCount(5);
		for (const [index, session] of played.slice(0, 5).entries()) {
			const best = Math.max(...session.scores.map((score) => score.score));
			await expect(rows.nth(index)).toContainText(session.winner);
			await expect(rows.nth(index)).toContainText(`${session.scores.length} players`);
			await expect(rows.nth(index)).toContainText(String(best));
		}
		await expect(main.getByRole("link", { name: "View sessions" })).toHaveAttribute("href", "/games/1/sessions");
	});

	test("shows the scoring results without a player for the average", async ({ page }) => {
		const average = Math.round(values.reduce((total, value) => total + value, 0) / values.length);

		for (const [label, score] of [
			["Top score", Math.max(...values)],
			["Highest losing", Math.max(...losing)],
			["Average", average],
			["Lowest winning", Math.min(...winning)],
			["Lowest", Math.min(...values)],
		] as const) {
			await expect(page.getByRole("progressbar", { name: label, exact: true })).toHaveAttribute(
				"aria-valuetext",
				String(score),
			);
		}
		await expect(page.locator('a[href="/players/0"]')).toHaveCount(0);
	});

	test("charts the sessions by weekday and player count", async ({ page }) => {
		const main = page.getByRole("main");
		const threePlayers = played.filter((session) => session.scores.length === 3).length;
		const fourPlayers = played.length - threePlayers;

		await expect(main.getByRole("heading", { name: "Sessions by weekday" })).toBeVisible();
		await expect(main.getByText("Mon", { exact: true })).toBeVisible();
		await expect(main.getByRole("heading", { name: "Sessions by player count" })).toBeVisible();
		await expect(
			main.getByRole("img", {
				name: `3 players: ${threePlayers}, 4 players: ${fourPlayers}, Total: ${played.length}`,
			}),
		).toBeVisible();
	});

	test("shows the facts, the copy and the best players", async ({ page }) => {
		const side = page.getByRole("complementary").last();
		const [best] = standingsOf(played);

		await expect(side.getByRole("img", { name: mostPlayedGame.title }).first()).toBeVisible();
		await expect(side).toContainText(`Year published${mostPlayedGame.yearPublished}`);
		await expect(side).toContainText(`Players${mostPlayedGame.minPlayers}–${mostPlayedGame.maxPlayers}`);
		await expect(side).toContainText(`Duration${mostPlayedGame.minPlayTime}–${mostPlayedGame.maxPlayTime} min`);
		await expect(side).toContainText(`Minimum age${mostPlayedGame.minAge}+`);
		await expect(side).toContainText(`Rating${mostPlayedGame.rating} / 10`);
		await expect(side).toContainText(`Weight${mostPlayedGame.weight} / 5`);
		await expect(side).toContainText(`Cost€${mostPlayedGame.buyingPrice}`);
		await expect(side.getByRole("listitem").first()).toContainText(best.name);
		await expect(side.getByRole("listitem").first()).toContainText(`${best.sessions} sessions`);
		await expect(side.getByRole("listitem").first()).toContainText(`${best.wins} win`);
		await expectVisibleImagesToLoad(page);
	});
});

test("shows no statistics for a game that was never played", async ({ page }) => {
	await page.goto("/games");
	await gameLink(page, neverPlayedGame.title).click();
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { name: neverPlayedGame.title, level: 1 })).toBeVisible();
	await expect(main.getByRole("heading", { name: "Not on the table yet" })).toBeVisible();
	await expect(main.getByRole("button", { name: "Log the first play" })).toBeVisible();
	await expect(main.getByRole("heading", { name: "Recent sessions" })).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Scoring results" })).toHaveCount(0);
});
