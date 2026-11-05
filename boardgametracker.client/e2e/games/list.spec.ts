import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { gameLink, gameLinks, openGames, pick } from "../support/gamesPage";
import {
	expected,
	games,
	mostPlayedGame,
	neverPlayedGame,
	secondMostPlayedGame,
	sessions,
	wantedGame,
} from "../support/seedData";

const playsOf = (title: string) => sessions.filter((session) => session.game === title).length;

const titlesInOrder = games.map((game) => game.title).sort((a, b) => a.localeCompare(b));

test.describe("list", () => {
	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	test("shows every game sorted by title with its plays", async ({ page }) => {
		await expect(gameLinks(page)).toContainText(titlesInOrder);
		await expect(gameLink(page, mostPlayedGame.title)).toContainText(`${playsOf(mostPlayedGame.title)} plays`);
		await expect(gameLink(page, neverPlayedGame.title)).toContainText("Never played");
		await expect(gameLink(page, wantedGame.title)).toContainText("Wanted");
		await expectVisibleImagesToLoad(page);
	});

	test("filters by name", async ({ page }) => {
		await page.getByRole("textbox", { name: "Filter by name" }).fill("redw");

		await expect(page.getByText("1 game", { exact: true })).toBeVisible();
		await expect(gameLinks(page)).toContainText(["Redwood"]);
	});

	for (const [state, count] of [
		["Owned", expected.ownedCount],
		["Wanted", expected.wantedCount],
		["For trade", expected.forTradeCount],
		["Previously owned", expected.previouslyOwnedCount],
		["Sold", expected.soldCount],
	] as const) {
		test(`filters by the state ${state}`, async ({ page }) => {
			await pick(page, "State", state);

			await expect(gameLinks(page)).toHaveCount(count);
		});
	}

	test("sorts by most played", async ({ page }) => {
		await pick(page, "Sort by", "Most played");

		await expect(gameLinks(page).first()).toContainText(mostPlayedGame.title);
		await expect(gameLinks(page).nth(1)).toContainText(secondMostPlayedGame.title);
		await expect(gameLinks(page).last()).toContainText("Never played");
	});

	test("shows the totals in the table view", async ({ page }) => {
		await page.getByRole("button", { name: "Table" }).click();

		await expect(page.getByRole("table").getByRole("row")).toHaveCount(expected.gameCount + 1);
		await expect(page.getByText(`Total price (owned): €${expected.ownedValue.toLocaleString("en-US")}`)).toBeVisible();
		await expect(page.getByRole("row", { name: new RegExp(mostPlayedGame.title) })).toContainText(
			`€${mostPlayedGame.buyingPrice}`,
		);
	});

	test("opens the detail page of a game", async ({ page }) => {
		await gameLink(page, mostPlayedGame.title).click();

		await expect(page).toHaveURL("/games/1");
		await expect(page.getByRole("heading", { name: mostPlayedGame.title, level: 1 })).toBeVisible();
	});
});

test("remembers the table view in the address", async ({ page }) => {
	await page.goto("/games");
	await page.getByRole("button", { name: "Table" }).click();
	await expect(page).toHaveURL(/view=table/);

	await page.reload();

	await expect(page.getByRole("table")).toBeVisible();
	await expect(page.getByRole("button", { name: "Table" })).toHaveAttribute("aria-pressed", "true");
});

test("hands the filters to the wheel", async ({ page }) => {
	await openGames(page);
	await pick(page, "Players", "2 players");
	await page.getByRole("button", { name: "Pick for us" }).click();

	await expect(page).toHaveURL(/\/games\/pick.*players=2/);
	await expect(page.getByRole("main")).toContainText("2 players");
});

test("spins the wheel and opens the picked game", async ({ page }) => {
	await page.goto("/games");
	await page.getByRole("button", { name: "Pick for us" }).click();

	await expect(page).toHaveURL(/\/games\/pick/);
	await expect(page.getByRole("heading", { name: "What do we play?" })).toBeVisible();

	await page.getByRole("button", { name: "Spin the wheel" }).click();
	await page
		.getByRole("button", { name: "Open game" })
		.or(page.getByRole("link", { name: "Open game" }))
		.click({ timeout: 20_000 });

	await expect(page).toHaveURL(/\/games\/\d+$/);
});
