import { expect, test } from "../support/fixtures";
import { gameLinks, openGames, pick } from "../support/gamesPage";
import { expected, games, mostPlayedGame } from "../support/seedData";

const highest = (value: (game: (typeof games)[number]) => number) =>
	games.reduce((best, game) => (value(game) > value(best) ? game : best), games[0]).title;

test.describe("filters", () => {
	const countOf = (matches: (game: (typeof games)[number]) => boolean) => games.filter(matches).length;

	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	for (const { combobox, option, count } of [
		...[1, 2, 3, 4, 5, 6, 7, 8].map((players) => ({
			combobox: "Players",
			option: players === 1 ? "1 player" : `${players} players`,
			count: countOf((game) => game.minPlayers <= players && game.maxPlayers >= players),
		})),
		...[30, 60, 90, 120, 180].map((minutes) => ({
			combobox: "Play time",
			option: `≤ ${minutes} min`,
			count: countOf((game) => game.maxPlayTime <= minutes),
		})),
		{ combobox: "Language", option: "English", count: countOf((game) => game.language === "en") },
		{ combobox: "Language", option: "Dutch", count: countOf((game) => game.language === "nl") },
		{ combobox: "Language", option: "Not set", count: countOf((game) => game.language === null) },
		{ combobox: "Weight", option: "Light (< 2)", count: countOf((game) => game.weight < 2) },
		{ combobox: "Weight", option: "Medium (2 - 3)", count: countOf((game) => game.weight >= 2 && game.weight <= 3) },
		{ combobox: "Weight", option: "Heavy (> 3)", count: countOf((game) => game.weight > 3) },
		{ combobox: "Age", option: "7 - 9", count: countOf(({ minAge }) => minAge !== null && minAge >= 7 && minAge <= 9) },
		{
			combobox: "Age",
			option: "10 - 12",
			count: countOf(({ minAge }) => minAge !== null && minAge >= 10 && minAge <= 12),
		},
		{ combobox: "Age", option: "13+", count: countOf(({ minAge }) => minAge !== null && minAge >= 13) },
	]) {
		test(`filters on ${combobox} ${option}`, async ({ page }) => {
			await pick(page, combobox, option);

			await expect(gameLinks(page)).toHaveCount(count);
			await expect(page.getByRole("button", { name: `Remove filter ${combobox}: ${option}` })).toBeVisible();
		});
	}

	test("combines filters and removes them one at a time", async ({ page }) => {
		const mediumAndShort = countOf((game) => game.weight >= 2 && game.weight <= 3 && game.maxPlayTime <= 60);
		const short = countOf((game) => game.maxPlayTime <= 60);

		await pick(page, "Weight", "Medium (2 - 3)");
		await pick(page, "Play time", "≤ 60 min");
		await expect(gameLinks(page)).toHaveCount(mediumAndShort);

		await page.getByRole("button", { name: "Remove filter Weight: Medium (2 - 3)" }).click();

		await expect(gameLinks(page)).toHaveCount(short);
	});

	test("explains when nothing matches and clears the filters", async ({ page }) => {
		await page.getByRole("textbox", { name: "Filter by name" }).fill("no such game");

		await expect(page.getByText("No games match the current filters.")).toBeVisible();
		await expect(gameLinks(page)).toHaveCount(0);

		await page.getByRole("button", { name: "Clear filters" }).click();

		await expect(gameLinks(page)).toHaveCount(expected.gameCount);
		await expect(page.getByRole("textbox", { name: "Filter by name" })).toHaveValue("");
	});

	test("keeps a filter in the address so a reload still applies it", async ({ page }) => {
		await pick(page, "State", "Wanted");
		await expect(page).toHaveURL(/state=wanted/);

		await page.reload();

		await expect(gameLinks(page)).toHaveCount(expected.wantedCount);
	});
});

test.describe("sorting", () => {
	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	for (const [option, first] of [
		["Recently played", mostPlayedGame.title],
		["Recently added", games[games.length - 1].title],
		["Rating", highest((game) => game.rating)],
		["Weight", highest((game) => game.weight)],
	]) {
		test(`sorts by ${option}`, async ({ page }) => {
			await pick(page, "Sort by", option);

			await expect(gameLinks(page).first()).toContainText(first);
		});
	}
});
