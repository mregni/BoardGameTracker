import { expect, test } from "../support/fixtures";

test.beforeEach(async ({ page }) => {
	await page.setViewportSize({ width: 375, height: 812 });
});

for (const path of ["/", "/games", "/games?view=table", "/games/1", "/games/add", "/games/pick"]) {
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

test("keeps logging a session on the dashboard and moves adding a game away", async ({ page }) => {
	await page.goto("/");
	const main = page.getByRole("main");

	await expect(main.getByRole("link", { name: "Log session" })).toBeVisible();
	await expect(main.getByRole("link", { name: "Add game" })).toBeHidden();
});
