import { expect, test } from "./support/fixtures";

test("the dashboard invites you to add a first game", async ({ page }) => {
	await page.goto("/");

	await expect(page.getByRole("button", { name: "Add your first game" })).toBeVisible();
});
