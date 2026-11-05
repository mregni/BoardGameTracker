import { expect, test } from "../support/fixtures";
import { gameLink, openGames, pick } from "../support/gamesPage";
import { expected, games } from "../support/seedData";

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

test("refuses a game without a title", async ({ page }) => {
	await page.goto("/games/new");

	await page.getByRole("main").getByRole("button", { name: "Add game" }).click();

	await expect(page.getByText("Game title is required")).toBeVisible();
	await expect(page).toHaveURL("/games/new");
});

test("keeps a game when deleting is cancelled", async ({ page }) => {
	const [game] = games;
	await page.goto("/games/1");

	await page.getByRole("main").getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();

	await expect(page.getByRole("dialog")).toBeHidden();
	await expect(page.getByRole("heading", { name: game.title, level: 1 })).toBeVisible();
});

test("adds an expansion by name and removes it", async ({ page }) => {
	const title = "Hand made expansion";
	await page.goto("/games/3");
	await page.getByRole("button", { name: "Expansions (0)" }).click();
	const dialog = page.getByRole("dialog");

	await dialog.getByRole("textbox", { name: "Expansion name" }).fill(title);
	await dialog.getByRole("button", { name: "Add" }).click();

	await expect(dialog.getByText(title)).toBeVisible();
	await expect(dialog.getByText("Added manually")).toBeVisible();

	await dialog.getByRole("button", { name: "Delete" }).click();

	await expect(dialog.getByText("No expansions added to this game.")).toBeVisible();
});

test("uploads a manual and deletes it", async ({ page }) => {
	const name = "rules.pdf";
	await page.goto("/games/3");
	await page.getByRole("button", { name: "Manuals (0)" }).click();
	const dialog = page.getByRole("dialog");

	await dialog.locator('input[type="file"]').setInputFiles({
		name,
		mimeType: "application/pdf",
		buffer: Buffer.from("%PDF-1.4\n%%EOF\n"),
	});

	await expect(dialog.getByText(/rules/)).toBeVisible();

	await dialog.getByRole("button", { name: "Delete" }).click();

	await expect(dialog.getByText("No manuals uploaded for this game.")).toBeVisible();
});

test("changes the price inline in the table and updates the total", async ({ page }) => {
	const game = games.find((x) => x.state === "owned" && x.buyingPrice !== null) ?? games[0];
	const raised = (game.buyingPrice ?? 0) + 10;
	const total = (value: number) => `Total price (owned): €${value.toLocaleString("en-US")}`;
	await openGames(page);
	await page.getByRole("button", { name: "Table" }).click();
	const row = page.getByRole("row", { name: new RegExp(game.title) });
	const changePrice = async (from: number, to: number) => {
		await row.getByText(`€${from}`, { exact: true }).click();
		await page.getByRole("textbox", { name: `Price: ${game.title}` }).fill(String(to));
		await page.keyboard.press("Enter");
	};

	await changePrice(game.buyingPrice ?? 0, raised);
	await expect(page.getByText(total(expected.ownedValue + 10))).toBeVisible();

	await changePrice(raised, game.buyingPrice ?? 0);
	await expect(page.getByText(total(expected.ownedValue))).toBeVisible();
});

test("edits every kind of field and marks a game as sold", async ({ page }) => {
	const title = "Game to sell";
	await page.goto("/games/new");
	await page.getByRole("textbox", { name: "Game title" }).fill(title);
	await page.getByRole("main").getByRole("button", { name: "Add game" }).click();
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();

	await page.getByRole("main").getByRole("button", { name: "Edit" }).click();
	await pick(page, "Game state", "Sold");
	await page.getByRole("textbox", { name: "Sold price" }).fill("15");
	await page.getByRole("textbox", { name: "Rating (0–10)" }).fill("7.5");
	await page.getByRole("textbox", { name: "Weight (0–5)" }).fill("2.5");
	await page.getByRole("main").getByRole("button", { name: "Save" }).click();

	await expect(page).toHaveURL(/\/games\/\d+$/);
	const main = page.getByRole("main");
	await expect(main.getByText("Sold", { exact: true })).toBeVisible();
	await expect(main).toContainText("Rating7.5 / 10");
	await expect(main).toContainText("Weight2.5 / 5");

	await main.getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
	await expect(page).toHaveURL("/games");
	await expect(gameLink(page, title)).toHaveCount(0);
});
