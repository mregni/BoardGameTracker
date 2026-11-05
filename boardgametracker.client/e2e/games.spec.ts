import type { Page } from "@playwright/test";
import { expect, expectVisibleImagesToLoad, test } from "./support/fixtures";
import { expansions, expected, games, sessions, standingsOf } from "./support/seedData";

const gameLinks = (page: Page) => page.getByRole("main").getByRole("link");

const pick = async (page: Page, combobox: string, option: string) => {
	await page.getByRole("combobox", { name: combobox, exact: true }).click();
	await page.getByRole("option", { name: option, exact: true }).click();
};

const openGames = async (page: Page) => {
	await page.goto("/games");
	await expect(page.getByText(`${expected.gameCount} games`)).toBeVisible();
};

const titlesInOrder = games.map((game) => game.title).sort((a, b) => a.localeCompare(b));

test.describe("list", () => {
	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	test("shows every game sorted by title with its plays", async ({ page }) => {
		await expect(gameLinks(page)).toContainText(titlesInOrder);
		await expect(page.getByRole("link", { name: /^Azul / })).toContainText("15 plays");
		await expect(page.getByRole("link", { name: /^Agricola / })).toContainText("Never played");
		await expect(page.getByRole("link", { name: /^Ark Nova / })).toContainText("Wanted");
		await expectVisibleImagesToLoad(page);
	});

	test("filters by name", async ({ page }) => {
		await page.getByRole("textbox", { name: "Filter by name" }).fill("wing");

		await expect(page.getByText("1 game", { exact: true })).toBeVisible();
		await expect(gameLinks(page)).toContainText(["Wingspan"]);
	});

	for (const [state, count] of [
		["Owned", expected.ownedCount],
		["Wanted", expected.wantedCount],
		["For trade", expected.forTradeCount],
		["Previously owned", expected.previouslyOwnedCount],
	] as const) {
		test(`filters by the state ${state}`, async ({ page }) => {
			await pick(page, "State", state);

			await expect(gameLinks(page)).toHaveCount(count);
		});
	}

	test("sorts by most played", async ({ page }) => {
		await pick(page, "Sort by", "Most played");

		await expect(gameLinks(page).first()).toContainText("Azul");
		await expect(gameLinks(page).nth(1)).toContainText("Carcassonne");
		await expect(gameLinks(page).last()).toContainText("Never played");
	});

	test("shows the totals in the table view", async ({ page }) => {
		await page.getByRole("button", { name: "Table" }).click();

		await expect(page.getByRole("table").getByRole("row")).toHaveCount(expected.gameCount + 1);
		await expect(page.getByText(`Total price (owned): €${expected.ownedValue.toLocaleString("en-US")}`)).toBeVisible();
		await expect(page.getByRole("row", { name: /Azul/ })).toContainText("€20");
	});

	test("opens the detail page of a game", async ({ page }) => {
		await page.getByRole("link", { name: /^Azul / }).click();

		await expect(page).toHaveURL("/games/1");
		await expect(page.getByRole("heading", { name: "Azul", level: 1 })).toBeVisible();
	});
});

test.describe("filters", () => {
	const countOf = (matches: (game: (typeof games)[number]) => boolean) => games.filter(matches).length;

	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	for (const { combobox, option, count } of [
		{ combobox: "Players", option: "6 players", count: countOf((game) => game.maxPlayers >= 6) },
		{ combobox: "Play time", option: "≤ 60 min", count: countOf((game) => game.maxPlayTime <= 60) },
		{ combobox: "Weight", option: "Light (< 2)", count: countOf((game) => game.weight < 2) },
		{ combobox: "Weight", option: "Medium (2 - 3)", count: countOf((game) => game.weight >= 2 && game.weight <= 3) },
		{ combobox: "Weight", option: "Heavy (> 3)", count: countOf((game) => game.weight > 3) },
		{ combobox: "Age", option: "7 - 9", count: countOf((game) => game.minAge <= 9) },
		{ combobox: "Age", option: "10 - 12", count: countOf((game) => game.minAge >= 10) },
	]) {
		test(`filters on ${combobox} ${option}`, async ({ page }) => {
			await pick(page, combobox, option);

			await expect(gameLinks(page)).toHaveCount(count);
			await expect(page.getByRole("button", { name: `Remove filter ${combobox}: ${option}` })).toBeVisible();
		});
	}

	test("combines filters and removes them one at a time", async ({ page }) => {
		const heavyAndShort = countOf((game) => game.weight > 3 && game.maxPlayTime <= 60);
		const short = countOf((game) => game.maxPlayTime <= 60);

		await pick(page, "Weight", "Heavy (> 3)");
		await pick(page, "Play time", "≤ 60 min");
		await expect(gameLinks(page)).toHaveCount(heavyAndShort);

		await page.getByRole("button", { name: "Remove filter Weight: Heavy (> 3)" }).click();

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
		["Recently played", "Azul"],
		["Recently added", "Santorini"],
		["Rating", "Santorini"],
		["Weight", "Santorini"],
	]) {
		test(`sorts by ${option}`, async ({ page }) => {
			await pick(page, "Sort by", option);

			await expect(gameLinks(page).first()).toContainText(first);
		});
	}
});

test("remembers the table view in the address", async ({ page }) => {
	await page.goto("/games");
	await page.getByRole("button", { name: "Table" }).click();
	await expect(page).toHaveURL(/view=table/);

	await page.reload();

	await expect(page.getByRole("table")).toBeVisible();
	await expect(page.getByRole("button", { name: "Table" })).toHaveAttribute("aria-pressed", "true");
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

test.describe("detail of a played game", () => {
	const azul = games[0];
	const played = sessions.filter((session) => session.game === azul.title);
	const scores = played.flatMap((session) =>
		session.scores.map((score) => ({ ...score, won: score.player === session.winner })),
	);
	const values = scores.map((score) => score.score);
	const winning = scores.filter((score) => score.won).map((score) => score.score);
	const losing = scores.filter((score) => !score.won).map((score) => score.score);

	test.beforeEach(async ({ page }) => {
		await page.goto("/games/1");
		await expect(page.getByRole("heading", { name: azul.title, level: 1 })).toBeVisible();
	});

	test("shows the header with its actions", async ({ page }) => {
		const main = page.getByRole("main");
		const azulExpansions = expansions.filter((expansion) => expansion.game === azul.title);

		await expect(main.getByText("Owned", { exact: true })).toBeVisible();
		await expect(main.getByText(azul.description)).toBeVisible();
		await expect(main.getByRole("button", { name: "Log session" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Edit" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Delete" })).toBeVisible();
		await expect(main.getByRole("button", { name: "Manuals (0)" })).toBeVisible();
		await expect(main.getByRole("button", { name: `Expansions (${azulExpansions.length})` })).toBeVisible();
	});

	test("sums the plays in the statistics", async ({ page }) => {
		const hours = played.reduce((total, session) => total + session.minutes, 0) / 60;
		const region = page.getByRole("region", {
			name: new RegExp(`^Played ${played.length} times · ${hours}h in total`),
		});

		await expect(region).toContainText("Price / play");
		await expect(region).toContainText(`€${((azul.buyingPrice ?? 0) / played.length).toFixed(2)}`);
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

		await expect(side.getByRole("img", { name: azul.title }).first()).toBeVisible();
		await expect(side).toContainText(`Year published${azul.yearPublished}`);
		await expect(side).toContainText(`Players${azul.minPlayers}–${azul.maxPlayers}`);
		await expect(side).toContainText(`Duration${azul.minPlayTime}–${azul.maxPlayTime} min`);
		await expect(side).toContainText(`Minimum age${azul.minAge}+`);
		await expect(side).toContainText(`Rating${azul.rating} / 10`);
		await expect(side).toContainText(`Weight${azul.weight} / 5`);
		await expect(side).toContainText(`Cost€${azul.buyingPrice}`);
		await expect(side.getByRole("listitem").first()).toContainText(best.name);
		await expect(side.getByRole("listitem").first()).toContainText(`${best.sessions} sessions`);
		await expect(side.getByRole("listitem").first()).toContainText(`${best.wins} win`);
		await expectVisibleImagesToLoad(page);
	});
});

test("shows no statistics for a game that was never played", async ({ page }) => {
	await page.goto("/games");
	await page.getByRole("link", { name: /^Agricola / }).click();
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { name: "Agricola", level: 1 })).toBeVisible();
	await expect(main.getByRole("heading", { name: "Not on the table yet" })).toBeVisible();
	await expect(main.getByRole("button", { name: "Log the first play" })).toBeVisible();
	await expect(main.getByRole("heading", { name: "Recent sessions" })).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Scoring results" })).toHaveCount(0);
});

test("adds a game by hand, renames it and deletes it again", async ({ page }) => {
	const title = "Hand made game";
	const renamed = "Renamed game";
	await page.goto("/games");
	await page.getByRole("button", { name: "Add game" }).click();
	await page.getByRole("button", { name: /^Manual input/ }).click();
	await page.getByRole("textbox", { name: "Game title" }).fill(title);
	await page.getByRole("main").getByRole("button", { name: "Add game" }).click();

	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();

	await page.getByRole("button", { name: "Edit" }).click();
	await page.getByRole("textbox", { name: "Game title" }).fill(renamed);
	await page
		.getByRole("main")
		.getByRole("button", { name: /save|update/i })
		.click();

	await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();

	await page.getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(page).toHaveURL("/games");
	await expect(page.getByText(`${expected.gameCount} games`)).toBeVisible();
	await expect(page.getByRole("link", { name: renamed })).toHaveCount(0);
});
