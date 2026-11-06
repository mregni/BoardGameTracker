import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { expectGames, gameLink, gameLinks, inTitleOrder, openGames, pick } from "../support/gamesPage";
import { expected, games, loans, mostPlayedGame, neverPlayedGame, sessions, wantedGame } from "../support/seedData";

const playsOf = (title: string) => sessions.filter((session) => session.game === title).length;

const WHEEL_SIZE = 10;

const titlesInOrder = inTitleOrder(games.map((game) => game.title));

test.describe("list", () => {
	test.beforeEach(async ({ page }) => {
		await openGames(page);
	});

	test("shows every game sorted by title with its plays", async ({ page }) => {
		await expectGames(page, titlesInOrder);
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

	for (const [label, state] of [
		["Owned", "owned"],
		["Wanted", "wanted"],
		["For trade", "forTrade"],
		["Previously owned", "previouslyOwned"],
		["Sold", "sold"],
	] as const) {
		test(`filters by the state ${label}`, async ({ page }) => {
			await pick(page, "State", label);

			await expectGames(page, inTitleOrder(games.filter((game) => game.state === state).map((game) => game.title)));
		});
	}

	test("sorts by most played", async ({ page }) => {
		await pick(page, "Sort by", "Most played");

		await expectGames(
			page,
			[...games]
				.sort((a, b) => playsOf(b.title) - playsOf(a.title) || a.title.localeCompare(b.title))
				.map((game) => game.title),
		);
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
	await expect(page).toHaveURL(/wheel=/);
	await expect(page.getByRole("main")).toContainText("2 players");
	const wheel: number[] = JSON.parse(new URL(page.url()).searchParams.get("wheel") ?? "[]");
	const onLoan = loans.filter((loan) => !loan.returned).map((loan) => loan.game);
	const fitting = games
		.filter((game) => game.state === "owned" && game.minPlayers <= 2 && game.maxPlayers >= 2)
		.filter((game) => !onLoan.includes(game.title))
		.map((game) => game.title);
	expect(wheel).toHaveLength(WHEEL_SIZE);
	expect(fitting).toEqual(expect.arrayContaining(wheel.map((id) => games[id - 1].title)));
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
	await expect(page.getByRole("main").getByText("Owned", { exact: true })).toBeVisible();
});
