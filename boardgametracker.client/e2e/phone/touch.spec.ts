import type { BrowserContext, Page } from "@playwright/test";
import { credentials, expect, test } from "../support/fixtures";
import { expectDialogFits, phone, playerId, settled, signInOnTouchPhone } from "../support/phone";
import { gameNights, linkedPlayer } from "../support/seedData";

const firstNight = gameNights[0].title;
const pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

let context: BrowserContext;
let page: Page;

test.beforeAll(async ({ browser }, info) => {
	context = await signInOnTouchPhone(browser, info, credentials);
});

test.afterAll(async () => {
	await context.close();
});

test.beforeEach(async () => {
	page = await context.newPage();
});

test.afterEach(async () => {
	await page.close();
});

test("runs as a phone without hover", async () => {
	await page.goto("/");

	expect(await page.evaluate(() => matchMedia("(hover: none)").matches)).toBe(true);
	expect(await page.evaluate(() => matchMedia("(pointer: coarse)").matches)).toBe(true);
});

test("the More menu works by tapping", async () => {
	await page.goto("/");
	await settled(page);

	await page.getByRole("button", { name: "More" }).tap();
	await page.locator("a:visible:not(nav a, main a)").filter({ hasText: "Leaderboard" }).tap();

	await expect(page).toHaveURL("/leaderboard");
});

test("a tap on a game opens it", async () => {
	await page.goto("/games");
	await settled(page);
	const link = page.getByRole("main").getByRole("link").first();
	const href = await link.getAttribute("href");

	await link.tap();

	await expect(page).toHaveURL(href ?? "");
});

test("the actions of a game night open by tapping and close by tapping elsewhere", async () => {
	await page.goto("/game-nights");
	await settled(page);
	const menu = page.getByRole("menu", { name: `Actions for ${firstNight}` });

	await page.getByRole("button", { name: `Actions for ${firstNight}` }).tap();
	await expect(menu.getByRole("menuitem")).toHaveText(["Edit", "Delete"]);

	await page.touchscreen.tap(phone.width / 2, 10);
	await expect(menu).toBeHidden();
});

test("the inline price of the games table opens by tapping", async () => {
	await page.goto("/games?view=table");
	await settled(page);
	const input = page.getByRole("textbox", { name: /^Price:/ });

	await page.getByRole("main").getByRole("button", { name: /^€/ }).first().tap();

	await expect(input).toBeVisible();
	await page.keyboard.press("Escape");
	await expect(input).toBeHidden();
});

test("an achievement explains itself after a tap and closes after the next tap", async () => {
	await page.goto(`/players/${playerId(linkedPlayer)}`);
	await settled(page);
	const badge = page.getByRole("button", { name: "First Try" });
	const tooltip = page.getByRole("tooltip");

	await badge.tap();
	await expect(tooltip).toContainText("First Try");
	await expect(tooltip).toBeInViewport({ ratio: 1 });

	await badge.tap();
	await expect(tooltip).toBeHidden();
});

const chooseCover = async () => {
	await page.goto("/games/new");
	await settled(page);

	await page.locator('input[type="file"]').setInputFiles({
		name: "cover.png",
		mimeType: "image/png",
		buffer: Buffer.from(pixel, "base64"),
	});
	return page.getByRole("button", { name: "Remove image" });
};

test("the image chosen for a new game can be removed by touch", async () => {
	const remove = await chooseCover();

	await remove.tap();

	await expect(remove).toHaveCount(0);
});

test("the image chosen for a new game can be removed with the keyboard", async () => {
	const remove = await chooseCover();

	await remove.focus();
	await expect(remove).toBeFocused();
	await page.keyboard.press("Enter");

	await expect(remove).toHaveCount(0);
});

test("the tap targets of the bottom bar are large enough", async () => {
	await page.goto("/");
	await settled(page);

	for (const entry of await page.getByRole("navigation").locator("a, button").all()) {
		const box = await entry.boundingBox();
		expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
		expect(box?.width ?? 0).toBeGreaterThanOrEqual(44);
	}
});

test("the close cross and footer buttons of a dialog are large enough for a finger", async () => {
	await page.goto("/locations");
	await settled(page);
	await page.getByRole("button", { name: "Add location" }).tap();
	const dialog = page.getByRole("dialog");

	await expectDialogFits(dialog);
	for (const button of [
		dialog.getByRole("button", { name: "Close" }),
		dialog.getByRole("button", { name: "Cancel" }),
		dialog.getByRole("button", { name: "Add" }),
	]) {
		await expect.poll(async () => (await button.boundingBox())?.height).toBeGreaterThanOrEqual(44);
		await expect.poll(async () => (await button.boundingBox())?.width).toBeGreaterThanOrEqual(44);
	}

	await dialog.getByRole("button", { name: "Close" }).tap();
	await expect(dialog).toBeHidden();
});

test("a calendar day can be tapped", async () => {
	await page.goto("/games/new");
	await settled(page);

	await page.getByRole("button", { name: "Open calendar Date added" }).tap();
	await page.getByRole("grid").getByText("15", { exact: true }).tap();

	await expect(page.getByRole("spinbutton", { name: "day, Date added" })).toHaveText("15");
});

test("the settings sections open through a select by tapping", async () => {
	await page.goto("/settings");
	await settled(page);

	await page.getByRole("combobox", { name: "Settings" }).tap();
	await page.getByRole("option", { name: "Advanced" }).tap();

	await expect(page).toHaveURL(/tab=advanced/);
	await expect(page.getByRole("heading", { name: "Danger zone", level: 2 })).toBeVisible();
});
