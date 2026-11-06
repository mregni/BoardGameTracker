import path from "node:path";
import type { Page } from "@playwright/test";
import { apiFor } from "../support/api";
import { credentials, expect, test } from "../support/fixtures";
import { api, removeCreatedData } from "../support/gameNights";
import { playerId } from "../support/playerStats";
import { expected, games, linkedPlayer, loans, mostPlayedGame, players } from "../support/seedData";

const count = (total: number) => `${total} players`;

test.afterEach(async ({ request }) => {
	await removeCreatedData(request);
});

const addPlayer = async (page: Page, name: string, email = "") => {
	await page.goto("/players/new");
	await page.getByRole("textbox", { name: "Name" }).fill(name);
	await page.getByRole("textbox", { name: "Email" }).fill(email);
	await page.getByRole("main").getByRole("button", { name: "Add player" }).click();
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
	return Number(new URL(page.url()).pathname.split("/").pop());
};

const deletePlayer = async (page: Page, id: number) => {
	await page.goto(`/players/${id}`);
	await page.getByRole("main").getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();
	await expect(page).toHaveURL("/players");
};

const openEdit = async (page: Page) => {
	await page.getByRole("main").getByRole("button", { name: "Edit" }).click();
	return page.getByRole("dialog", { name: "Edit player" });
};

const pickAccount = async (page: Page, option: string) => {
	await page.getByRole("combobox", { name: "Linked account" }).click();
	await page.getByRole("option").filter({ hasText: option }).click();
};

const sessionFor = (playerIds: number[]) => ({
	gameId: 1,
	start: new Date("2020-01-01T12:00:00Z").toISOString(),
	minutes: 30,
	expansionIds: [],
	isCoop: false,
	teamWon: null,
	playerSessions: playerIds.map((playerId, index) => ({
		playerId,
		score: 10 - index,
		firstPlay: false,
		won: index === 0,
	})),
});

test("adds a player, shows it everywhere and deletes it again", async ({ page }) => {
	const name = "Statler";
	await page.goto("/players/new");
	await page.getByRole("textbox", { name: "Name" }).fill(`  ${name}  `);
	await page.getByRole("main").getByRole("button", { name: "Add player" }).click();

	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
	await expect(page.getByText("Player added")).toBeVisible();
	const id = Number(new URL(page.url()).pathname.split("/").pop());

	await page.goto("/players");
	await expect(page.getByText(count(expected.playerCount + 1), { exact: true })).toBeVisible();
	const section = page.getByRole("region", { name: "Not played yet" });
	await expect(section.getByRole("link", { name })).toHaveAttribute("href", `/players/${id}`);
	await expect(section.getByRole("link", { name: "Log a game" })).toHaveAttribute("href", "/sessions/new");

	await page.goto("/");
	await expect(page.getByRole("link", { name: new RegExp(`^Players ${expected.playerCount + 1} `) })).toBeVisible();

	await deletePlayer(page, id);
	await expect(page.getByText("Player deleted")).toBeVisible();
	await expect(page.getByText(count(expected.playerCount), { exact: true })).toBeVisible();
	await expect(page.getByRole("link", { name })).toHaveCount(0);
	await expect(page.getByRole("region", { name: "Not played yet" })).toHaveCount(0);

	await page.goto("/");
	await expect(page.getByRole("link", { name: new RegExp(`^Players ${expected.playerCount} `) })).toBeVisible();
});

test("shows a player without sessions as an invitation", async ({ page, readerPage }) => {
	const name = "Waldorf";
	const id = await addPlayer(page, name);
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { name: `No games logged with ${name} yet`, level: 2 })).toBeVisible();
	await expect(main.getByText("Log a session and their record, ranking and achievements show up here.")).toBeVisible();
	await expect(main.getByRole("term")).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Head-to-head" })).toHaveCount(0);
	await expect(main.getByRole("heading", { name: "Achievements" })).toHaveCount(0);
	await expect(main.getByRole("link", { name: "Compare" })).toHaveCount(0);
	await expect(main.getByRole("link", { name: /on the leaderboard/ })).toHaveCount(0);

	await readerPage.goto(`/players/${id}`);
	await expect(readerPage.getByRole("heading", { name: `No games logged with ${name} yet` })).toBeVisible();
	await expect(readerPage.getByRole("main").getByRole("button", { name: "Log session" })).toHaveCount(0);

	await page.goto(`/players/${id}/sessions`);
	await expect(page.getByRole("table")).toContainText("No data yet");

	await page.goto(`/players/${id}`);
	await main.getByRole("button", { name: "Log session" }).click();
	await expect(page).toHaveURL(`/sessions/new?player=${id}`);
});

test("stores an uploaded picture and removes it again", async ({ page }) => {
	const name = "Sam Eagle";
	await page.goto("/players/new");
	await page
		.locator('input[type="file"]')
		.setInputFiles(path.join(test.info().project.testDir, "support", "avatars", "kermit.jpg"));
	await page.getByRole("textbox", { name: "Name" }).fill(name);
	await page.getByRole("main").getByRole("button", { name: "Add player" }).click();

	const portrait = page.getByRole("complementary").getByRole("img", { name });
	await expect(portrait).toBeVisible();
	await expect
		.poll(() => portrait.evaluate((image: HTMLImageElement) => image.complete && image.naturalWidth > 0))
		.toBe(true);

	const dialog = await openEdit(page);
	await dialog.getByRole("img", { name }).hover({ force: true });
	await dialog.getByRole("button", { name: "Remove" }).click();
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(dialog).toBeHidden();
	await expect(page.getByRole("main").getByRole("complementary")).toHaveCount(0);
});

test("edits the name and email of a player", async ({ page }) => {
	const name = "Bunsen";
	const renamed = "Dr. Bunsen";
	const email = "bunsen@example.test";
	await addPlayer(page, name);

	let dialog = await openEdit(page);
	await expect(dialog.getByRole("textbox", { name: "Name" })).toHaveValue(name);
	await expect(dialog.getByRole("textbox", { name: "Email" })).toHaveValue("");
	await dialog.getByRole("textbox", { name: "Name" }).fill(renamed);
	await dialog.getByRole("textbox", { name: "Email" }).fill(email);
	await dialog.getByRole("button", { name: "Save" }).click();

	await expect(dialog).toBeHidden();
	await expect(page.getByText("Player saved")).toBeVisible();
	await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();

	await page.reload();
	dialog = await openEdit(page);
	await expect(dialog.getByRole("textbox", { name: "Name" })).toHaveValue(renamed);
	await expect(dialog.getByRole("textbox", { name: "Email" })).toHaveValue(email);
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await page.goto("/players");
	await expect(page.getByRole("region", { name: "Not played yet" }).getByRole("link", { name: renamed })).toBeVisible();
});

test("keeps a player when editing is cancelled", async ({ page }) => {
	const name = "Rizzo";
	await addPlayer(page, name);

	const dialog = await openEdit(page);
	await dialog.getByRole("textbox", { name: "Name" }).fill("Something else");
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(dialog).toBeHidden();
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();

	const reopened = await openEdit(page);
	await expect(reopened.getByRole("textbox", { name: "Name" })).toHaveValue(name);
	await reopened.getByRole("button", { name: "Cancel" }).click();
});

test("refuses a player without a name or with a wrong email", async ({ page }) => {
	await page.goto("/players/new");
	const main = page.getByRole("main");

	await main.getByRole("button", { name: "Add player" }).click();
	await expect(main.getByText("Player name is required")).toBeVisible();

	await main.getByRole("textbox", { name: "Name" }).fill("   ");
	await main.getByRole("button", { name: "Add player" }).click();
	await expect(main.getByText("Player name is required")).toBeVisible();

	await main.getByRole("textbox", { name: "Name" }).fill("Pepe");
	await main.getByRole("textbox", { name: "Email" }).fill("no-email");
	await main.getByRole("button", { name: "Add player" }).click();
	await expect(main.getByText("Enter a valid email address")).toBeVisible();

	await expect(page).toHaveURL("/players/new");
	await page.goto("/players");
	await expect(page.getByText(count(expected.playerCount), { exact: true })).toBeVisible();
});

test("refuses an edit without a name or with a wrong email", async ({ page }) => {
	const name = "Crazy Harry";
	await addPlayer(page, name);
	const dialog = await openEdit(page);

	await dialog.getByRole("textbox", { name: "Name" }).fill("");
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog.getByText("Player name is required")).toBeVisible();

	await dialog.getByRole("textbox", { name: "Name" }).fill(name);
	await dialog.getByRole("textbox", { name: "Email" }).fill("no-email");
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog.getByText("Enter a valid email address")).toBeVisible();
});

test("leaves the new player page without saving", async ({ page }) => {
	await page.goto("/players");
	await page.getByRole("button", { name: "Add player" }).click();
	await page.getByRole("textbox", { name: "Name" }).fill("Unsaved");

	await page.getByRole("main").getByRole("button", { name: "Cancel" }).click();

	await expect(page).toHaveURL("/players");
	await expect(page.getByText(count(expected.playerCount), { exact: true })).toBeVisible();

	await page.getByRole("button", { name: "Add player" }).click();
	await page.getByRole("button", { name: "Back" }).click();

	await expect(page).toHaveURL("/players");
});

test("sends a reader away from the new player page", async ({ readerPage }) => {
	await readerPage.goto("/players/new");

	await expect(readerPage).toHaveURL("/");
});

test.describe("refused by the server", () => {
	test.use({ allowedApiStatuses: [400, 401, 409, 422] });

	test("refuses a name that is taken", async ({ page }) => {
		await page.goto("/players/new");
		await page.getByRole("textbox", { name: "Name" }).fill(linkedPlayer);
		await page.getByRole("main").getByRole("button", { name: "Add player" }).click();

		await expect(page.getByText("A player with this name already exists")).toBeVisible();
		await expect(page).toHaveURL("/players/new");
	});

	test("refuses renaming to a name that is taken", async ({ page }) => {
		await addPlayer(page, "Lew Zealand");
		const dialog = await openEdit(page);

		await dialog.getByRole("textbox", { name: "Name" }).fill(linkedPlayer);
		await dialog.getByRole("button", { name: "Save" }).click();

		await expect(page.getByText("A player with this name already exists")).toBeVisible();
	});

	test("keeps the host of a game night", async ({ page }) => {
		await page.goto(`/players/${playerId(linkedPlayer)}`);
		await page.getByRole("main").getByRole("button", { name: "Delete" }).click();
		await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

		await expect(page.getByText("This player hosts one or more game nights")).toBeVisible();
		await expect(page.getByRole("dialog")).toBeHidden();
		await expect(page).toHaveURL(`/players/${playerId(linkedPlayer)}`);
		await expect(page.getByRole("heading", { name: linkedPlayer, level: 1 })).toBeVisible();
	});
});

test("keeps a player when deleting is cancelled", async ({ page }) => {
	const name = players[1];
	await page.goto(`/players/${playerId(name)}`);
	await page.getByRole("main").getByRole("button", { name: "Delete" }).click();
	const dialog = page.getByRole("dialog");

	await expect(dialog).toContainText(`Are you sure you want to delete player ${name}?`);
	await expect(dialog).toContainText("will delete all game sessions connected to this player");
	await dialog.getByRole("button", { name: "Cancel" }).click();

	await expect(dialog).toBeHidden();
	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
});

test("takes the sessions of a deleted player along", async ({ page }) => {
	const name = "Link Hogthrob";
	const rival = playerId(players[1]);
	const api = await apiFor(page);
	const { id } = await api.post("/api/player", { name, image: null });
	await api.post("/api/session", sessionFor([id, rival]));

	await page.goto(`/players/${rival}/sessions`);
	const rows = page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");
	await expect(rows.first()).toBeVisible();
	const before = await rows.count();

	await deletePlayer(page, id);

	await page.goto(`/players/${rival}/sessions`);
	await expect(rows).toHaveCount(before - 1);
});

test("deletes a player with a loan", async ({ page }) => {
	const name = "Pops";
	const api = await apiFor(page);
	const { id } = await api.post("/api/player", { name, image: null });
	const game = games.find(
		(candidate) => candidate.state === "owned" && !loans.some((loan) => loan.game === candidate.title),
	);
	await api.post("/api/loans", {
		gameId: games.indexOf(game ?? games[0]) + 1,
		playerId: id,
		loanDate: new Date().toISOString(),
		dueDate: null,
	});

	await deletePlayer(page, id);

	await expect(page.getByText("Player deleted")).toBeVisible();
	await page.goto("/loans");
	await expect(page.getByRole("main")).toContainText(loans[0].game);
	await expect(page.getByRole("main")).not.toContainText(name);
});

test("deletes a session and updates the player", async ({ page }) => {
	const api = await apiFor(page);
	const name = "Janice";
	const { id } = await api.post("/api/player", { name, image: null });
	await api.post("/api/session", sessionFor([id]));
	await api.post("/api/session", { ...sessionFor([id]), minutes: 45 });

	await page.goto(`/players/${id}/sessions`);
	const rows = page.getByRole("table").getByRole("rowgroup").nth(1).getByRole("row");
	await expect(rows).toHaveCount(2);

	await rows.first().getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(rows).toHaveCount(1);
	await page.goto(`/players/${id}`);
	await expect(
		page
			.getByRole("term")
			.filter({ hasText: /^Sessions$/ })
			.locator("xpath=..")
			.getByRole("definition"),
	).toHaveText("1");
	await expect(page.getByRole("link", { name: mostPlayedGame.title }).first()).toBeVisible();
});

test("links an account to a player and removes the link", async ({ page }) => {
	await addPlayer(page, "Beauregard");
	const account = () => page.getByRole("combobox", { name: "Linked account" });

	let dialog = await openEdit(page);
	await expect(account()).toHaveText(/Not linked/);

	await pickAccount(page, "admin (Administrator)");
	await expect(dialog.getByTestId("linked-account-move")).toHaveText(
		`Currently linked to ${linkedPlayer}; saving moves the link here`,
	);

	await pickAccount(page, "writer");
	await expect(dialog.getByTestId("linked-account-move")).toHaveCount(0);
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog).toBeHidden();

	await page.reload();
	dialog = await openEdit(page);
	await expect(account()).toHaveText(/writer/);

	await pickAccount(page, "Not linked");
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog).toBeHidden();

	await page.reload();
	await openEdit(page);
	await expect(account()).toHaveText(/Not linked/);
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();
});

test.describe("moving an account link", () => {
	test.afterEach(async ({ request }) => {
		const users: { id: string; username: string }[] = await api(request, "get", "/api/admin/users");
		const admin = users.find((user) => user.username === credentials.username);
		await api(request, "put", `/api/admin/players/${playerId(linkedPlayer)}/account`, { userId: admin?.id });
	});

	test("takes the account away from the player who had it", async ({ page }) => {
		const name = "Sweetums";
		await addPlayer(page, name);
		const dialog = await openEdit(page);

		await pickAccount(page, "admin (Administrator)");
		await dialog.getByRole("button", { name: "Save" }).click();
		await expect(dialog).toBeHidden();

		await page.goto("/");
		await expect(page.getByRole("heading", { name: `Welcome back, ${name}`, level: 1 })).toBeVisible();

		await page.goto(`/players/${playerId(linkedPlayer)}`);
		await openEdit(page);
		await expect(page.getByRole("combobox", { name: "Linked account" })).toHaveText(/Not linked/);
	});
});

test("shows a user the linked account without a choice", async ({ writerPage }) => {
	await writerPage.goto(`/players/${playerId(linkedPlayer)}`);
	const dialog = await openEdit(writerPage);

	await expect(dialog.getByTestId("linked-account")).toContainText("admin (Administrator)");
	await expect(dialog.getByRole("combobox", { name: "Linked account" })).toHaveCount(0);
});

test("shows the linked account of the seeded player to the admin", async ({ page }) => {
	await page.goto(`/players/${playerId(linkedPlayer)}`);
	await openEdit(page);

	await expect(page.getByRole("combobox", { name: "Linked account" })).toHaveText(/admin/);
	await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();
});
