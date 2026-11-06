import { addDays } from "date-fns";
import { api, apiFor } from "../support/api";
import { expect, test } from "../support/fixtures";
import { removeCreatedData } from "../support/gameNights";
import { openGames, pick } from "../support/gamesPage";
import {
	coopGame,
	expected,
	games,
	linkedPlayer,
	loans,
	locations,
	mostPlayedGame,
	players,
	sessions,
} from "../support/seedData";

const activeLoan = loans.find((loan) => !loan.returned) ?? loans[0];

test.afterEach(async ({ request }) => {
	await removeCreatedData(request);
});

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
	await expect(dialog.getByRole("heading", { name: "Expansions (1)" })).toBeVisible();

	await dialog.getByRole("button", { name: "Delete" }).click();

	await expect(dialog.getByText("No expansions added to this game.")).toBeVisible();
});

test("uploads, downloads and deletes a manual", async ({ page }) => {
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

	const download = page.waitForEvent("download");
	await dialog.getByRole("button", { name: "Download" }).click();
	expect((await download).suggestedFilename()).toContain("rules");

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

test("edits a game, marks it as sold and keeps the sold price", async ({ page }) => {
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
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
	const main = page.getByRole("main");
	await expect(main.getByText("Sold", { exact: true })).toBeVisible();
	await expect(main).toContainText("Rating7.5 / 10");
	await expect(main).toContainText("Weight2.5 / 5");

	await main.getByRole("button", { name: "Edit" }).click();
	await expect(page.getByRole("textbox", { name: "Sold price" })).toHaveValue("15");
});

test("deletes a session and updates the game", async ({ page, request }) => {
	const played = sessions.filter((session) => session.game === coopGame.title).length;
	const gameId = games.indexOf(coopGame) + 1;
	const { post } = await apiFor(page);
	const allLocations: { id: number; name: string }[] = await api(request, "get", "/api/location");
	const location = allLocations.find((x) => x.name === locations[0]) ?? allLocations[0];
	await post("/api/session", {
		gameId,
		start: new Date().toISOString(),
		minutes: 10,
		locationId: location.id,
		expansionIds: [],
		isCoop: true,
		teamWon: true,
		playerSessions: [{ playerId: 1, score: null, firstPlay: false, won: true }],
	});
	await page.goto(`/games/${gameId}/sessions`);
	const rows = page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");
	await expect(rows).toHaveCount(played + 1);

	await rows.first().getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(rows).toHaveCount(played);
	await page.goto(`/games/${gameId}`);
	await expect(page.getByRole("region", { name: new RegExp(`^Played ${played} times`) })).toBeVisible();
});

test("takes the sessions, loans and plans of a deleted game along", async ({ page }) => {
	const title = "Game to lose";
	const name = "Sweetums";
	const nightTitle = "Night with a lost game";
	const { post } = await apiFor(page);
	const { cover: _, ...details } = mostPlayedGame;
	const game = await post("/api/game", { ...details, title, image: null });
	const player = await post("/api/player", { name, image: null });
	await post("/api/session", {
		gameId: game.id,
		start: new Date().toISOString(),
		minutes: 10,
		expansionIds: [],
		isCoop: false,
		teamWon: null,
		playerSessions: [{ playerId: player.id, score: 1, firstPlay: true, won: true }],
	});
	await post("/api/loans", {
		gameId: game.id,
		playerId: player.id,
		loanDate: new Date().toISOString(),
		dueDate: null,
	});
	const night = await post("/api/gamenight", {
		title: nightTitle,
		notes: "",
		startDate: addDays(new Date(), 30).toISOString(),
		hostId: players.indexOf(linkedPlayer) + 1,
		locationId: locations.length,
		suggestedGameIds: [game.id],
		invitedPlayerIds: [],
	});
	await page.goto(`/games/${game.id}`);
	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();

	await page.getByRole("main").getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
	await expect(page).toHaveURL("/games");

	await page.goto(`/players/${player.id}/sessions`);
	await expect(page.getByRole("table")).toContainText("No data yet");

	await page.goto("/loans");
	await expect(page.getByRole("main")).toContainText(activeLoan.game);
	await expect(page.getByRole("main")).not.toContainText(title);

	await page.goto(`/game-nights/${night.id}`);
	await expect(page.getByRole("region", { name: "Planned games" })).toContainText("No games planned yet");
});
