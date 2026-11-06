import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { pick } from "../support/gamesPage";
import { expected } from "../support/seedData";
import { inUnplayedOrder, nextNight, shelf, shelfRow, shelfRows } from "../support/shelf";

type Json = Record<string, unknown>;
type Row = Json & { id: number; title: string; price: number | null; plannedGameNightId: number | null };

const patchJson = (page: Page, url: string | RegExp, patch: (body: never) => unknown) =>
	page.route(url, async (route) => {
		const response = await route.fetch();
		if (!response.ok()) {
			await route.fulfill({ response });
			return;
		}
		await route.fulfill({ response, json: patch((await response.json()) as never) });
	});

const patchStatistics = (page: Page, patch: (statistics: Json) => Json) =>
	patchJson(page, /\/api\/game\/shames\/statistics/, (statistics: Json) => ({ ...statistics, ...patch(statistics) }));

const patchShames = (page: Page, patch: (rows: Row[]) => Row[]) => patchJson(page, /\/api\/game\/shames(\?|$)/, patch);

const patchSettings = (page: Page, patch: Json) =>
	patchJson(page, "**/api/settings", (settings: Json) => ({ ...settings, ...patch }));

const open = async (page: Page) => {
	await page.goto("/shames");
	await expect(page.getByRole("heading", { name: "Shelf of Shame", level: 1 })).toBeVisible();
};

const withoutPrice = (rows: Row[], titles: string[]) =>
	rows.map((row) => (titles.includes(row.title) ? { ...row, price: null } : row));

const [first, second] = inUnplayedOrder(shelf);
const dearest = [...shelf].sort((a, b) => (b.buyingPrice ?? 0) - (a.buyingPrice ?? 0));

test("names the one game without a price", async ({ page }) => {
	await patchShames(page, (rows) => withoutPrice(rows, [first.title]));
	await open(page);

	await expect(page.getByRole("region", { name: "Money on the shelf" })).toContainText(
		`${first.title} has no price, so it isn't in the money.`,
	);
	await expect(shelfRow(page, first.title)).toContainText("No price");
});

test("counts the games without a price and sorts them last", async ({ page }) => {
	await patchShames(page, (rows) => withoutPrice(rows, [dearest[0].title, dearest[1].title]));
	await open(page);

	await expect(page.getByRole("region", { name: "Money on the shelf" })).toContainText(
		"2 games on the shelf have no price, so they aren't in the money.",
	);

	await page.getByRole("button", { name: "Sort by price paid" }).click();

	await expect(shelfRows(page).first()).toContainText(dearest[2].title);
	await expect(shelfRows(page).nth(shelf.length - 2)).toContainText(
		[dearest[0].title, dearest[1].title].sort((a, b) => a.localeCompare(b))[0],
	);
});

test("asks to add prices when no game has one", async ({ page }) => {
	await patchStatistics(page, () => ({ totalValue: null, collectionValue: 0, neverPlayedValue: 0 }));
	await open(page);

	await expect(
		page.getByText("Add what you paid for your games to see how much money sits on the shelf."),
	).toBeVisible();
	await expect(page.getByRole("region", { name: "Money on the shelf" })).toHaveCount(0);
});

test("does not ask for prices when only the shelf games lack one", async ({ page }) => {
	await patchStatistics(page, () => ({ totalValue: null, neverPlayedValue: 0 }));
	await patchShames(page, (rows) =>
		withoutPrice(
			rows,
			shelf.map((game) => game.title),
		),
	);
	await open(page);
	await expect(shelfRows(page)).toHaveCount(shelf.length);

	test.fail(true, "BUG: the shelf asks to add what you paid although the rest of the collection has prices");
	await expect(page.getByRole("main")).not.toContainText("Add what you paid", { timeout: 1_000 });
});

test("does not ask a reader to add prices", async ({ readerPage }) => {
	await patchStatistics(readerPage, () => ({ totalValue: null, collectionValue: 0, neverPlayedValue: 0 }));
	await open(readerPage);

	await expect(shelfRows(readerPage)).toHaveCount(shelf.length);
	await expect(readerPage.getByRole("main")).not.toContainText("Add what you paid");
});

test("invites you to plan a night when none is coming", async ({ page }) => {
	await patchStatistics(page, () => ({ nextGameNight: null, plannedCount: 0 }));
	await patchShames(page, (rows) => rows.map((row) => ({ ...row, plannedGameNightId: null, plannedFor: null })));
	await open(page);
	const main = page.getByRole("main");

	await expect(main.getByText("No game night coming up, so there is nothing to plan games for yet.")).toBeVisible();
	await expect(main.getByRole("link", { name: "Plan a game night" })).toHaveAttribute("href", "/game-nights");
	await expect(main.getByRole("button", { name: /^Plan / })).toHaveCount(0);
	await expect(main.getByRole("link", { name: /^Log a session of/ })).toHaveCount(shelf.length);
});

test("does not invite a reader to plan a night", async ({ readerPage }) => {
	await patchStatistics(readerPage, () => ({ nextGameNight: null, plannedCount: 0 }));
	await open(readerPage);

	await expect(shelfRows(readerPage)).toHaveCount(shelf.length);
	await expect(readerPage.getByRole("main")).not.toContainText("No game night coming up");
});

test("does not mention game nights when they are switched off", async ({ page }) => {
	await patchSettings(page, { gameNightsEnabled: false });
	await patchStatistics(page, () => ({ nextGameNight: null, plannedCount: 0 }));
	await patchShames(page, (rows) => rows.map((row) => ({ ...row, plannedGameNightId: null, plannedFor: null })));
	await open(page);
	const main = page.getByRole("main");

	await expect(shelfRows(page)).toHaveCount(shelf.length);
	await expect(main).not.toContainText("game night");
	await expect(main.getByRole("button", { name: /^Plan / })).toHaveCount(0);
});

test("asks to pick a game when none is planned yet", async ({ page }) => {
	await patchStatistics(page, () => ({ plannedCount: 0 }));
	await patchShames(page, (rows) => rows.map((row) => ({ ...row, plannedGameNightId: null, plannedFor: null })));
	await open(page);

	await expect(
		page.getByText(
			`Next game night: ${nextNight.title} on ${displayDate(nextNight.startDate)}. Pick a game from the shelf with Plan.`,
		),
	).toBeVisible();
});

test("does not ask a reader to pick a game", async ({ readerPage }) => {
	await patchStatistics(readerPage, () => ({ plannedCount: 0 }));
	await open(readerPage);

	await expect(readerPage.getByRole("main").getByText(/^Next game night/)).toBeVisible();
	await expect(readerPage.getByRole("main")).not.toContainText("Pick a game");
});

test("speaks of a single planned game in the singular", async ({ page }) => {
	await patchStatistics(page, () => ({ plannedCount: 1 }));
	await open(page);

	await expect(page.getByText("1 game is coming off the shelf.")).toBeVisible();
});

test("counts planned games across several nights", async ({ page }) => {
	const planned = shelf.filter((game) => game === first || game === second).length;
	await patchStatistics(page, () => ({ plannedCount: planned }));
	await patchShames(page, (rows) =>
		rows.map((row, index) => ({ ...row, plannedGameNightId: index < planned ? index + 1 : null })),
	);
	await open(page);

	await expect(
		page.getByText(`${planned} games are coming off the shelf across ${planned} game nights.`),
	).toBeVisible();
});

test("explains an empty collection", async ({ page }) => {
	await patchStatistics(page, () => ({
		count: 0,
		ownedCount: 0,
		totalValue: null,
		collectionValue: 0,
		neverPlayedCount: 0,
		neverPlayedValue: 0,
		plannedCount: 0,
	}));
	await patchShames(page, () => []);
	await open(page);
	const main = page.getByRole("main");

	await expect(main.getByText("Owned games not played in 6+ months")).toBeVisible();
	await expect(main.getByRole("combobox", { name: "Not played in" })).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Nothing on the shelf", level: 2 })).toBeVisible();
});

test("does not repeat an empty never played count in the summary", async ({ page }) => {
	await patchStatistics(page, (statistics) => ({ ...statistics, neverPlayedCount: 0 }));
	await patchShames(page, (rows) =>
		rows.map((row) => ({ ...row, lastSessionDate: new Date(2020, 0, 1).toISOString(), playCount: 1 })),
	);
	await open(page);

	await expect(
		page.getByText(`${shelf.length} of ${expected.ownedCount} owned games on the shelf`, { exact: true }),
	).toBeVisible();
});

test("leaves the shelf when it is switched off", async ({ page }) => {
	await patchSettings(page, { shelfOfShameEnabled: false });

	await page.goto("/shames");

	await expect(page).toHaveURL("/");
	await expect(
		page
			.getByRole("navigation")
			.getByRole("link", { name: /^Players/ })
			.first(),
	).toBeVisible();
	await expect(page.getByRole("navigation").getByRole("link", { name: /Shelf of Shame/ })).toHaveCount(0);
});

test("keeps the filter choice when the sort changes", async ({ page }) => {
	await page.goto("/shames?show=never");

	await pick(page, "Sort", "Title");

	await expect(page).toHaveURL("/shames?show=never&sort=title");
});
