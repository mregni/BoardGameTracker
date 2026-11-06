import type { Page } from "@playwright/test";
import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import {
	FEAT_DESCRIPTION,
	FEAT_TYPES,
	featsOf,
	gameId,
	MILESTONE_TYPES,
	milestonesOf,
	percent,
	playerId,
	sessionId,
	sessionLine,
	sessionsOf,
	statsOf,
} from "../support/playerStats";
import { expected, linkedPlayer, players, sessions, standingsOf } from "../support/seedData";

const noCoopPlayer = players[players.length - 1];

const open = async (page: Page, name: string) => {
	await page.goto(`/players/${playerId(name)}`);
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
};

const figure = (page: Page, label: string) =>
	page
		.getByRole("main")
		.getByRole("term")
		.filter({ hasText: new RegExp(`^${label}$`) })
		.locator("xpath=..")
		.getByRole("definition");

const mostPlayed = (page: Page) => page.getByRole("main").getByRole("listitem").locator('a[href^="/games/"]');
const rivals = (page: Page) => page.getByRole("main").getByRole("listitem").locator('a[href^="/compare"]');
const recent = (page: Page) => page.getByRole("main").locator('a[href^="/sessions/update/"]');
const milestones = (page: Page) =>
	page.getByRole("heading", { name: "Milestones" }).locator("xpath=..").getByRole("listitem");
const feats = (page: Page) => page.getByRole("heading", { name: "Feats" }).locator("xpath=..").getByRole("listitem");

for (const name of players) {
	test.describe(name, () => {
		const stats = statsOf(name);
		const id = playerId(name);

		test.beforeEach(async ({ page }) => {
			await open(page, name);
		});

		test("shows the record", async ({ page }) => {
			await expect(figure(page, "Sessions")).toHaveText(String(stats.sessions));
			await expect(figure(page, "Wins")).toHaveText([
				String(stats.wins),
				`${percent(stats.wins, stats.sessions)} win rate`,
			]);
			await expect(figure(page, "Total play time")).toHaveText(duration(stats.minutes));
			await expect(figure(page, "Different games")).toHaveText(String(stats.distinctGames));
		});

		test("shows the last ten results, oldest first", async ({ page }) => {
			const main = page.getByRole("main");
			const wins = stats.form.filter((result) => result === "W").length;

			await expect(main.getByText(`Last ${stats.form.length} sessions`)).toBeVisible();
			await expect(main.getByText(`${wins} won, ${stats.form.length - wins} lost`)).toBeAttached();
			await expect(main.locator('ol[aria-hidden="true"] li')).toHaveText(stats.form);
		});

		test("shows the facts", async ({ page }) => {
			const [top] = stats.gameRecords;
			const favorites = stats.gameRecords.filter((record) => record.sessions === top.sessions);
			const favorite = figure(page, "Favorite game");

			await expect(favorite.nth(1)).toHaveText(`${top.sessions} sessions`);
			const title = (await favorite.first().textContent()) ?? "";
			expect(favorites.map((record) => record.title)).toContain(title);
			await expect(favorite.getByRole("link")).toHaveAttribute("href", `/games/${gameId(title)}`);

			await expect(figure(page, "Last played")).toHaveText([displayDate(stats.last.start), stats.last.game]);

			if (stats.coopWon + stats.coopLost > 0) {
				await expect(figure(page, "Co-op")).toHaveText(`${stats.coopWon} won · ${stats.coopLost} lost`);
			} else {
				await expect(figure(page, "Co-op")).toHaveCount(0);
			}

			if (stats.nemesis) {
				const nemesis = figure(page, "Nemesis");
				await expect(nemesis).toHaveText([
					stats.nemesis.name,
					`${stats.nemesis.wins} won, ${stats.nemesis.losses} lost`,
				]);
				await expect(nemesis.getByRole("link")).toHaveAttribute(
					"href",
					`/compare?left=${id}&right=${playerId(stats.nemesis.name)}`,
				);
			} else {
				await expect(figure(page, "Nemesis")).toHaveCount(0);
			}
		});

		test("shows the five most recent sessions", async ({ page }) => {
			const latest = sessionsOf(name).slice(0, 5);
			const rows = recent(page);

			await expect(rows).toHaveCount(latest.length);
			for (const [index, session] of latest.entries()) {
				await expect(rows.nth(index)).toHaveAccessibleName(sessionLine(session, name));
				await expect(rows.nth(index)).toHaveAttribute("href", `/sessions/update/${sessionId(session)}`);
			}
		});

		test("shows the head-to-head record against the five most met players", async ({ page }) => {
			const shown = stats.rivals.slice(0, 5);
			const rows = rivals(page);

			await expect(page.getByRole("heading", { name: "Head-to-head" })).toBeVisible();
			await expect(rows).toHaveCount(shown.length);
			for (const [index, rival] of shown.entries()) {
				await expect(rows.nth(index)).toHaveAccessibleName(
					`${rival.name} ${rival.sessions} sessions ${rival.wins} won, ${rival.losses} lost`,
				);
				await expect(rows.nth(index)).toHaveAttribute("href", `/compare?left=${id}&right=${playerId(rival.name)}`);
			}
		});

		test("shows the most played games", async ({ page }) => {
			const rows = mostPlayed(page);
			await expect(rows).toHaveCount(5);
			const hrefs = await rows.evaluateAll((links) => links.map((link) => link.getAttribute("href")));

			const shown = hrefs.map((href) => {
				const record = stats.gameRecords.find((game) => `/games/${gameId(game.title)}` === href);
				if (!record) throw new Error(`${href} is not a game of ${name}`);
				return record;
			});

			expect(shown.map((record) => record.sessions)).toEqual(
				stats.gameRecords.slice(0, 5).map((record) => record.sessions),
			);
			for (const [index, record] of shown.entries()) {
				const wins = record.wins === 1 ? "1 win" : `${record.wins} wins`;
				await expect(rows.nth(index)).toHaveAccessibleName(
					`${record.title} ${record.sessions} sessions · ${wins} ${percent(record.wins, record.sessions)}`,
				);
			}
		});

		test("shows the earned milestones with the next goal", async ({ page }) => {
			for (const milestone of milestonesOf(stats).filter((x) => x.earned > 0)) {
				const tile = milestones(page).filter({ hasText: milestone.title });

				await expect(tile).toContainText(milestone.goal);
				await expect(tile).toContainText(`${milestone.earned} of 4 levels`);
			}
			for (const milestone of milestonesOf(stats).filter((x) => x.earned === 0)) {
				await expect(milestones(page).filter({ hasText: milestone.title })).toHaveCount(0);
			}
		});

		test("shows the earned feats and hides the others", async ({ page }) => {
			for (const feat of featsOf(name)) {
				await expect(feats(page).getByRole("button", { name: feat.title, exact: true })).toHaveCount(
					feat.earned ? 1 : 0,
				);
			}
		});

		test("shows the leaderboard position", async ({ page }) => {
			const rank = standingsOf(sessions).findIndex((standing) => standing.name === name) + 1;
			const link = page.getByRole("link", { name: `#${rank} of ${expected.playerCount} on the leaderboard` });

			await expect(link).toHaveAttribute("href", "/leaderboard");
		});
	});
}

test("reveals and hides the locked achievements", async ({ page }) => {
	await open(page, noCoopPlayer);
	const earnedFeats = await feats(page).count();
	const earned = (await milestones(page).count()) + earnedFeats;
	const locked = MILESTONE_TYPES + FEAT_TYPES - earned;
	const show = page.getByRole("button", { name: `Show ${locked} locked` });

	await show.click();

	await expect(milestones(page)).toHaveCount(MILESTONE_TYPES);
	await expect(feats(page)).toHaveCount(FEAT_TYPES);
	await expect(milestones(page).filter({ hasText: "Next: Play 5 solo sessions." })).toContainText("0 of 4 levels");
	await expect(feats(page).getByRole("img", { name: /, Not earned yet$/ })).toHaveCount(FEAT_TYPES - earnedFeats);

	await page.getByRole("button", { name: "Hide locked" }).click();

	await expect(milestones(page).or(feats(page))).toHaveCount(earned);
	await expect(show).toBeVisible();
});

test("explains a feat when you hover it", async ({ page }) => {
	const name = players.find((player) => featsOf(player)[0].earned) ?? linkedPlayer;
	await open(page, name);

	await feats(page)
		.getByRole("button", { name: featsOf(name)[0].title })
		.hover();

	await expect(page.getByRole("tooltip")).toContainText(FEAT_DESCRIPTION);
});

test("shows the portrait and all avatars", async ({ page }) => {
	await open(page, linkedPlayer);

	await expect(page.getByRole("complementary").getByRole("img", { name: linkedPlayer })).toBeVisible();
	await expectVisibleImagesToLoad(page);
});

test("names the page after the player", async ({ page }) => {
	await open(page, linkedPlayer);

	await expect(page).toHaveTitle(`${linkedPlayer} · BoardGameTracker`);
});

test("leads to the actions of a player", async ({ page }) => {
	const id = playerId(linkedPlayer);
	await open(page, linkedPlayer);
	const main = page.getByRole("main");

	await expect(main.getByRole("link", { name: "Log a session" })).toHaveAttribute("href", `/sessions/new?player=${id}`);
	await expect(main.getByRole("link", { name: "Compare" })).toHaveAttribute("href", `/compare?left=${id}`);

	await main.getByRole("button", { name: "View sessions" }).click();
	await expect(page).toHaveURL(`/players/${id}/sessions`);
});

test("preselects the player when logging a session", async ({ page }) => {
	await open(page, linkedPlayer);

	await page.getByRole("link", { name: "Log a session" }).click();

	await expect(page).toHaveURL(`/sessions/new?player=${playerId(linkedPlayer)}`);
	await expect(page.getByRole("main").getByText(linkedPlayer, { exact: true })).toBeVisible();
});

test("goes back to the players", async ({ page }) => {
	await page.goto("/players");
	await page.getByRole("link", { name: new RegExp(`^${linkedPlayer} `) }).click();

	await page.getByRole("button", { name: "Back to players" }).click();

	await expect(page).toHaveURL("/players");
});

test("offers a reader no changes", async ({ readerPage }) => {
	await open(readerPage, linkedPlayer);
	const main = readerPage.getByRole("main");

	await expect(main.getByRole("link", { name: "Compare" })).toBeVisible();
	await expect(main.getByRole("button", { name: "Edit" })).toHaveCount(0);
	await expect(main.getByRole("button", { name: "Delete" })).toHaveCount(0);
	await expect(main.getByRole("link", { name: "Log a session" })).toHaveCount(0);
	await expect(recent(readerPage)).toHaveCount(0);
	await expect(main.getByText(displayDate(sessionsOf(linkedPlayer)[0].start), { exact: false })).not.toHaveCount(0);
});

test("offers a user changes", async ({ writerPage }) => {
	await open(writerPage, linkedPlayer);
	const main = writerPage.getByRole("main");

	await expect(main.getByRole("button", { name: "Edit" })).toBeVisible();
	await expect(main.getByRole("button", { name: "Delete" })).toBeVisible();
	await expect(main.getByRole("link", { name: "Log a session" })).toBeVisible();
	await expect(recent(writerPage)).toHaveCount(5);
});

test.describe("a player that does not exist", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("explains it and leads back to the players", async ({ page }) => {
		await page.goto("/players/9999");
		const main = page.getByRole("main");

		await expect(main.getByRole("heading", { name: "Player not found", level: 1 })).toBeVisible();
		await expect(main.getByText("This player may have been deleted, or the link is wrong.")).toBeVisible();
		await expect(page).toHaveTitle("Player not found · BoardGameTracker");

		await main.getByRole("button", { name: "Back to players" }).last().click();

		await expect(page).toHaveURL("/players");
	});
});

test("treats a malformed player address as not found", async ({ page }) => {
	test.fail(true, "BUG: /players/abc throws in the router and shows the generic error page");
	await page.goto("/players/abc");

	await expect(page.getByRole("heading", { name: "Player not found", level: 1 })).toBeVisible({ timeout: 2_000 });
});

test("explains when the statistics cannot be loaded and retries", async ({ page }) => {
	const url = `**/api/player/${playerId(linkedPlayer)}/statistics`;
	await page.route(url, (route) => route.abort());
	await page.goto(`/players/${playerId(linkedPlayer)}`);
	const retry = page.getByRole("button", { name: "Try again" });

	await expect(retry).toBeVisible();

	await page.unroute(url);
	await retry.click();

	await expect(page.getByRole("heading", { name: linkedPlayer, level: 1 })).toBeVisible();
	await expect(figure(page, "Sessions")).toHaveText(String(statsOf(linkedPlayer).sessions));
});
