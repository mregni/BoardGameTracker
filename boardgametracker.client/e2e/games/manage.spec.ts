import { expect, test } from "../support/fixtures";
import { expected } from "../support/seedData";

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
