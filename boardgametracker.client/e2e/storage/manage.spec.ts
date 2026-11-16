import type { Page } from "@playwright/test";
import { adminApi } from "../support/adminApi";
import { expect, test } from "../support/fixtures";
import { games, locations } from "../support/seedData";

const [clubhouse] = locations;
const game = games[5];
const cabinet = "E2E Kallax";

const option = (page: Page, name: string | RegExp) => page.getByRole("option", { name });

test.afterEach(async ({ request }) => {
	const api = await adminApi(request);
	const all: { id: number; title: string }[] = await api.get("/api/game");
	const stored = all.find((x) => x.title === game.title);
	if (stored) {
		await api.put("/api/storage/assign", {
			items: [{ type: "game", id: stored.id }],
			locationId: null,
			storageLocationId: null,
		});
	}
	const nodes: { id: number; name: string | null; parentId: number | null }[] = await api.get("/api/storage");
	for (const { id } of nodes.filter((node) => node.name === cabinet && node.parentId === null)) {
		await api.remove(`/api/storage/${id}`);
	}
});

test("gives a game a place in a new cabinet, finds it and moves it up when the cabinet goes", async ({ page }) => {
	await page.goto("/storage");
	await expect(page.getByRole("heading", { name: "Storage", level: 1 })).toBeVisible();

	await page.getByRole("button", { name: "New storage location" }).click();
	const dialog = page.getByRole("dialog", { name: "New storage location" });
	await dialog.getByRole("textbox", { name: "Name" }).fill(cabinet);
	await dialog.getByRole("combobox", { name: "Inside" }).click();
	await option(page, clubhouse).click();
	await dialog.getByRole("button", { name: "Grid" }).click();
	await dialog.getByRole("spinbutton", { name: "Rows" }).fill("2");
	await dialog.getByRole("spinbutton", { name: "Columns" }).fill("2");
	await expect(dialog).toContainText("Creates 4 cells");
	await dialog.getByRole("button", { name: "Create" }).click();

	await expect(page.getByRole("heading", { name: cabinet, level: 2 })).toBeVisible();
	await expect(page.getByRole("group", { name: "R2 C2" })).toBeVisible();

	const inbox = page.getByRole("heading", { name: /Needs a place/ }).locator("..");
	await inbox.getByRole("checkbox", { name: game.title }).check();
	await inbox.getByRole("combobox", { name: "Move 1 game to…" }).click();
	await option(page, `${clubhouse} › ${cabinet} › R1 C1`).click();

	await expect(page.getByRole("group", { name: "R1 C1" }).getByRole("button", { name: game.title })).toBeVisible();

	await page.getByRole("combobox", { name: "Where is…? Type a game or expansion" }).fill(game.title);
	await expect(page.getByRole("option", { name: new RegExp(`${clubhouse} › ${cabinet} › R1 C1`) })).toBeVisible();
	await page.keyboard.press("Escape");

	await page.goto("/games");
	await page.getByRole("combobox", { name: "Storage" }).click();
	await option(page, new RegExp(`^${clubhouse} › ${cabinet} \\(1\\)`)).click();
	await expect(page.getByRole("status")).toHaveText(
		"Type switched to Base games + expansions because a storage location is selected.",
	);
	await expect(page.getByRole("main").getByRole("link", { name: new RegExp(game.title) })).toBeVisible();

	await page.goto("/storage");
	await page
		.getByRole("list", { name: "All storage" })
		.getByRole("button", { name: new RegExp(cabinet) })
		.click();
	await page.getByRole("button", { name: `Delete ${cabinet}` }).click();
	const confirm = page.getByRole("dialog", { name: `Delete ${cabinet}?` });
	await expect(confirm).toContainText("Its 4 shelves or cells are deleted as well.");
	await expect(confirm).toContainText(clubhouse);
	await confirm.getByRole("button", { name: `Delete ${cabinet}` }).click();

	await expect(page.getByRole("heading", { name: clubhouse, level: 2 })).toBeVisible();
	await expect(page.getByRole("region", { name: clubhouse }).getByRole("button", { name: game.title })).toBeVisible();
});
