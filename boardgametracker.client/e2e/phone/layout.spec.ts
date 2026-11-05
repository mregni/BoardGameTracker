import { expect, test } from "../support/fixtures";

test.beforeEach(async ({ page }) => {
	await page.setViewportSize({ width: 375, height: 812 });
});

for (const path of ["/", "/games", "/games?view=table", "/games/1", "/games/1/sessions", "/games/add", "/games/pick"]) {
	test(`${path} fits a phone without sideways scrolling`, async ({ page }) => {
		await page.goto(path);
		await expect(page.getByRole("main")).toBeVisible();
		await page.waitForLoadState("networkidle");

		const { contentWidth, screenWidth } = await page.evaluate(() => ({
			contentWidth: document.documentElement.scrollWidth,
			screenWidth: document.documentElement.clientWidth,
		}));

		expect(contentWidth).toBeLessThanOrEqual(screenWidth);
	});
}

test("offers logging a session but not adding a game on the phone dashboard", async ({ page }) => {
	await page.goto("/");
	const main = page.getByRole("main");

	await expect(main.getByRole("link", { name: "Log session" })).toBeVisible();
	await expect(main.getByRole("link", { name: "Add game" })).toBeHidden();
});

test("shows the sessions of a game as cards instead of a table", async ({ page }) => {
	await page.goto("/games/1/sessions");
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { level: 1 })).toBeVisible();
	await expect(main.getByRole("table")).toBeHidden();
	await expect(main.getByRole("button", { name: "Edit" }).first()).toBeVisible();
});
