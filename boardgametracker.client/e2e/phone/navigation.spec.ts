import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { gameId, phone, playerId, settled } from "../support/phone";
import { expected, gameNights, games, linkedPlayer, loans, locations } from "../support/seedData";

const bottomNav = (page: Page) => page.getByRole("navigation");
const moreButton = (page: Page) => bottomNav(page).getByRole("button", { name: "More" });
const moreEntries = (page: Page) => page.locator("a:visible:not(nav a, main a)");

const lit = (page: Page) =>
	bottomNav(page)
		.locator("a, button")
		.filter({ has: page.locator("div.bg-primary") });

const openMore = async (page: Page) => {
	await settled(page);
	await moreButton(page).click();
	await expect(moreButton(page)).toHaveAttribute("aria-expanded", "true");
};

const adminEntries = [
	{ name: "Shelf of Shame", path: "/shames", heading: "Shelf of Shame", count: String(expected.shameCount) },
	{ name: "Player vs player", path: "/compare", heading: "Player vs player", count: null },
	{ name: "Leaderboard", path: "/leaderboard", heading: "Leaderboard", count: null },
	{ name: "Game nights", path: "/game-nights", heading: "Game nights", count: String(gameNights.length) },
	{ name: "Loans", path: "/loans", heading: "Loans", count: String(loans.filter((x) => !x.returned).length) },
	{ name: "Locations", path: "/locations", heading: "Locations", count: String(locations.length) },
	{ name: "Settings", path: "/settings", heading: "Settings", count: null },
];

test.beforeEach(async ({ page }) => {
	await page.setViewportSize(phone);
});

test("shows the bottom bar with the main entries in order", async ({ page }) => {
	await page.goto("/");
	await settled(page);

	await expect(bottomNav(page).locator("a, button")).toHaveText([
		"Dashboard",
		"Log session",
		"Games",
		"Players",
		"More",
	]);
	await expect(page.getByRole("button", { name: "Add game" })).toHaveCount(0);
});

for (const { name, path, heading } of [
	{ name: "Log session", path: "/sessions/new", heading: "Log session" },
	{ name: "Games", path: "/games", heading: "Games" },
	{ name: "Players", path: "/players", heading: "Players" },
]) {
	test(`the bottom bar opens ${name}`, async ({ page }) => {
		await page.goto("/leaderboard");
		await settled(page);

		await bottomNav(page).getByRole("link", { name }).click();

		await expect(page).toHaveURL(path);
		await expect(page.getByRole("heading", { name: heading, level: 1 })).toBeVisible();
	});
}

test("the bottom bar goes back to the dashboard", async ({ page }) => {
	await page.goto("/players");
	await settled(page);

	await bottomNav(page).getByRole("link", { name: "Dashboard" }).click();

	await expect(page).toHaveURL("/");
	await expect(page.getByRole("heading", { name: `Welcome back, ${linkedPlayer}`, level: 1 })).toBeVisible();
});

test("lights the entry of the open page and only that one", async ({ page }) => {
	for (const [path, name] of [
		["/", "Dashboard"],
		["/games", "Games"],
		["/players", "Players"],
		["/loans", "More"],
		["/settings", "More"],
	]) {
		await page.goto(path);
		await settled(page);
		await expect(lit(page)).toHaveText([name]);
	}
});

test("keeps Games lit on a game detail page", async ({ page }) => {
	test.fail(true, "BUG: the bottom bar lights nothing on detail pages, it compares the exact path");
	await page.goto(`/games/${gameId(games[0].title)}`);
	await settled(page);

	await expect(lit(page)).toHaveText(["Games"], { timeout: 1000 });
});

test("keeps More lit on a game night detail page", async ({ page }) => {
	test.fail(true, "BUG: the bottom bar lights nothing on detail pages, it compares the exact path");
	await page.goto("/game-nights/1");
	await settled(page);

	await expect(lit(page)).toHaveText(["More"], { timeout: 1000 });
});

test("marks only the open page as current for screen readers", async ({ page }) => {
	await page.goto("/games");
	await settled(page);

	await expect(bottomNav(page).locator('[aria-current="page"]')).toHaveText(["Games"]);
});

test("the More menu lists every entry with its count", async ({ page }) => {
	await page.goto("/");
	await openMore(page);

	await expect(moreEntries(page)).toHaveCount(adminEntries.length);
	for (const [index, { name, path, count }] of adminEntries.entries()) {
		const entry = moreEntries(page).nth(index);
		await expect(entry).toHaveAttribute("href", path);
		await expect(entry).toHaveText(`${name}${count ?? ""}`);
	}
});

for (const { name, path, heading } of adminEntries) {
	test(`the More menu opens ${name} and closes`, async ({ page }) => {
		await page.goto("/");
		await openMore(page);

		await moreEntries(page).filter({ hasText: name }).click();

		await expect(page).toHaveURL(path);
		await expect(page.getByRole("heading", { name: heading, level: 1 })).toBeVisible();
		await expect(moreEntries(page)).toHaveCount(0);
		await expect(moreButton(page)).toHaveAttribute("aria-expanded", "false");
	});
}

test("the More button closes the menu again", async ({ page }) => {
	await page.goto("/");
	await openMore(page);

	await moreButton(page).click();

	await expect(moreEntries(page)).toHaveCount(0);
	await expect(page).toHaveURL("/");
});

test("tapping next to the More menu closes it", async ({ page }) => {
	await page.goto("/");
	await openMore(page);

	await page.mouse.click(phone.width / 2, 40);

	await expect(moreEntries(page)).toHaveCount(0);
	await expect(page).toHaveURL("/");
});

test("Escape closes the More menu", async ({ page }) => {
	test.fail(true, "BUG: the More menu ignores Escape, so keyboard users have to tab back to the More button");
	await page.goto("/");
	await openMore(page);

	await page.keyboard.press("Escape");

	await expect(moreEntries(page)).toHaveCount(0, { timeout: 1000 });
});

test("a bottom bar entry closes the More menu", async ({ page }) => {
	test.fail(true, "BUG: the More menu stays open over the new page after tapping a bottom bar entry");
	await page.goto("/");
	await openMore(page);

	await bottomNav(page).getByRole("link", { name: "Games" }).click();

	await expect(page).toHaveURL("/games");
	await expect(moreEntries(page)).toHaveCount(0, { timeout: 1000 });
});

test("lights More and not the page while the menu is open", async ({ page }) => {
	await page.goto("/games");
	await openMore(page);

	await expect(lit(page)).toHaveText(["More"]);
	await moreButton(page).click();
	await expect(lit(page)).toHaveText(["Games"]);
});

test("the More menu marks the open page", async ({ page }) => {
	await page.goto("/loans");
	await openMore(page);

	await expect(moreEntries(page).and(page.locator('[aria-current="page"]'))).toHaveText([/^Loans/]);
});

test("the More menu shows the version and the signed in user", async ({ page }) => {
	await page.goto("/");
	await openMore(page);

	await expect(page.getByText("Version", { exact: true }).locator("visible=true")).toBeVisible();
	await expect(page.getByText(/^v\d+\.\d+\.\d+/).locator("visible=true")).toBeVisible();
	await expect(page.getByText("Administrator", { exact: true }).locator("visible=true")).toBeVisible();
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
});

test("the whole More menu fits above the bottom bar on a small phone", async ({ page }) => {
	await page.setViewportSize({ width: 320, height: 568 });
	await page.goto("/");
	await openMore(page);
	const bar = await bottomNav(page).boundingBox();
	const first = await page.getByText("Version", { exact: true }).locator("visible=true").boundingBox();
	const last = await moreEntries(page).last().boundingBox();

	expect(first?.y ?? -1).toBeGreaterThanOrEqual(0);
	expect((last?.y ?? 0) + (last?.height ?? 0)).toBeLessThanOrEqual(bar?.y ?? 0);
});

test("hides the bottom bar on forms with their own action bar and brings it back", async ({ page }) => {
	await page.goto("/games");
	await settled(page);

	await bottomNav(page).getByRole("link", { name: "Log session" }).click();
	await expect(page.getByRole("heading", { name: "Log session", level: 1 })).toBeVisible();
	await expect(bottomNav(page)).toHaveCount(0);

	await page.getByRole("button", { name: "Cancel" }).click();
	await expect(page).toHaveURL("/games");
	await expect(bottomNav(page).getByRole("link", { name: "Games" })).toBeVisible();
});

test("a detail link keeps the bottom bar", async ({ page }) => {
	await page.goto(`/players/${playerId(linkedPlayer)}`);
	await settled(page);

	await expect(bottomNav(page).getByRole("link", { name: "Players" })).toBeVisible();
});

test.describe("as a Reader", () => {
	test("offers no way to log a session", async ({ readerPage: page }) => {
		await page.setViewportSize(phone);
		await page.goto("/");
		await settled(page);

		await expect(bottomNav(page).locator("a, button")).toHaveText(["Dashboard", "Games", "Players", "More"]);
	});

	test("shows the same More entries without chat", async ({ readerPage: page }) => {
		await page.setViewportSize(phone);
		await page.goto("/");
		await openMore(page);

		await expect(moreEntries(page)).toHaveText(adminEntries.map(({ name, count }) => `${name}${count ?? ""}`));
		await expect(page.getByRole("link", { name: "Chat" })).toHaveCount(0);
	});

	test("shows the reader as the user with a log out button", async ({ readerPage: page }) => {
		await page.setViewportSize(phone);
		await page.goto("/");
		await openMore(page);

		await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
		await expect(page.getByText("reader", { exact: true }).locator("visible=true")).toBeVisible();
	});

	test("hides every way to change data on the list pages", async ({ readerPage: page }) => {
		await page.setViewportSize(phone);
		for (const [path, name] of [
			["/games", "Add game"],
			["/players", "Add player"],
			["/locations", "Add location"],
			["/loans", "Lend a game"],
			["/game-nights", "Add game night"],
		]) {
			await page.goto(path);
			await settled(page);
			await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
			await expect(page.getByRole("button", { name })).toHaveCount(0);
		}
	});
});
