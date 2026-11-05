import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { displayDate, duration } from "../support/format";
import { coopGame, games, mostPlayedGame, neverPlayedGame, sessions, unscoredGame } from "../support/seedData";

const idOf = (title: string) => games.findIndex((game) => game.title === title) + 1;

const sessionsOf = (title: string) =>
	sessions.filter((session) => session.game === title).sort((a, b) => b.start.getTime() - a.start.getTime());

const rowsOf = (page: Page) => page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");

const openSessions = async (page: Page, title: string) => {
	await page.goto(`/games/${idOf(title)}/sessions`);
	await expect(page.getByRole("heading", { name: `${title} sessions`, level: 1 })).toBeVisible();
};

test("lists every session newest first with its scores and winner", async ({ page }) => {
	const played = sessionsOf(mostPlayedGame.title);
	await openSessions(page, mostPlayedGame.title);
	const rows = rowsOf(page);

	await expect(rows).toHaveCount(played.length);
	for (const [index, session] of played.entries()) {
		const row = rows.nth(index);
		const seats = row.getByRole("listitem");
		await expect(row.getByRole("cell").first()).toHaveText(displayDate(session.start));
		await expect(row.getByRole("cell").nth(2)).toHaveText(duration(session.minutes));
		await expect(seats).toHaveCount(session.scores.length);
		for (const { player, score } of session.scores) {
			await expect(seats.filter({ hasText: player })).toContainText(String(score));
		}
		await expect(seats.filter({ has: page.getByRole("img", { name: "Won" }) })).toHaveText(
			new RegExp(session.winner ?? ""),
		);
	}
});

test("shows the team result of co-op sessions without scores", async ({ page }) => {
	const played = sessionsOf(coopGame.title);
	await openSessions(page, coopGame.title);
	const rows = rowsOf(page);

	await expect(rows).toHaveCount(played.length);
	for (const [index, session] of played.entries()) {
		const row = rows.nth(index);
		await expect(row).toContainText(session.teamWon ? "Co-op · won" : "Co-op · lost");
		await expect(row.getByRole("img", { name: "Won" })).toHaveCount(session.teamWon ? session.scores.length : 0);
		await expect(row.getByRole("list")).not.toContainText(/\d/);
	}
});

test("shows the winner of unscored sessions without scores", async ({ page }) => {
	const played = sessionsOf(unscoredGame.title);
	await openSessions(page, unscoredGame.title);
	const rows = rowsOf(page);

	await expect(rows).toHaveCount(played.length);
	for (const [index, session] of played.entries()) {
		const row = rows.nth(index);
		await expect(row.getByRole("list")).not.toContainText(/\d/);
		await expect(row.getByRole("listitem").filter({ has: page.getByRole("img", { name: "Won" }) })).toHaveText(
			new RegExp(session.winner ?? ""),
		);
	}
});

test("says when a game has no sessions", async ({ page }) => {
	await openSessions(page, neverPlayedGame.title);

	await expect(page.getByRole("table")).toContainText("No data yet");
});

test("sorts on the duration column", async ({ page }) => {
	const minutes = sessionsOf(mostPlayedGame.title).map((session) => session.minutes);
	await openSessions(page, mostPlayedGame.title);
	const firstDuration = rowsOf(page).first().getByRole("cell").nth(2);
	const header = page.getByRole("columnheader", { name: "Duration" }).getByRole("button");

	await header.click();
	await expect(firstDuration).toHaveText(duration(Math.max(...minutes)));

	await header.click();
	await expect(firstDuration).toHaveText(duration(Math.min(...minutes)));
});

test("offers no sorting on columns without an order", async ({ page }) => {
	await openSessions(page, mostPlayedGame.title);

	for (const column of ["Players", "Comment", "Actions"]) {
		await expect(page.getByRole("columnheader", { name: column }).getByRole("button")).toHaveCount(0);
	}
});

test("leads to logging a session and back to the game", async ({ page }) => {
	await openSessions(page, mostPlayedGame.title);

	await page.getByRole("main").getByRole("button", { name: "Log session" }).click();
	await expect(page).toHaveURL(`/sessions/new/${idOf(mostPlayedGame.title)}`);

	await openSessions(page, mostPlayedGame.title);
	await page.getByRole("button", { name: "Back" }).click();
	await expect(page).toHaveURL(`/games/${idOf(mostPlayedGame.title)}`);
});

test("opens a session to edit it", async ({ page }) => {
	await openSessions(page, mostPlayedGame.title);

	await rowsOf(page).first().getByRole("button", { name: "Edit" }).click();

	await expect(page).toHaveURL(/\/sessions\/update\/\d+$/);
});

test("keeps a session when deleting is cancelled", async ({ page }) => {
	const played = sessionsOf(mostPlayedGame.title);
	await openSessions(page, mostPlayedGame.title);

	await rowsOf(page).first().getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();

	await expect(page.getByRole("dialog")).toBeHidden();
	await expect(rowsOf(page)).toHaveCount(played.length);
});

test.describe("a game that does not exist", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("explains it and leads back to the games", async ({ page }) => {
		await page.goto("/games/9999/sessions");

		await expect(page.getByText("Could not find game").first()).toBeVisible();
		await page.getByRole("button", { name: "Back to games" }).click();
		await expect(page).toHaveURL("/games");
	});
});
