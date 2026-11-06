import type { Page } from "@playwright/test";
import {
	choosePlayer,
	compareUrl,
	expectComparison,
	expectGameRows,
	expectPair,
	leftPicker,
	openCompare,
	recentRows,
	rightPicker,
} from "../support/comparePage";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { players } from "../support/seedData";
import { allPairs, defaultPair, headToHead, playerId } from "../support/standings";

const namesInOrder = [...players].sort((a, b) => a.localeCompare(b));
const PLAYERS_ROUTE = /\/api\/player$/;
const { one: busiest, two: rival } = defaultPair();
const [firstByName, secondByName] = namesInOrder;
const strangers = allPairs.find(([one, two]) => headToHead(one, two).together.length === 0) ?? allPairs[0];

const holdComparisons = async (page: Page) => {
	let release = () => {};
	const gate = new Promise<void>((resolve) => {
		release = resolve;
	});
	await page.route("**/api/compare/*/*", async (route) => {
		await gate;
		await route.continue();
	});
	return release;
};

const seededPlayers = players.map((name) => ({ id: playerId(name), name, image: null }));

const withPlayers = (page: Page, list: unknown[]) =>
	page.route(PLAYERS_ROUTE, (route) => route.fulfill({ json: list }));

test("opens on the pair with the most sessions together", async ({ page }) => {
	await page.goto("/compare");

	await expectPair(page, busiest, rival);
	await expect(page.getByRole("heading", { name: "Player vs player", level: 1 })).toBeVisible();
	await expectComparison(page, busiest, rival);
});

test("offers every other player in the left picker", async ({ page }) => {
	await openCompare(page, busiest, rival);

	await leftPicker(page, busiest).click();

	await expect(page.getByRole("option")).toHaveText(namesInOrder.filter((name) => name !== rival));
	await expect(page.getByRole("option").filter({ hasText: busiest })).toHaveAttribute("aria-selected", "true");
	await expect(page.getByPlaceholder("Search...")).toHaveCount(0);
});

test("offers every other player in the right picker", async ({ page }) => {
	await openCompare(page, busiest, rival);

	await rightPicker(page, rival).click();

	await expect(page.getByRole("option")).toHaveText(namesInOrder.filter((name) => name !== busiest));
});

test("changes the left player and keeps the right one", async ({ page }) => {
	const [, , third] = players;
	await openCompare(page, busiest, rival);

	await choosePlayer(page, leftPicker(page, busiest), third);

	await expectPair(page, third, rival);
	await expectComparison(page, third, rival);
});

test("changes the right player and keeps the left one", async ({ page }) => {
	const [, , third] = players;
	await openCompare(page, busiest, rival);

	await choosePlayer(page, rightPicker(page, rival), third);

	await expectPair(page, busiest, third);
	await expectComparison(page, busiest, third);
});

test("swapping mirrors the whole comparison", async ({ page }) => {
	await openCompare(page, busiest, rival);

	await page.getByRole("button", { name: "Swap players" }).click();

	await expectPair(page, rival, busiest);
	await expectComparison(page, rival, busiest);
});

test("swapping twice brings the first order back", async ({ page }) => {
	const swap = page.getByRole("button", { name: "Swap players" });
	await openCompare(page, busiest, rival);

	await swap.click();
	await expectPair(page, rival, busiest);
	await swap.click();

	await expectPair(page, busiest, rival);
	await expectComparison(page, busiest, rival);
});

test("keeps the chosen pair after a reload", async ({ page }) => {
	const [, , , fourth] = players;
	await openCompare(page, busiest, rival);
	await choosePlayer(page, rightPicker(page, rival), fourth);
	await expectPair(page, busiest, fourth);

	await page.reload();

	await expectPair(page, busiest, fourth);
	await expectComparison(page, busiest, fourth);
});

test("keeps the left player and picks another one when the same player is asked twice", async ({ page }) => {
	await page.goto(`/compare?left=${playerId(rival)}&right=${playerId(rival)}`);

	await expectPair(page, rival, busiest);
	await expectComparison(page, rival, busiest);
});

test("replaces an unknown left player", async ({ page }) => {
	await page.goto(`/compare?left=9999&right=${playerId(busiest)}`);

	await expectPair(page, rival, busiest);
});

test("replaces an unknown right player", async ({ page }) => {
	await page.goto(`/compare?left=${playerId(rival)}&right=9999`);

	await expectPair(page, rival, busiest);
});

test("falls back to the default pair when both players are unknown", async ({ page }) => {
	await page.goto("/compare?left=9998&right=9999");

	await expectPair(page, busiest, rival);
});

test("ignores parameters that are not a number", async ({ page }) => {
	await page.goto("/compare?left=kermit&right=%7B%7D");

	await expectPair(page, busiest, rival);
});

test("completes a link that only names the left player", async ({ page }) => {
	const [, , , fourth] = players;
	await page.goto(`/compare?left=${playerId(fourth)}`);

	await expectPair(page, fourth, busiest);
});

test("completes a link that only names the right player", async ({ page }) => {
	const [, , , fourth] = players;
	await page.goto(`/compare?right=${playerId(fourth)}`);

	await expectPair(page, busiest, fourth);
});

test("sorts the games on the biggest difference and back", async ({ page }) => {
	const stats = headToHead(busiest, rival);
	await openCompare(page, busiest, rival);
	const mostPlayed = page.getByRole("button", { name: "Most played" });
	const biggestLead = page.getByRole("button", { name: "Biggest difference" });

	await expect(mostPlayed).toHaveAttribute("aria-pressed", "true");
	await biggestLead.click();

	await expect(biggestLead).toHaveAttribute("aria-pressed", "true");
	await expect(mostPlayed).toHaveAttribute("aria-pressed", "false");
	await expectGameRows(page, busiest, rival, stats.biggestLead.slice(0, 5));

	await mostPlayed.click();

	await expectGameRows(page, busiest, rival, stats.perGame.slice(0, 5));
});

test("shows all games and folds them again in the chosen order", async ({ page }) => {
	const stats = headToHead(busiest, rival);
	await openCompare(page, busiest, rival);

	await page.getByRole("button", { name: "Biggest difference" }).click();
	await page.getByRole("button", { name: `Show all ${stats.perGame.length} games` }).click();

	await expectGameRows(page, busiest, rival, stats.biggestLead);

	await page.getByRole("button", { name: "Show less" }).click();

	await expectGameRows(page, busiest, rival, stats.biggestLead.slice(0, 5));
	await expect(page.getByRole("button", { name: `Show all ${stats.perGame.length} games` })).toBeVisible();
});

test("opens the most played game", async ({ page }) => {
	const [game] = headToHead(busiest, rival).perGame;
	await openCompare(page, busiest, rival);

	await page.getByRole("link", { name: /^Most played/ }).click();

	await expect(page).toHaveURL(`/games/${game.id}`);
	await expect(page.getByRole("heading", { name: game.title, level: 1 })).toBeVisible();
});

test("opens a recent session and comes back to the same pair", async ({ page }) => {
	const [newest] = recentRows(rival, busiest);
	await openCompare(page, rival, busiest);

	await page.getByRole("region", { name: "Recent sessions together" }).getByRole("link").first().click();

	await expect(page).toHaveURL(newest.href);
	await expect(page.getByRole("heading", { name: "Update session" })).toBeVisible();

	await page.goBack();

	await expectPair(page, rival, busiest);
});

test("lets a reader read the recent sessions without opening them", async ({ readerPage }) => {
	const rows = recentRows(busiest, rival);
	await openCompare(readerPage, busiest, rival);
	const region = readerPage.getByRole("region", { name: "Recent sessions together" });

	await expect(region.getByRole("listitem")).toHaveCount(rows.length);
	await expect(region.getByRole("link")).toHaveCount(0);
	await expect(region.getByRole("listitem").first()).toContainText(
		displayDate(headToHead(busiest, rival).recent[0].start),
	);
	await expect(readerPage.getByTestId("record-playerOne")).toHaveText(String(headToHead(busiest, rival).oneWins));
});

test("lets a user with write access open the recent sessions", async ({ writerPage }) => {
	const rows = recentRows(busiest, rival);
	await openCompare(writerPage, busiest, rival);

	await expect(writerPage.getByRole("region", { name: "Recent sessions together" }).getByRole("link")).toHaveCount(
		rows.length,
	);
});

test("is reachable from the menu", async ({ page }) => {
	await page.goto("/");

	await page.getByRole("link", { name: "Player vs player" }).click();

	await expectPair(page, busiest, rival);
});

test("opens from the compare button of a player", async ({ page }) => {
	const [, , third] = players;
	await page.goto(`/players/${playerId(third)}`);

	await page.getByRole("main").getByRole("link", { name: "Compare", exact: true }).click();

	await expectPair(page, third, busiest);
});

test("opens from a rival on the player page with both players chosen", async ({ page }) => {
	await page.goto(`/players/${playerId(busiest)}`);

	await page
		.getByRole("main")
		.getByRole("link", { name: new RegExp(`^${rival} .*sessions`) })
		.click();

	await expectPair(page, busiest, rival);
});

test("keeps the old numbers dimmed while another player loads", async ({ page }) => {
	const oldWins = String(headToHead(busiest, rival).oneWins);
	const third =
		players.find((name) => name !== busiest && name !== rival && String(headToHead(name, rival).oneWins) !== oldWins) ??
		"";
	await openCompare(page, busiest, rival);
	const release = await holdComparisons(page);

	await choosePlayer(page, leftPicker(page, busiest), third);

	await expect(leftPicker(page, third)).toBeVisible();
	await expect(page.getByRole("status", { name: "Updating the comparison" })).toBeAttached();
	await expect(page.getByTestId("compare-content")).toHaveAttribute("aria-busy", "true");
	await expect(page.getByTestId("record-playerOne")).toHaveText(oldWins);

	release();

	await expectPair(page, third, rival);
	await expect(page.getByRole("status", { name: "Updating the comparison" })).toHaveCount(0);
	await expectComparison(page, third, rival);
});

test("mirrors the numbers at once when swapping, before the server answers", async ({ page }) => {
	const stats = headToHead(busiest, rival);
	await openCompare(page, busiest, rival);
	const release = await holdComparisons(page);

	await page.getByRole("button", { name: "Swap players" }).click();

	await expect(leftPicker(page, rival)).toBeVisible();
	await expect(page.getByTestId("record-playerOne")).toHaveText(String(stats.twoWins));
	await expect(page.getByTestId("record-playerTwo")).toHaveText(String(stats.oneWins));
	await expect(page.getByTestId("compare-content")).toHaveAttribute("aria-busy", "false");

	release();

	await expectComparison(page, rival, busiest);
});

test("adds a search box to the pickers from nine players on", async ({ page }) => {
	await withPlayers(page, [...seededPlayers, { id: 9999, name: "Sam Eagle", image: null }]);
	await openCompare(page, busiest, rival);

	await leftPicker(page, busiest).click();
	await page.getByPlaceholder("Search...").fill("sam");

	await expect(page.getByRole("option")).toHaveText([new RegExp(`${busiest}$`), /Sam Eagle$/]);
});

test("compares the first players when nobody played together", async ({ page }) => {
	await page.route("**/api/compare/default-pair", (route) => route.fulfill({ status: 204 }));

	await page.goto("/compare");

	await expectPair(page, firstByName, secondByName);
});

test("still opens when the default pair cannot be loaded", async ({ page }) => {
	await page.route("**/api/compare/default-pair", (route) => route.abort());

	await page.goto("/compare");

	await expectPair(page, firstByName, secondByName);
	await expectComparison(page, firstByName, secondByName);
});

test("says so when two players never played together", async ({ page }) => {
	const [one, two] = strangers;
	await openCompare(page, one, two);

	await expectComparison(page, one, two);
	await expect(page.getByRole("region", { name: "All sessions, not just together" })).toBeVisible();
});

test.describe("too few players", () => {
	for (const count of [0, 1]) {
		test(`explains that ${count} players cannot be compared`, async ({ page }) => {
			await withPlayers(page, seededPlayers.slice(0, count));

			await page.goto("/compare");

			await expect(page.getByRole("heading", { name: "Player vs player", level: 1 })).toBeVisible();
			await expect(page.getByText("Not enough players to compare")).toBeVisible();
			await expect(page.getByRole("combobox")).toHaveCount(0);

			await page.getByRole("button", { name: "Go to players page" }).click();

			await expect(page).toHaveURL("/players");
		});
	}

	test("does not offer a reader a way to add players", async ({ readerPage }) => {
		await withPlayers(readerPage, seededPlayers.slice(0, 1));

		await readerPage.goto("/compare");

		await expect(readerPage.getByText("Not enough players to compare")).toBeVisible();
		await expect(readerPage.getByRole("button", { name: "Go to players page" })).toHaveCount(0);
	});
});

test("explains when the comparison cannot be loaded", async ({ page }) => {
	await page.route("**/api/compare/*/*", (route) => route.abort());

	await page.goto(compareUrl(busiest, rival));

	await expect(page.getByText("Couldn't load this page")).toBeVisible({ timeout: 3000 });
});

test("explains when the players cannot be loaded", async ({ page }) => {
	await page.route(PLAYERS_ROUTE, (route) => route.abort());

	await page.goto("/compare");

	await expect(page.getByText("Couldn't load this page")).toBeVisible({ timeout: 3000 });
});
