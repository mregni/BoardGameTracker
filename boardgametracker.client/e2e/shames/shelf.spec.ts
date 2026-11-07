import type { Page } from "@playwright/test";
import { expect, expectVisibleImagesToLoad, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { pick } from "../support/gamesPage";
import { gameId } from "../support/playerStats";
import { expected } from "../support/seedData";
import {
	adminRowText,
	inOwnedOrder,
	inPriceOrder,
	inTitleOrder,
	inUnplayedOrder,
	isNeverPlayed,
	money,
	nextNight,
	nightOf,
	ownedLongerThan,
	priceOf,
	readerRowText,
	shelf,
	shelfOf,
	shelfRow,
	shelfRows,
	summaryOf,
} from "../support/shelf";

type Shelf = typeof shelf;

const open = async (page: Page, query = "") => {
	await page.goto(`/shames${query}`);
	await expect(page.getByRole("heading", { name: "Shelf of Shame", level: 1 })).toBeVisible();
};

const expectTitles = async (page: Page, games: Shelf) => {
	await expect(shelfRows(page).getByRole("link", { name: /^(?!Log a session)/ })).toHaveText(
		games.map((game) => game.title),
	);
};

const showButton = (page: Page, name: string, total: number) =>
	page.getByRole("group", { name: "Show" }).getByRole("button", { name: `${name} ${total}`, exact: true });

const listCount = (page: Page, count: number) =>
	page.getByText(`${count} of ${shelf.length} games`).locator("visible=true");

const never = shelf.filter(isNeverPlayed);
const played = shelf.filter((game) => !isNeverPlayed(game));

const plannedCount = (games: Shelf) => games.filter(nightOf).length;

test("sums up the shelf", async ({ page }) => {
	await open(page);

	await expect(page.getByText(summaryOf(shelf))).toBeVisible();
	await expect(page.getByRole("combobox", { name: "Not played in" })).toHaveText("6+ months");
	await expect(listCount(page, shelf.length)).toBeVisible();
});

test("shows the money on the shelf", async ({ page }) => {
	const total = priceOf(shelf);
	const neverValue = priceOf(never);
	const rest = expected.ownedValue - total;
	const parts = [
		`Never played ${money(neverValue)}`,
		`Not played in 6+ months ${money(total - neverValue)}`,
		`Rest of the collection ${money(rest)}`,
	];
	await open(page);
	const card = page.getByRole("region", { name: "Money on the shelf" });

	await expect(card.getByRole("paragraph").first()).toHaveText(money(total));
	await expect(card).toContainText(
		`${Math.round((total / expected.ownedValue) * 100)}% of the ${money(expected.ownedValue)} paid for owned games`,
	);
	await expect(card.getByRole("img")).toHaveAccessibleName(
		`Of ${money(expected.ownedValue)} paid for owned games: ${parts.join(", ")}`,
	);
	await expect(card.getByRole("listitem")).toHaveText(parts.map((part) => part.replace(/ (€[\d,]+)$/, "$1")));
});

test("sorts on the price from the money card", async ({ page }) => {
	await open(page);

	await page.getByRole("button", { name: "Sort by price paid" }).click();

	await expect(page).toHaveURL("/shames?sort=price");
	await expect(page.getByRole("combobox", { name: "Sort" })).toHaveText("Price paid");
	await expectTitles(page, inPriceOrder(shelf));
});

test("names the next game night and counts the planned games", async ({ page }) => {
	await open(page);

	await expect(
		page.getByText(
			`Next game night: ${nextNight.title} on ${displayDate(nextNight.startDate)}. ${plannedCount(shelf)} games are coming off the shelf.`,
		),
	).toBeVisible();
});

test("lists every game with its values, longest unplayed first", async ({ page }) => {
	const games = inUnplayedOrder(shelf);
	await open(page);

	await expect(shelfRows(page)).toHaveText(games.map(adminRowText));
});

test("links the rows to the game and to logging a session", async ({ page }) => {
	await open(page);

	for (const game of shelf) {
		const row = shelfRow(page, game.title);
		const id = gameId(game.title);

		await expect(row.getByRole("link", { name: game.title, exact: true })).toHaveAttribute("href", `/games/${id}`);
		await expect(row.getByRole("link", { name: `Log a session of ${game.title}` })).toHaveAttribute(
			"href",
			`/sessions/new/${id}`,
		);
	}
});

test("offers to plan the games that are not planned yet", async ({ page }) => {
	await open(page);

	for (const game of shelf) {
		const row = shelfRow(page, game.title);
		const plan = row.getByRole("button", { name: `Plan ${game.title} for ${nextNight.title}` });
		const unplan = row.getByRole("button", { name: `Remove ${game.title} from the game night` });

		await expect(plan).toHaveCount(nightOf(game) ? 0 : 1);
		await expect(unplan).toHaveCount(nightOf(game) ? 1 : 0);
	}
});

test("shows the covers", async ({ page }) => {
	await open(page);

	await expect(shelfRows(page).getByRole("img")).toHaveCount(shelf.length);
	await expectVisibleImagesToLoad(page);
});

test("opens the row of a game", async ({ page }) => {
	const [game] = inUnplayedOrder(shelf);
	await open(page);

	await shelfRow(page, game.title).getByRole("link", { name: game.title, exact: true }).click();

	await expect(page).toHaveURL(`/games/${gameId(game.title)}`);
});

test("leads to the settings of the shelf", async ({ page }) => {
	await open(page);

	await expect(page.getByRole("link", { name: "change in Settings" })).toHaveAttribute(
		"href",
		"/settings?tab=shelf-of-shame",
	);
});

test.describe("show", () => {
	test("counts the games per choice", async ({ page }) => {
		await open(page);

		await expect(showButton(page, "All", shelf.length)).toHaveAttribute("aria-pressed", "true");
		await expect(showButton(page, "Never played", never.length)).toHaveAttribute("aria-pressed", "false");
		await expect(showButton(page, "Played before", played.length)).toHaveAttribute("aria-pressed", "false");
	});

	test("shows only the games that were never played", async ({ page }) => {
		await open(page);

		await showButton(page, "Never played", never.length).click();

		await expect(page).toHaveURL("/shames?show=never");
		await expect(listCount(page, never.length)).toBeVisible();
		await expectTitles(page, inUnplayedOrder(never));
	});

	test("shows only the games that were played before", async ({ page }) => {
		await open(page);

		await showButton(page, "Played before", played.length).click();

		await expect(page).toHaveURL("/shames?show=played");
		await expect(listCount(page, played.length)).toBeVisible();
		await expectTitles(page, played);
	});

	test("goes back to all", async ({ page }) => {
		await open(page, "?show=never");

		await showButton(page, "All", shelf.length).click();

		await expect(page).toHaveURL("/shames");
		await expect(shelfRows(page)).toHaveCount(shelf.length);
	});
});

test.describe("owned", () => {
	test("keeps the games owned for more than a year", async ({ page }) => {
		const games = ownedLongerThan(shelf, 1);
		await open(page);

		await pick(page, "Owned", "more than 1 year");

		await expect(page).toHaveURL("/shames?owned=1y");
		await expect(listCount(page, games.length)).toBeVisible();
		await expectTitles(page, inUnplayedOrder(games));
	});

	test("says when no game was owned for more than two years", async ({ page }) => {
		expect(ownedLongerThan(shelf, 2)).toEqual([]);
		await open(page);

		await pick(page, "Owned", "more than 2 years");

		await expect(page).toHaveURL("/shames?owned=2y");
		await expect(page.getByText("No games on the shelf match these filters.")).toBeVisible();
		await expect(listCount(page, 0)).toBeVisible();
		await expect(page.getByRole("list", { name: "Games on the shelf" })).toHaveCount(0);
	});

	test("goes back to any time", async ({ page }) => {
		await open(page, "?owned=2y");

		await pick(page, "Owned", "any time");

		await expect(page).toHaveURL("/shames");
		await expect(shelfRows(page)).toHaveCount(shelf.length);
	});

	test("combines with the show filter", async ({ page }) => {
		await open(page, "?owned=1y&show=played");

		const games = ownedLongerThan(played, 1);
		await expect(listCount(page, games.length)).toBeVisible();
		await expectTitles(page, games);
	});
});

test.describe("sort", () => {
	const orders: [string, string, Shelf][] = [
		["Price paid", "price", inPriceOrder(shelf)],
		["Owned since", "owned", inOwnedOrder(shelf)],
		["Title", "title", inTitleOrder(shelf)],
	];

	for (const [label, value, games] of orders) {
		test(`sorts on ${label}`, async ({ page }) => {
			await open(page);

			await pick(page, "Sort", label);

			await expect(page).toHaveURL(`/shames?sort=${value}`);
			await expectTitles(page, games);
		});
	}

	test("goes back to the longest unplayed", async ({ page }) => {
		await open(page, "?sort=title");

		await pick(page, "Sort", "Longest unplayed");

		await expect(page).toHaveURL("/shames");
		await expectTitles(page, inUnplayedOrder(shelf));
	});
});

test.describe("the address", () => {
	test("restores the filters", async ({ page }) => {
		await open(page, "?show=never&owned=1y&sort=title");

		await expect(showButton(page, "Never played", never.length)).toHaveAttribute("aria-pressed", "true");
		await expect(page.getByRole("combobox", { name: "Owned" })).toHaveText("more than 1 year");
		await expect(page.getByRole("combobox", { name: "Sort" })).toHaveText("Title");
		await expectTitles(page, inTitleOrder(ownedLongerThan(never, 1)));
	});

	test("ignores unknown values", async ({ page }) => {
		await open(page, "?show=bogus&owned=bogus&sort=bogus&months=bogus");

		await expect(showButton(page, "All", shelf.length)).toHaveAttribute("aria-pressed", "true");
		await expect(page.getByRole("combobox", { name: "Owned" })).toHaveText("any time");
		await expect(page.getByRole("combobox", { name: "Sort" })).toHaveText("Longest unplayed");
		await expect(page.getByRole("combobox", { name: "Not played in" })).toHaveText("6+ months");
	});

	test("cannot go below the configured months", async ({ page }) => {
		await open(page, "?months=3");

		await expect(page.getByRole("combobox", { name: "Not played in" })).toHaveText("6+ months");
		await expect(shelfRows(page)).toHaveCount(shelf.length);
	});
});

test.describe("not played in", () => {
	test("offers the configured months and longer", async ({ page }) => {
		await open(page);

		await page.getByRole("combobox", { name: "Not played in" }).click();

		await expect(page.getByRole("option")).toHaveText(["6+ months", "12+ months", "24+ months"]);
	});

	test("narrows the shelf to longer periods", async ({ page }) => {
		const games = shelfOf(12);
		const total = priceOf(games);
		await open(page);

		await pick(page, "Not played in", "12+ months");

		await expect(page).toHaveURL("/shames?months=12");
		await expectTitles(page, inUnplayedOrder(games));
		await expect(page.getByText(summaryOf(games))).toBeVisible();
		const card = page.getByRole("region", { name: "Money on the shelf" });
		await expect(card.getByRole("paragraph").first()).toHaveText(money(total));
		await expect(card.getByRole("listitem").first()).toHaveText(
			`Never played${money(priceOf(games.filter(isNeverPlayed)))}`,
		);
		await expect(page.getByText(`${plannedCount(games)} games are coming off the shelf.`)).toBeVisible();
	});

	test("names what is left the rest of the collection", async ({ page }) => {
		await open(page, "?months=12");
		const card = page.getByRole("region", { name: "Money on the shelf" });

		await expect(card.getByRole("listitem").last()).toHaveText(
			`Rest of the collection${money(expected.ownedValue - priceOf(shelfOf(12)))}`,
		);
	});

	test("goes back to the configured months", async ({ page }) => {
		await open(page, "?months=12");

		await pick(page, "Not played in", "6+ months");

		await expect(page).toHaveURL("/shames");
		await expect(shelfRows(page)).toHaveCount(shelf.length);
	});

	test("keeps the filters when the months change", async ({ page }) => {
		await open(page, "?sort=title");

		await pick(page, "Not played in", "12+ months");

		await expect(page).toHaveURL("/shames?sort=title&months=12");
		await expectTitles(page, inTitleOrder(shelfOf(12)));
	});

	test("says when nothing is left on the shelf", async ({ page }) => {
		expect(shelfOf(24)).toEqual([]);
		await open(page);

		await pick(page, "Not played in", "24+ months");

		await expect(page.getByRole("heading", { name: "Nothing on the shelf", level: 2 })).toBeVisible();
		await expect(page.getByText("Owned games that haven't been played in a while show up here.")).toBeVisible();
		await expect(page.getByRole("region", { name: "Money on the shelf" })).toHaveCount(0);
		await expect(page.getByRole("group", { name: "Show" })).toHaveCount(0);
		await expect(page.getByRole("combobox", { name: "Not played in" })).toHaveText("24+ months");
	});
});

test("shows a reader the shelf without actions", async ({ readerPage }) => {
	const games = inUnplayedOrder(shelf);
	await open(readerPage);
	const main = readerPage.getByRole("main");

	await expect(shelfRows(readerPage)).toHaveText(games.map(readerRowText));
	await expect(main.getByRole("button", { name: /^Plan / })).toHaveCount(0);
	await expect(main.getByRole("button", { name: /^Remove / })).toHaveCount(0);
	await expect(main.getByRole("link", { name: /^Log a session of/ })).toHaveCount(0);
	await expect(main.getByRole("link", { name: "change in Settings" })).toHaveCount(0);
});

test("offers a user the actions but not the settings", async ({ writerPage }) => {
	await open(writerPage);
	const main = writerPage.getByRole("main");

	await expect(main.getByRole("link", { name: /^Log a session of/ })).toHaveCount(shelf.length);
	await expect(main.getByRole("button", { name: /^Plan / })).toHaveCount(shelf.length - plannedCount(shelf));
	await expect(main.getByRole("link", { name: "change in Settings" })).toHaveCount(0);
});

test.describe("on a phone", () => {
	test.beforeEach(async ({ page }) => {
		await page.setViewportSize({ width: 375, height: 812 });
	});

	test("folds the filters away and counts the active ones", async ({ page }) => {
		await open(page);
		const toggle = page.getByRole("button", { name: "Filters", exact: true });

		await expect(toggle).toHaveAttribute("aria-expanded", "false");
		await expect(page.getByRole("group", { name: "Show" })).toBeHidden();

		await toggle.click();
		await expect(toggle).toHaveAttribute("aria-expanded", "true");
		await showButton(page, "Never played", never.length).click();

		await expect(page.getByRole("button", { name: "Filters (1)" })).toBeVisible();
		await expect(listCount(page, never.length)).toBeVisible();
	});

	test("fits the screen", async ({ page }) => {
		await open(page);
		await expect(shelfRows(page)).toHaveCount(shelf.length);

		const { contentWidth, screenWidth } = await page.evaluate(() => ({
			contentWidth: document.documentElement.scrollWidth,
			screenWidth: document.documentElement.clientWidth,
		}));

		expect(contentWidth).toBeLessThanOrEqual(screenWidth);
	});
});
