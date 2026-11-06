import type { Page } from "@playwright/test";
import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import { gameId, playerId, sessionId, sessionsOf } from "../support/playerStats";
import { linkedPlayer, players, sessions } from "../support/seedData";

const rowsOf = (page: Page) => page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");

const openSessions = async (page: Page, name: string) => {
	await page.goto(`/players/${playerId(name)}/sessions`);
	await expect(page.getByRole("heading", { name: `${name} sessions`, level: 1 })).toBeVisible();
};

test("sorts the sessions of a player on duration", async ({ page }) => {
	const minutes = sessions
		.filter((session) => session.scores.some((score) => score.player === linkedPlayer))
		.map((session) => session.minutes);
	await page.goto("/players/1/sessions");
	const firstRow = page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row").first();
	const header = page.getByRole("columnheader", { name: "Duration" }).getByRole("button");

	await header.click();
	await expect(firstRow.getByRole("cell").nth(3)).toHaveText(duration(Math.max(...minutes)));

	await header.click();
	await expect(firstRow.getByRole("cell").nth(3)).toHaveText(duration(Math.min(...minutes)));
});

test("sorts on the date and the game", async ({ page }) => {
	const played = sessionsOf(linkedPlayer);
	const titles = played.map((session) => session.game).sort((a, b) => a.localeCompare(b));
	await openSessions(page, linkedPlayer);
	const firstRow = rowsOf(page).first();
	const newest = displayDate(played[0].start);
	const oldest = displayDate(played[played.length - 1].start);
	const date = page.getByRole("columnheader", { name: "Date" }).getByRole("button");

	await date.click();
	await expect(firstRow.getByRole("cell").first()).toHaveText(newest);
	await date.click();
	await expect(firstRow.getByRole("cell").first()).toHaveText(oldest);

	const game = page.getByRole("columnheader", { name: "Game" }).getByRole("button");
	await game.click();
	await expect(firstRow.getByRole("cell").nth(2)).toContainText(titles[0]);
	await game.click();
	await expect(firstRow.getByRole("cell").nth(2)).toContainText(titles[titles.length - 1]);
});

for (const name of [linkedPlayer, players[players.length - 1]]) {
	test(`lists every session of ${name} newest first`, async ({ page }) => {
		const played = sessionsOf(name);
		await openSessions(page, name);
		const rows = rowsOf(page);

		await expect(rows).toHaveCount(played.length);
		for (const [index, session] of played.entries()) {
			const cells = rows.nth(index).getByRole("cell");
			const seats = rows.nth(index).getByRole("listitem");

			await expect(cells.nth(0)).toHaveText(displayDate(session.start));
			await expect(cells.nth(1)).toHaveText(/^\d\d:\d\d$/);
			await expect(cells.nth(2)).toContainText(session.game);
			await expect(cells.nth(3)).toHaveText(duration(session.minutes));
			await expect(seats).toHaveCount(session.scores.length);
			for (const { player, score } of session.scores) {
				await expect(seats.filter({ hasText: player })).toHaveText(new RegExp(`^${player}${score ?? ""}$`));
			}
			await expect(cells.nth(4).getByRole("img", { name: "Won" })).toHaveCount(
				session.teamWon === true ? session.scores.length : session.winner ? 1 : 0,
			);
			if (session.isCoop) await expect(cells.nth(4)).toContainText(session.teamWon ? "Co-op · won" : "Co-op · lost");
		}
	});
}

test("loads the cover of the games", async ({ page }) => {
	await openSessions(page, linkedPlayer);

	await expectVisibleImagesToLoad(page);
});

test("opens the game from its cover", async ({ page }) => {
	const [session] = sessionsOf(linkedPlayer);
	await openSessions(page, linkedPlayer);

	await rowsOf(page).first().getByRole("button", { name: session.game }).click();

	await expect(page).toHaveURL(`/games/${gameId(session.game)}`);
});

test("opens a session to edit it", async ({ page }) => {
	const [session] = sessionsOf(linkedPlayer);
	await openSessions(page, linkedPlayer);

	await rowsOf(page).first().getByRole("button", { name: "Edit" }).click();

	await expect(page).toHaveURL(`/sessions/update/${sessionId(session)}`);
});

test("keeps a session when deleting is cancelled", async ({ page }) => {
	const played = sessionsOf(linkedPlayer);
	await openSessions(page, linkedPlayer);

	await rowsOf(page).first().getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();

	await expect(page.getByRole("dialog")).toBeHidden();
	await expect(rowsOf(page)).toHaveCount(played.length);
});

test("preselects the player when logging a session", async ({ page }) => {
	test.fail(true, "BUG: Log session on the sessions page of a player does not preselect that player");
	await openSessions(page, linkedPlayer);

	await page.getByRole("main").getByRole("button", { name: "Log session" }).click();

	await expect(page).toHaveURL(`/sessions/new?player=${playerId(linkedPlayer)}`);
});

test("goes back to the player", async ({ page }) => {
	await openSessions(page, linkedPlayer);

	await page.getByRole("button", { name: "Back" }).click();

	await expect(page).toHaveURL(`/players/${playerId(linkedPlayer)}`);
});

test("offers a reader no changes", async ({ readerPage }) => {
	await openSessions(readerPage, linkedPlayer);

	await expect(rowsOf(readerPage)).toHaveCount(sessionsOf(linkedPlayer).length);
	await expect(readerPage.getByRole("columnheader", { name: "Actions" })).toHaveCount(0);
	await expect(readerPage.getByRole("button", { name: "Edit" })).toHaveCount(0);
	await expect(readerPage.getByRole("button", { name: "Delete" })).toHaveCount(0);
	await expect(readerPage.getByRole("button", { name: "Log session" })).toHaveCount(0);
});

test("shows the sessions as cards on a phone", async ({ page }) => {
	const played = sessionsOf(linkedPlayer);
	await page.setViewportSize({ width: 375, height: 812 });
	await openSessions(page, linkedPlayer);

	const cards = page.getByTestId("session-card-list");

	await expect(page.getByRole("table")).toBeHidden();
	await expect(cards.getByRole("button", { name: "Edit" })).toHaveCount(played.length);
	await expect(cards.getByRole("listitem").first()).toContainText(played[0].game);
});

test.describe("a player that does not exist", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("explains it instead of loading forever", async ({ page }) => {
		await page.goto("/players/9999/sessions");

		await expect(page.getByText("Could not find player")).toBeVisible();
	});

	test("leads back to the players", async ({ page }) => {
		test.fail(true, "BUG: the sessions page of a missing player has no way back, unlike the one of a missing game");
		await page.goto("/players/9999/sessions");
		await expect(page.getByText("Could not find player")).toBeVisible();

		await page.getByRole("button", { name: /^Back/ }).click({ timeout: 2_000 });

		await expect(page).toHaveURL("/players");
	});
});
