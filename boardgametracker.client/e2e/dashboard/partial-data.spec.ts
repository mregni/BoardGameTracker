import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";

type Statistics = Record<string, unknown>;

const openWith = async (page: Page, patch: (statistics: Statistics) => Statistics) => {
	await page.route("**/api/dashboard/statistics", async (route) => {
		const response = await route.fetch();
		if (!response.ok()) {
			await route.fulfill({ response });
			return;
		}
		const statistics = await response.json();
		await route.fulfill({ response, json: { ...statistics, ...patch(statistics) } });
	});
	await page.goto("/");
	await expect(page.getByRole("heading", { name: "Top players" })).toBeVisible();
};

test("uses a neutral title without a linked player", async ({ page }) => {
	await openWith(page, () => ({ linkedPlayer: null }));
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { name: "Dashboard", level: 1 })).toBeVisible();
	await expect(main.getByText(/This month you played/)).toHaveCount(0);
});

test("invites you to plan a game night when none is planned", async ({ page }) => {
	await openWith(page, () => ({ nextGameNight: null, plannedGameNights: 0 }));
	const card = page.getByRole("region", { name: "Next game night" });

	await expect(card).toContainText("No game night planned");
	await expect(card.getByText("Plan one")).toBeVisible();
});

test("marks a game night that is in progress", async ({ page }) => {
	await openWith(page, (statistics) => ({
		nextGameNight: { ...(statistics.nextGameNight as object), startDate: new Date().toISOString() },
	}));

	await expect(page.getByRole("region", { name: "Game night in progress" })).toContainText("playing now");
});

test("explains the cards when no session was logged", async ({ page }) => {
	await openWith(page, () => ({
		recentActivities: [],
		topPlayers: [],
		mostPlayedGames: [],
		mostPlayedGamesThisYear: [],
		mostPlayedGamesLast12Months: [],
	}));

	await expect(page.getByRole("region", { name: "Last sessions" })).toContainText("No sessions logged yet");
	await expect(page.getByRole("region", { name: "Top players" })).toContainText("No sessions played yet");
	await expect(page.getByRole("region", { name: "Most played" })).toContainText("No sessions played yet");
});

test("says when nothing was played in a period", async ({ page }) => {
	await openWith(page, () => ({ mostPlayedGamesThisYear: [] }));
	const card = page.getByRole("region", { name: "Most played" });

	await card.getByRole("button", { name: "This year" }).click();

	await expect(card).toContainText("Nothing played in this period");
});

test("leaves out the shelf nudge when nothing gathers dust", async ({ page }) => {
	await openWith(page, () => ({ shelfOfShame: null }));

	await expect(
		page.getByRole("link", { name: /Shelf of Shame/ }).and(page.getByRole("main").getByRole("link")),
	).toHaveCount(0);
});

test("says when no game has a price", async ({ page }) => {
	await openWith(page, () => ({ totalCollectionValue: null, pricedGameCount: 0 }));

	await expect(page.getByRole("region", { name: "Collection" })).toContainText("No prices yet");
});
