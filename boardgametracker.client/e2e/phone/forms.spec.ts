import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { pick } from "../support/gamesPage";
import { phone, settled, smallPhone } from "../support/phone";
import { expected, games, players } from "../support/seedData";

const bar = (page: Page) => page.locator('[data-slot="form-action-bar"]');

const focusedClearOfBar = (page: Page) =>
	page.evaluate(() => {
		const element = document.activeElement;
		const actionBar = document.querySelector('[data-slot="form-action-bar"]');
		if (!element || !actionBar || actionBar.contains(element)) return true;
		return element.getBoundingClientRect().bottom <= actionBar.getBoundingClientRect().top;
	});

test.beforeEach(async ({ page }) => {
	await page.setViewportSize(phone);
});

test("add game keeps its buttons in a bar at the bottom of the screen", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);

	await expect(bar(page).getByRole("button")).toHaveText(["Cancel", "Add game"]);
	await expect(bar(page)).toBeInViewport({ ratio: 1 });
	await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
	await expect(bar(page)).toBeInViewport({ ratio: 1 });
});

test("add game shows the error next to the title when tapping save from the bottom", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));

	await bar(page).getByRole("button", { name: "Add game" }).click();

	await expect(page.getByText("Game title is required")).toBeInViewport();
	await expect(page.getByRole("textbox", { name: "Game title" })).toBeFocused();
	await expect(page).toHaveURL("/games/new");
});

test("every field of add game stays clear of the action bar while tabbing", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	await page.getByRole("textbox", { name: "Game title" }).focus();
	const cancel = bar(page).getByRole("button", { name: "Cancel" });

	for (let step = 1; step < 40; step++) {
		await page.keyboard.press("Tab");
		await page.evaluate(() => new Promise(requestAnimationFrame));
		expect(await focusedClearOfBar(page), `behind the bar after ${step} tabs`).toBe(true);
		if (await cancel.evaluate((button) => button === document.activeElement)) return;
	}
	throw new Error("Tab never reached the action bar");
});

test("the keyboard reaches the save button of add game", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	await page.getByRole("switch", { name: "Can be played co-op" }).focus();

	await page.keyboard.press("Tab");
	await expect(bar(page).getByRole("button", { name: "Cancel" })).toBeFocused();
	await page.keyboard.press("Tab");
	await expect(bar(page).getByRole("button", { name: "Add game" })).toBeFocused();
});

test("a select opens with the keyboard, inside the screen, and sets the value", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	const select = page.getByRole("combobox", { name: "Game state" });

	await select.focus();
	await page.keyboard.press("Enter");
	const options = page.getByRole("option");
	await expect(options).toHaveText(["Wanted", "Owned", "Previously owned", "For trade", "Sold"]);
	for (const option of await options.all()) await expect(option).toBeInViewport({ ratio: 1 });

	await page.getByRole("option", { name: "Wanted" }).click();
	await expect(select).toHaveText("Wanted");
});

test("the date picker stays inside the screen and picking a day fills the date", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	await page.getByRole("button", { name: "Open calendar Date added" }).click();
	const calendar = page.getByRole("grid");

	await expect(calendar).toBeInViewport({ ratio: 1 });
	await calendar.getByText("15", { exact: true }).click();

	await expect(calendar).toBeHidden();
	await expect(page.getByRole("spinbutton", { name: "day, Date added" })).toHaveText("15");
});

test("the date field accepts typed digits", async ({ page }) => {
	await page.goto("/games/new");
	await settled(page);
	const day = page.getByRole("spinbutton", { name: "day, Date added" });

	await day.click();
	await page.keyboard.type("15");

	await expect(day).toHaveText("15");
	await expect(page.getByRole("spinbutton", { name: "month, Date added" })).toBeFocused();
});

test("the calendar fits a small phone", async ({ page }) => {
	await page.setViewportSize(smallPhone);
	await page.goto("/games/new");
	await settled(page);

	await page.getByRole("button", { name: "Open calendar Date added" }).click();

	const box = await page.getByRole("grid").boundingBox();
	expect(box?.x ?? -1).toBeGreaterThanOrEqual(0);
	expect((box?.x ?? 0) + (box?.width ?? 0)).toBeLessThanOrEqual(smallPhone.width);
});

test("add player refuses an empty name and keeps its bar", async ({ page }) => {
	await page.goto("/players/new");
	await settled(page);

	await bar(page).getByRole("button", { name: "Add player" }).click();

	await expect(page).toHaveURL("/players/new");
	await expect(page.getByText(/name is required/i)).toBeInViewport();
});

test("add player cancels back to the list", async ({ page }) => {
	await page.goto("/players");
	await settled(page);
	await page.getByRole("button", { name: "Add player" }).click();
	await expect(page).toHaveURL("/players/new");

	await bar(page).getByRole("button", { name: "Cancel" }).click();

	await expect(page).toHaveURL("/players");
	await expect(page.getByRole("main").getByRole("link", { name: new RegExp(players[0]) })).toBeVisible();
});

test("log session lists the games in a select that fits the screen and filters by typing", async ({ page }) => {
	await page.goto("/sessions/new");
	await settled(page);

	await page.getByRole("combobox", { name: "Game" }).click();
	await expect(page.getByRole("listbox")).toBeInViewport({ ratio: 1 });
	await page.keyboard.type(games[1].title.slice(0, 4));

	await expect(page.getByRole("option")).toHaveCount(1);
	await page.getByRole("option").click();
	await expect(page.getByRole("combobox", { name: "Game" })).toContainText(games[1].title);
});

test("log session refuses a session without a game", async ({ page }) => {
	await page.goto("/sessions/new");
	await settled(page);
	await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));

	await bar(page).getByRole("button", { name: "Log session" }).click();

	await expect(page.getByText("Game is required")).toBeInViewport();
	await expect(page.getByRole("combobox", { name: "Game" })).toBeFocused();
	await expect(page).toHaveURL("/sessions/new");
});

test("the games filters fold open and closed", async ({ page }) => {
	await page.goto("/games");
	await settled(page);
	const panel = page.locator("#games-filters");
	const height = async () => (await panel.boundingBox())?.height ?? 0;

	await expect(panel).toHaveAttribute("inert");
	await expect.poll(height).toBe(0);

	await page.getByRole("button", { name: "Filters" }).click();
	await expect(panel).not.toHaveAttribute("inert");
	await expect.poll(height).toBeGreaterThan(100);
	await pick(page, "State", "Wanted");
	await expect(page.getByText(`${expected.wantedCount} games`)).toBeVisible();

	await page.getByRole("button", { name: "Hide filters" }).click();
	await expect(panel).toHaveAttribute("inert");
	await expect.poll(height).toBe(0);
	await expect(page.getByRole("button", { name: "Filters · 1" })).toBeVisible();
});

test("the leaderboard and shelf filters fold open", async ({ page }) => {
	await page.goto("/leaderboard");
	await settled(page);
	await page.getByRole("button", { name: "More filters" }).click();
	await expect(page.getByRole("combobox", { name: "Rank by" })).toBeInViewport();

	await page.goto("/shames");
	await settled(page);
	await page.getByRole("button", { name: "Filters", exact: true }).click();
	await expect(page.getByRole("combobox", { name: "Owned" })).toBeVisible();
});
