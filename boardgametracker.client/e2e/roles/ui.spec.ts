import type { Locator, Page } from "@playwright/test";
import { credentials } from "../support/fixtures";
import { gameNights, games, locations, players } from "../support/seedData";
import { expect, test } from "../support/session";
import { mockSettings } from "../support/settings";

type Control = (main: Locator) => Locator;

const button =
	(name: string): Control =>
	(main) =>
		main.getByRole("button", { name, exact: true });
const link =
	(name: string): Control =>
	(main) =>
		main.getByRole("link", { name, exact: true });

const [strategyNight] = gameNights;
const [firstGame] = games;
const [location] = locations;
const [firstPlayer] = players;

const surfaces: { path: string; ready: Control; controls: Control[] }[] = [
	{
		path: "/",
		ready: (main) => main.getByRole("heading", { name: strategyNight.title }),
		controls: [link("Log session"), link("Add game")],
	},
	{
		path: "/games",
		ready: (main) => main.getByRole("link", { name: new RegExp(`^${firstGame.title} `) }),
		controls: [button("Add game")],
	},
	{
		path: "/games/1",
		ready: (main) => main.getByRole("heading", { name: firstGame.title, level: 1 }),
		controls: [button("Log session"), button("Edit"), button("Delete")],
	},
	{
		path: "/games/1/sessions",
		ready: (main) => main.getByRole("heading", { name: `${firstGame.title} sessions`, level: 1 }),
		controls: [
			button("Log session"),
			(main) => main.getByRole("columnheader", { name: "Actions" }),
			(main) => main.getByRole("button", { name: "Edit" }).first(),
			(main) => main.getByRole("button", { name: "Delete" }).first(),
		],
	},
	{
		path: "/players",
		ready: (main) => main.getByRole("heading", { name: "Players", level: 1 }),
		controls: [button("Add player")],
	},
	{
		path: "/players/1",
		ready: (main) => main.getByRole("heading", { name: firstPlayer, level: 1 }),
		controls: [button("Edit"), button("Delete")],
	},
	{
		path: "/players/1/sessions",
		ready: (main) => main.getByRole("heading", { name: new RegExp(firstPlayer), level: 1 }),
		controls: [
			(main) => main.getByRole("columnheader", { name: "Actions" }),
			(main) => main.getByRole("button", { name: "Edit" }).first(),
			(main) => main.getByRole("button", { name: "Delete" }).first(),
		],
	},
	{
		path: "/game-nights",
		ready: (main) => main.getByRole("heading", { name: strategyNight.title }),
		controls: [
			button("Add game night"),
			button(`Actions for ${strategyNight.title}`),
			(main) => main.getByRole("button", { name: "Manage RSVPs" }).first(),
			(main) => main.getByRole("button", { name: "Copy invite link" }).first(),
		],
	},
	{
		path: "/game-nights/1",
		ready: (main) => main.getByRole("heading", { name: strategyNight.title, level: 1 }),
		controls: [button("Edit"), button("Delete"), button("Copy invite link"), button("Manage RSVPs")],
	},
	{
		path: "/loans",
		ready: (main) => main.getByRole("button", { name: "Show history" }),
		controls: [
			button("Lend a game"),
			button("Actions for Moon"),
			(main) => main.getByRole("button", { name: "Mark returned Moon" }),
		],
	},
	{
		path: "/locations",
		ready: (main) => main.getByRole("link", { name: new RegExp(location) }),
		controls: [button("Add location")],
	},
	{
		path: "/locations/1",
		ready: (main) => main.getByRole("heading", { name: location, level: 1 }),
		controls: [button(`Edit ${location}`), button(`Delete ${location}`)],
	},
	{
		path: "/shames",
		ready: (main) => main.getByRole("button", { name: "Sort by price paid" }),
		controls: [(main) => main.getByRole("button", { name: /^Plan / }).first()],
	},
];

const open = async (page: Page, path: string, ready: Control) => {
	await page.goto(path);
	const main = page.getByRole("main");
	await expect(ready(main)).toBeVisible();
	return main;
};

for (const { path, ready, controls } of surfaces) {
	test(`a reader cannot change anything on ${path}`, async ({ readerPage: page }) => {
		const main = await open(page, path, ready);

		for (const control of controls) {
			await expect(control(main)).toHaveCount(0);
		}
	});

	test(`a user gets the controls on ${path}`, async ({ writerPage: page }) => {
		const main = await open(page, path, ready);

		for (const control of controls) {
			await expect(control(main)).toBeVisible();
		}
	});
}

const readerRedirects = [
	["/games/add", "/"],
	["/games/new", "/"],
	["/games/bgg", "/"],
	["/games/1/update", "/"],
	["/players/new", "/"],
	["/sessions/new", "/"],
	["/sessions/new/1", "/"],
];

for (const [path, target] of readerRedirects) {
	test(`a reader is sent away from ${path}`, async ({ readerPage: page }) => {
		await page.goto(path);

		await expect(page).toHaveURL(target);
		await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
	});
}

test("a reader is sent away from the BoardGameGeek import even with an API key", async ({ readerPage: page }) => {
	await mockSettings(page, { bggStatus: { isConfigured: true, source: "database", isReadOnly: false } });

	await page.goto("/games/import/start");

	await expect(page).toHaveURL("/");
});

test("a reader is sent away from the rules chat", async ({ readerPage: page }) => {
	await mockSettings(page, { ragEnabled: true });

	await page.goto("/chat");

	await expect(page).toHaveURL("/");
});

test("a user may open the rules chat", async ({ writerPage: page }) => {
	await mockSettings(page, { ragEnabled: true });

	await page.goto("/chat");

	await expect(page).toHaveURL("/chat");
	await expect(page.getByRole("complementary").getByRole("link", { name: "Rules chat" })).toBeVisible();
});

const writePages: { path: string; ready: Control }[] = [
	{ path: "/games/add", ready: (main) => main.getByRole("heading", { name: "Add game", level: 1 }) },
	{ path: "/games/new", ready: (main) => main.getByRole("textbox", { name: "Game title" }) },
	{ path: "/players/new", ready: (main) => main.getByRole("heading", { name: "Add player", level: 1 }) },
	{ path: "/sessions/new", ready: (main) => main.getByRole("heading", { name: "Log session", level: 1 }) },
	{ path: "/sessions/new/1", ready: (main) => main.getByRole("combobox", { name: "Game" }).getByText(firstGame.title) },
	{
		path: "/games/1/update",
		ready: (main) => main.getByRole("img", { name: firstGame.title }),
	},
];

for (const { path, ready } of writePages) {
	test(`a user may open ${path}`, async ({ writerPage: page }) => {
		await open(page, path, ready);

		await expect(page).toHaveURL(path);
	});
}

test("a user cannot reach the BoardGameGeek pages without an API key", async ({ writerPage: page }) => {
	await page.goto("/games/bgg");
	await expect(page).toHaveURL("/games/add");

	await page.goto("/games/import/start");
	await expect(page).toHaveURL("/games");
});

test("a reader is sent home from a session editor link", async ({ readerPage: page }) => {
	await page.goto("/sessions/update/1");

	await expect(page).toHaveURL("/", { timeout: 3_000 });
});

test("a user can open a session editor from a direct link", async ({ writerPage: page }) => {
	await page.goto("/sessions/update/1");

	await expect(page.getByRole("main").getByRole("button", { name: "Try again" })).toHaveCount(0, { timeout: 3_000 });
	await expect(page.getByRole("combobox", { name: "Game" })).toContainText(firstGame.title, { timeout: 3_000 });
});

test("the menu has no new session for a reader", async ({ readerPage: page }) => {
	await page.goto("/");

	const menu = page.getByRole("navigation");
	await expect(menu.getByRole("link", { name: "Games" })).toBeVisible();
	await expect(menu.getByRole("link", { name: "Log session" })).toHaveCount(0);
});

test("the menu offers a new session to a user", async ({ writerPage: page }) => {
	await page.goto("/");

	await expect(page.getByRole("navigation").getByRole("link", { name: "Log session" })).toHaveAttribute(
		"href",
		"/sessions/new",
	);
});

test("a reader can look at expansions and manuals but not change them", async ({ readerPage: page }) => {
	await page.goto("/games/1");
	const dialog = page.getByRole("dialog");

	await page.getByRole("button", { name: /^Expansions/ }).click();
	await expect(dialog).toContainText("Sheep in Disguise: Goblin Expansion");
	await expect(dialog.getByRole("textbox")).toHaveCount(0);
	await expect(dialog.getByRole("button", { name: "Delete" })).toHaveCount(0);
	await page.keyboard.press("Escape");

	await page.getByRole("button", { name: /^Manuals/ }).click();
	await expect(dialog).toBeVisible();
	await expect(dialog.getByRole("button", { name: "Upload" })).toHaveCount(0);
});

test("a user can add expansions and manuals", async ({ writerPage: page }) => {
	await page.goto("/games/1");
	const dialog = page.getByRole("dialog");

	await page.getByRole("button", { name: /^Expansions/ }).click();
	await expect(dialog.getByRole("textbox", { name: "Expansion name" })).toBeVisible();
	await expect(dialog.getByRole("button", { name: "Delete" })).toHaveCount(2);
	await page.keyboard.press("Escape");

	await page.getByRole("button", { name: /^Manuals/ }).click();
	await expect(dialog.getByRole("button", { name: "Upload" })).toBeVisible();
});

test("a reader sees past loans without actions", async ({ readerPage: page }) => {
	await page.goto("/loans");

	await page.getByRole("button", { name: "Show history" }).click();

	await expect(page.getByRole("main")).toContainText("Botany");
	await expect(page.getByRole("button", { name: /^Actions for/ })).toHaveCount(0);
});

test.describe("settings of each role", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	for (const [role, fixture] of [
		["a reader", "readerPage"],
		["a user", "writerPage"],
	] as const) {
		test(`${role} only has the account settings`, async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/settings");
			const main = page.getByRole("main");

			await expect(main.getByRole("heading", { name: "My account" })).toBeVisible();
			await expect(main.getByRole("navigation").getByRole("button")).toHaveText(["AccountProfile & password"]);
			await expect(main.getByRole("heading", { name: "Users" })).toHaveCount(0);
		});

		test(`${role} without a player is not asked to link one`, async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/");

			await expect(page.getByRole("region", { name: "Top players" })).toBeVisible();
			await expect(page.getByRole("dialog")).toHaveCount(0);
		});

		test(`${role} cannot open an admin settings tab through the address`, async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/settings?tab=general");

			await expect(page.getByRole("heading", { name: "My account" })).toBeVisible();
			await expect(page.getByRole("button", { name: "Save settings" })).toHaveCount(0);
		});

		test(`${role} has no shortcut to the shelf of shame settings`, async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/shames");

			await expect(page.getByRole("button", { name: "Sort by price paid" })).toBeVisible();
			await expect(page.getByRole("main").getByRole("link", { name: "Change in settings" })).toHaveCount(0);
		});
	}
});

test.describe("as an admin", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("an admin has every settings category and the user list", async ({ page }) => {
		await page.goto("/settings");

		await expect(page.getByRole("main").getByRole("navigation").getByRole("button")).toHaveText([
			"GeneralLanguage, formats, public URL",
			"AccountProfile & user management",
			"IntegrationsBoardGameGeek & price watch",
			"Shelf of ShameWhen a game counts as unplayed",
			"Game nightsPlanning and RSVPs",
			"Single sign-onIdentity provider",
			"AdvancedUpdates and data reset",
		]);

		await page.getByRole("button", { name: /^Account/ }).click();
		await expect(page.getByRole("main")).toContainText("reader");
		await expect(page.getByRole("main")).toContainText("writer");
	});

	test("only an admin can link an account to a player", async ({ page, writerPage }) => {
		await page.goto("/players/1");
		await page.getByRole("button", { name: "Edit", exact: true }).click();
		await expect(page.getByRole("dialog").getByRole("combobox", { name: "Linked account" })).toBeVisible();

		await writerPage.goto("/players/1");
		await writerPage.getByRole("button", { name: "Edit", exact: true }).click();
		await expect(writerPage.getByTestId("linked-account")).toContainText(credentials.username);
		await expect(writerPage.getByRole("dialog").getByRole("combobox", { name: "Linked account" })).toHaveCount(0);
	});
});
