import { expect, test } from "../support/fixtures";
import { expectGames, gameLinks, inTitleOrder, openGames, pick } from "../support/gamesPage";
import { games, sessions } from "../support/seedData";

type Game = (typeof games)[number];

const lastPlayed = (game: Game) =>
	Math.max(0, ...sessions.filter((session) => session.game === game.title).map((session) => session.start.getTime()));

const orderedBy = (value: (game: Game) => number) =>
	[...games].sort((a, b) => value(b) - value(a) || a.title.localeCompare(b.title)).map((game) => game.title);

test.describe("filters", () => {
	const titlesOf = (matches: (game: Game) => boolean) => inTitleOrder(games.filter(matches).map((game) => game.title));

	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	for (const { combobox, option, titles } of [
		...[1, 2, 3, 4, 5, 6, 7, 8].map((players) => ({
			combobox: "Players",
			option: players === 1 ? "1 player" : `${players} players`,
			titles: titlesOf((game) => game.minPlayers <= players && game.maxPlayers >= players),
		})),
		...[30, 60, 90, 120, 180].map((minutes) => ({
			combobox: "Play time",
			option: `≤ ${minutes} min`,
			titles: titlesOf((game) => game.maxPlayTime <= minutes),
		})),
		{ combobox: "Language", option: "English", titles: titlesOf((game) => game.language === "en") },
		{ combobox: "Language", option: "Dutch", titles: titlesOf((game) => game.language === "nl") },
		{ combobox: "Language", option: "Not set", titles: titlesOf((game) => game.language === null) },
		{ combobox: "Weight", option: "Light (< 2)", titles: titlesOf((game) => game.weight < 2) },
		{ combobox: "Weight", option: "Medium (2 - 3)", titles: titlesOf((game) => game.weight >= 2 && game.weight <= 3) },
		{ combobox: "Weight", option: "Heavy (> 3)", titles: titlesOf((game) => game.weight > 3) },
		{ combobox: "Age", option: "≤ 6", titles: titlesOf(({ minAge }) => minAge !== null && minAge <= 6) },
		{
			combobox: "Age",
			option: "7 - 9",
			titles: titlesOf(({ minAge }) => minAge !== null && minAge >= 7 && minAge <= 9),
		},
		{
			combobox: "Age",
			option: "10 - 12",
			titles: titlesOf(({ minAge }) => minAge !== null && minAge >= 10 && minAge <= 12),
		},
		{ combobox: "Age", option: "13+", titles: titlesOf(({ minAge }) => minAge !== null && minAge >= 13) },
	]) {
		test(`filters on ${combobox} ${option}`, async ({ page }) => {
			await pick(page, combobox, option);

			await expectGames(page, titles);
			await expect(page.getByRole("button", { name: `Remove filter ${combobox}: ${option}` })).toBeVisible();
		});
	}

	test("combines filters and removes them one at a time", async ({ page }) => {
		const mediumAndShort = titlesOf((game) => game.weight >= 2 && game.weight <= 3 && game.maxPlayTime <= 60);
		const short = titlesOf((game) => game.maxPlayTime <= 60);

		await pick(page, "Weight", "Medium (2 - 3)");
		await pick(page, "Play time", "≤ 60 min");
		await expectGames(page, mediumAndShort);

		await page.getByRole("button", { name: "Remove filter Weight: Medium (2 - 3)" }).click();

		await expectGames(page, short);
	});

	test("explains when nothing matches and clears the filters", async ({ page }) => {
		await page.getByRole("textbox", { name: "Filter by name" }).fill("no such game");

		await expect(page.getByText("No games match the current filters.")).toBeVisible();
		await expect(gameLinks(page)).toHaveCount(0);

		await page.getByRole("button", { name: "Clear filters" }).click();

		await expectGames(
			page,
			titlesOf(() => true),
		);
		await expect(page.getByRole("textbox", { name: "Filter by name" })).toHaveValue("");
	});

	test("keeps a filter in the address so a reload still applies it", async ({ page }) => {
		await pick(page, "State", "Wanted");
		await expect(page).toHaveURL(/state=wanted/);

		await page.reload();

		await expectGames(
			page,
			titlesOf((game) => game.state === "wanted"),
		);
	});
});

test.describe("sorting", () => {
	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	for (const [option, titles] of [
		["Recently played", orderedBy(lastPlayed)],
		["Recently added", orderedBy((game) => new Date(game.additionDate).getTime())],
		["Rating", orderedBy((game) => game.rating)],
		["Weight", orderedBy((game) => game.weight)],
	] as const) {
		test(`sorts by ${option}`, async ({ page }) => {
			await pick(page, "Sort by", option);

			await expectGames(page, [...titles]);
		});
	}
});
