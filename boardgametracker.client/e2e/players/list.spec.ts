import type { Page } from "@playwright/test";
import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { percent, playerId, relativeTime, statsOf } from "../support/playerStats";
import { expected, players } from "../support/seedData";

const stats = players.map((name) => ({ name, ...statsOf(name) }));

const rows = (page: Page) => page.getByRole("main").getByRole("listitem").getByRole("link");

const record = (player: (typeof stats)[number]) => `${player.name} ${player.wins} of ${player.sessions} wins`;

const expectRows = async (page: Page, names: (string | RegExp)[]) => {
	await expect(rows(page)).toHaveCount(names.length);
	for (const [index, name] of names.entries()) {
		await expect(rows(page).nth(index)).toHaveAccessibleName(name);
	}
};

const expectLinks = async (page: Page, shown: typeof stats) => {
	for (const [index, player] of shown.entries()) {
		await expect(rows(page).nth(index)).toHaveAttribute("href", `/players/${playerId(player.name)}`);
	}
};

const byName = [...stats].sort((a, b) => a.name.localeCompare(b.name));

test("orders the players on the last session they played", async ({ page }) => {
	const recent = [...byName].sort((a, b) => b.last.start.getTime() - a.last.start.getTime());
	await page.goto("/players");

	await expect(page.getByRole("button", { name: "Recent" })).toHaveAttribute("aria-pressed", "true");
	await expect(page.getByText(`${expected.playerCount} players`, { exact: true })).toBeVisible();
	await expectRows(
		page,
		recent.map((player) => new RegExp(`^${record(player)} ${relativeTime(player.last.start)} last played$`)),
	);
	await expectLinks(page, recent);
});

test("sorts on the name and shows the win rate", async ({ page }) => {
	await page.goto("/players");

	await page.getByRole("button", { name: "Name" }).click();

	await expect(page).toHaveURL("/players?sort=name");
	await expectRows(
		page,
		byName.map((player) => `${record(player)} ${percent(player.wins, player.sessions)} win rate`),
	);
	await expectLinks(page, byName);
});

test("sorts on the sessions and goes back to recent", async ({ page }) => {
	const bySessions = [...byName].sort((a, b) => b.sessions - a.sessions);
	await page.goto("/players");

	await page.getByRole("button", { name: "Sessions" }).click();

	await expect(page).toHaveURL("/players?sort=sessions");
	await expectRows(
		page,
		bySessions.map((player) => `${record(player)} ${player.sessions} sessions`),
	);

	await page.getByRole("button", { name: "Recent" }).click();
	await expect(page).toHaveURL("/players");
});

test("shows the avatar of every player", async ({ page }) => {
	await page.goto("/players");
	await expect(rows(page)).toHaveCount(expected.playerCount);

	await expect(page.getByRole("main").locator("img:visible")).toHaveCount(expected.playerCount);
	await expectVisibleImagesToLoad(page);
});

test("opens a player", async ({ page }) => {
	const [name] = players;
	await page.goto("/players");

	await rows(page).filter({ hasText: name }).click();

	await expect(page).toHaveURL(`/players/${playerId(name)}`);
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
});

test("filters on part of a name, ignoring capitals", async ({ page }) => {
	const needle = "KE";
	const matches = byName.filter((player) => player.name.toLowerCase().includes(needle.toLowerCase()));
	expect(matches.length).toBeGreaterThan(1);
	await page.goto("/players?sort=name");

	await page.getByRole("textbox", { name: "Filter by name" }).fill(needle);

	await expect(page).toHaveURL(`/players?q=${needle}&sort=name`);
	await expect(page.getByText(`${matches.length} of ${expected.playerCount} players`)).toBeVisible();
	await expectRows(
		page,
		matches.map((player) => `${record(player)} ${percent(player.wins, player.sessions)} win rate`),
	);
});

test("keeps a filter from the address and clears it", async ({ page }) => {
	const name = players[1];
	await page.goto(`/players?q=${encodeURIComponent(name)}`);

	await expect(page.getByRole("textbox", { name: "Filter by name" })).toHaveValue(name);
	await expect(rows(page)).toHaveCount(1);

	await page.getByRole("button", { name: "Clear search" }).click();

	await expect(page).toHaveURL("/players");
	await expect(rows(page)).toHaveCount(expected.playerCount);
	await expect(page.getByRole("textbox", { name: "Filter by name" })).toHaveValue("");
});

test("says when no player matches and offers to clear the filter", async ({ page }) => {
	const query = "nobody here";
	await page.goto("/players");

	await page.getByRole("textbox", { name: "Filter by name" }).fill(query);

	await expect(page.getByText(`No players match “${query}”`)).toBeVisible();
	await expect(page.getByText(`0 of ${expected.playerCount} players`)).toBeVisible();
	await expect(rows(page)).toHaveCount(0);

	await page.getByRole("button", { name: "Clear filter" }).click();

	await expect(page).toHaveURL("/players");
	await expect(rows(page)).toHaveCount(expected.playerCount);
});

test("ignores an unknown sort in the address", async ({ page }) => {
	test.fail(true, "BUG: /players?sort=wins crashes the page because the raw value survives validateSearch");
	await page.goto("/players?sort=wins");

	await expect(page.getByRole("button", { name: "Recent" })).toHaveAttribute("aria-pressed", "true", {
		timeout: 2_000,
	});
});

test("treats a number in the address as a filter", async ({ page }) => {
	test.fail(true, "BUG: /players?q=3 crashes the page because the router parses the value to a number");
	await page.goto("/players?q=3");

	await expect(page.getByText(`No players match “3”`)).toBeVisible({ timeout: 2_000 });
});

test("leads to the leaderboard and to adding a player", async ({ page }) => {
	await page.goto("/players");
	await page.getByRole("button", { name: "Leaderboard" }).click();
	await expect(page).toHaveURL(/\/leaderboard/);

	await page.goto("/players");
	await page.getByRole("button", { name: "Add player" }).click();
	await expect(page).toHaveURL("/players/new");
});

test("offers a reader no way to add a player", async ({ readerPage }) => {
	await readerPage.goto("/players");

	await expect(rows(readerPage)).toHaveCount(expected.playerCount);
	await expect(readerPage.getByRole("button", { name: "Leaderboard" })).toBeVisible();
	await expect(readerPage.getByRole("button", { name: "Add player" })).toHaveCount(0);
});

test("invites you to add the first player", async ({ page }) => {
	await page.route("**/api/player/overview", (route) => route.fulfill({ json: [] }));
	await page.goto("/players");
	const main = page.getByRole("main");

	await expect(main.getByText("No players yet")).toBeVisible();
	await expect(main.getByText("Add the people you play with to track wins and rivalries.")).toBeVisible();

	await main.getByRole("button", { name: "Add player" }).click();
	await expect(page).toHaveURL("/players/new");
});

test("offers a reader nothing to do without players", async ({ readerPage }) => {
	await readerPage.route("**/api/player/overview", (route) => route.fulfill({ json: [] }));
	await readerPage.goto("/players");
	const main = readerPage.getByRole("main");

	await expect(main.getByText("No players yet")).toBeVisible();
	await expect(main.getByRole("button", { name: "Add player" })).toHaveCount(0);
});

test("counts a single player in the singular", async ({ page }) => {
	const [player] = stats;
	await page.route("**/api/player/overview", (route) =>
		route.fulfill({
			json: [
				{
					id: 1,
					name: player.name,
					image: null,
					playCount: 1,
					winCount: 1,
					lastPlayed: new Date().toISOString(),
				},
			],
		}),
	);
	await page.goto("/players?sort=sessions");

	await expect(page.getByText("1 player", { exact: true })).toBeVisible();
	await expectRows(page, [`${player.name} 1 of 1 win 1 session`]);
});

test("explains when the players cannot be loaded and retries", async ({ page }) => {
	await page.route("**/api/player/overview", (route) => route.abort());
	await page.goto("/players");
	const main = page.getByRole("main");

	await expect(main.getByText("Couldn't load players")).toBeVisible();
	await expect(main.getByText("The server didn't answer. Check that it is running, then try again.")).toBeVisible();

	await page.unroute("**/api/player/overview");
	await main.getByRole("button", { name: "Try again" }).click();

	await expect(rows(page)).toHaveCount(expected.playerCount);
});
