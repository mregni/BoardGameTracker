import type { Locator, Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { pick } from "../support/gamesPage";
import { apiFor, expectNoSidewaysScroll, gameId, locationId, phone, playerId, settled } from "../support/phone";
import { expected, games, linkedPlayer, locations, neverPlayedGame, players } from "../support/seedData";

const main = (page: Page) => page.getByRole("main");
const bar = (page: Page) => page.locator('[data-slot="form-action-bar"]');

const cancelThenConfirm = async (page: Page, open: () => Promise<void>, stillThere: Locator) => {
	const dialog = page.getByRole("dialog");
	await open();
	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
	await expect(stillThere).toBeVisible();

	await open();
	await expect(dialog.getByRole("button", { name: "Delete" })).toBeInViewport({ ratio: 1 });
	await dialog.getByRole("button", { name: "Delete" }).click();
	await expect(dialog).toBeHidden();
};

const newNight = (title: string) => ({
	title,
	notes: "",
	startDate: new Date(Date.now() + 7 * 24 * 3_600_000).toISOString(),
	hostId: playerId(linkedPlayer),
	locationId: locationId(locations[0]),
	suggestedGameIds: [],
	invitedPlayerIds: [playerId(linkedPlayer)],
});

test.beforeEach(async ({ page }) => {
	await page.setViewportSize(phone);
});

test("deletes a game after cancelling once", async ({ page }) => {
	const title = "Phone game to delete";
	const api = await apiFor(page);
	const { id } = await api.post("/api/game", { ...games[0], cover: undefined, title, image: null });
	try {
		await page.goto(`/games/${id}`);
		await settled(page);

		await cancelThenConfirm(
			page,
			() => main(page).getByRole("button", { name: "Delete" }).click(),
			page.getByRole("heading", { name: title, level: 1 }),
		);

		await expect(page).toHaveURL("/games");
		await expect(page.getByText(`${expected.gameCount} games`)).toBeVisible();
		await expect(main(page).getByRole("link", { name: new RegExp(title) })).toHaveCount(0);
		expect(await api.status(`/api/game/${id}`)).toBe(404);
	} finally {
		await api.tidy(`/api/game/${id}`);
	}
});

test("deletes a session from the card list after cancelling once", async ({ page }) => {
	const game = gameId(neverPlayedGame.title);
	const comment = "A phone session with a comment that should stay readable on the card";
	const api = await apiFor(page);
	const { id } = await api.post("/api/session", {
		gameId: game,
		start: new Date().toISOString(),
		minutes: 30,
		locationId: locationId(locations[0]),
		expansionIds: [],
		isCoop: false,
		teamWon: null,
		comment,
		playerSessions: [{ playerId: playerId(linkedPlayer), score: 10, firstPlay: false, won: true }],
	});
	try {
		await page.goto(`/games/${game}/sessions`);
		await settled(page);
		await expect(page.getByText(comment).locator("visible=true")).toBeInViewport({ ratio: 1 });

		await cancelThenConfirm(
			page,
			() => main(page).getByRole("button", { name: "Delete" }).locator("visible=true").click(),
			page.getByText(comment).locator("visible=true"),
		);

		await expect(page.getByText(comment)).toHaveCount(0);
		expect(await api.status(`/api/session/${id}`)).toBe(404);
	} finally {
		await api.tidy(`/api/session/${id}`);
	}
});

test("deletes a player after cancelling once", async ({ page }) => {
	const name = "Phone player to delete";
	const api = await apiFor(page);
	const { id } = await api.post("/api/player", { name });
	try {
		await page.goto(`/players/${id}`);
		await settled(page);

		await cancelThenConfirm(
			page,
			() => main(page).getByRole("button", { name: "Delete" }).click(),
			page.getByRole("heading", { name, level: 1 }),
		);

		await expect(page).toHaveURL("/players");
		await expect(main(page).getByRole("link", { name: new RegExp(players[0]) })).toBeVisible();
		await expect(main(page).getByRole("link", { name: new RegExp(name) })).toHaveCount(0);
		expect(await api.status(`/api/player/${id}`)).toBe(404);
	} finally {
		await api.tidy(`/api/player/${id}`);
	}
});

test("deletes a location after cancelling once", async ({ page }) => {
	const name = "Phone location to delete";
	const api = await apiFor(page);
	const { id } = await api.post("/api/location", { name });
	try {
		await page.goto(`/locations/${id}`);
		await settled(page);

		await cancelThenConfirm(
			page,
			() =>
				main(page)
					.getByRole("button", { name: `Delete ${name}` })
					.click(),
			page.getByRole("heading", { name, level: 1 }),
		);

		await expect(page).toHaveURL("/locations");
		await expect(main(page).getByRole("link", { name: new RegExp(locations[0]) })).toBeVisible();
		await expect(main(page).getByRole("link", { name: new RegExp(name) })).toHaveCount(0);
	} finally {
		await api.tidy(`/api/location/${id}`);
	}
});

test("deletes a loan through its actions menu after cancelling once", async ({ page }) => {
	const title = neverPlayedGame.title;
	const api = await apiFor(page);
	const { id } = await api.post("/api/loans", {
		gameId: gameId(title),
		playerId: playerId(players[4]),
		loanDate: new Date().toISOString(),
		dueDate: null,
	});
	try {
		await page.goto("/loans");
		await settled(page);
		const actions = page.getByRole("button", { name: `Actions for ${title}` });
		const open = async () => {
			await actions.click();
			await page.getByRole("menuitem", { name: "Delete" }).click();
		};

		await cancelThenConfirm(page, open, actions);

		await expect(actions).toHaveCount(0);
		expect(await api.status(`/api/loans/${id}`)).toBe(404);
	} finally {
		await api.tidy(`/api/loans/${id}`);
	}
});

test("deletes a game night from the list after cancelling once", async ({ page }) => {
	const title = "Phone night from the list";
	const api = await apiFor(page);
	const { id } = await api.post("/api/gamenight", newNight(title));
	try {
		await page.goto("/game-nights");
		await settled(page);
		const actions = page.getByRole("button", { name: `Actions for ${title}` });
		const open = async () => {
			await actions.click();
			await page.getByRole("menuitem", { name: "Delete" }).click();
		};

		await cancelThenConfirm(page, open, page.getByRole("heading", { name: title, level: 3 }));

		await expect(page.getByRole("heading", { name: title, level: 3 })).toHaveCount(0);
		expect(await api.status(`/api/gamenight/${id}`)).toBe(404);
	} finally {
		await api.tidy(`/api/gamenight/${id}`);
	}
});

test.describe("from the detail page", () => {
	test.use({ allowedApiStatuses: [401, 404] });

	test("deletes a game night", async ({ page }) => {
		const title = "Phone night from the detail page";
		const api = await apiFor(page);
		const { id } = await api.post("/api/gamenight", newNight(title));
		try {
			await page.goto(`/game-nights/${id}`);
			await settled(page);

			await cancelThenConfirm(
				page,
				() => main(page).getByRole("button", { name: "Delete" }).click(),
				page.getByRole("heading", { name: title, level: 1 }),
			);

			await expect(page).toHaveURL("/game-nights");
			await expect(page.getByRole("heading", { name: title })).toHaveCount(0);
		} finally {
			await api.tidy(`/api/gamenight/${id}`);
		}
	});
});

test("a long game title in most played keeps the player page on a phone", async ({ page }) => {
	const api = await apiFor(page);
	const player = await api.post("/api/player", { name: "Phone player with a long title" });
	const game = await api.post("/api/game", {
		...games[0],
		cover: undefined,
		title: "A very long board game title that keeps going on and on",
		image: null,
	});
	const session = await api.post("/api/session", {
		gameId: game.id,
		start: new Date().toISOString(),
		minutes: 30,
		locationId: locationId(locations[0]),
		expansionIds: [],
		isCoop: false,
		teamWon: null,
		playerSessions: [{ playerId: player.id, score: 10, firstPlay: false, won: true }],
	});
	try {
		await page.goto(`/players/${player.id}`);
		await settled(page);
		await expect(page.getByRole("heading", { name: "Most played games", level: 2 })).toBeVisible();

		test.fail(
			true,
			"BUG: a long title in Most played widens the grid of the player page, so the page scrolls sideways",
		);
		await expectNoSidewaysScroll(page);
	} finally {
		await api.tidy(`/api/session/${session.id}`);
		await api.tidy(`/api/game/${game.id}`);
		await api.tidy(`/api/player/${player.id}`);
	}
});

test("adds a game with a select, a date and a price and removes it", async ({ page }) => {
	const title = "Phone made game";
	const api = await apiFor(page);
	await page.goto("/games/add");
	await page.getByRole("button", { name: /^Manual input/ }).click();

	await page.getByRole("textbox", { name: "Game title" }).fill(title);
	await page.getByRole("textbox", { name: "Price" }).fill("42");
	await pick(page, "Game state", "Wanted");
	await page.getByRole("button", { name: "Open calendar Date added" }).click();
	await page.getByRole("grid").getByText("15", { exact: true }).click();
	await bar(page).getByRole("button", { name: "Add game" }).click();

	await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
	const id = Number(new URL(page.url()).pathname.split("/").pop());
	try {
		const game = await api.get<{ state: string; buyingPrice: number; additionDate: string }>(`/api/game/${id}`);
		expect(game.state).toBe("wanted");
		expect(game.buyingPrice).toBe(42);
		expect(displayDate(new Date(game.additionDate))).toMatch(/^15-/);
	} finally {
		await api.tidy(`/api/game/${id}`);
	}
});

test("adds a player from the phone form", async ({ page }) => {
	const name = "Phone made player";
	const api = await apiFor(page);
	await page.goto("/players/new");

	await page.getByRole("textbox", { name: "Name" }).fill(name);
	await bar(page).getByRole("button", { name: "Add player" }).click();

	await expect(page.getByRole("heading", { name, level: 1 })).toBeVisible();
	await api.tidy(`/api/player/${new URL(page.url()).pathname.split("/").pop()}`);
});

test("adds and renames a location in a dialog with the Enter key", async ({ page }) => {
	const name = "Phone made location";
	const renamed = "Phone renamed location";
	const api = await apiFor(page);
	await page.goto("/locations");
	await settled(page);
	await page.getByRole("button", { name: "Add location" }).click();
	const dialog = page.getByRole("dialog");

	await dialog.getByRole("textbox", { name: "Name" }).fill(name);
	await page.keyboard.press("Enter");
	await expect(dialog).toBeHidden();

	const link = main(page).getByRole("link", { name: new RegExp(name) });
	await expect(link).toBeVisible();
	const href = (await link.getAttribute("href")) ?? "";
	const id = href.split("/").pop();
	try {
		await page.goto(href);
		await settled(page);
		await page.getByRole("button", { name: `Edit ${name}` }).click();
		await dialog.getByRole("textbox", { name: "Name" }).fill(renamed);
		await dialog.getByRole("button", { name: "Save" }).click();

		await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();
	} finally {
		await api.tidy(`/api/location/${id}`);
	}
});

test("logs a session from the phone form and finds it in the card list", async ({ page }) => {
	const title = neverPlayedGame.title;
	const api = await apiFor(page);
	try {
		await page.goto(`/sessions/new/${gameId(title)}`);
		await settled(page);

		await page.getByRole("combobox", { name: "Players & scores" }).click();
		await page.getByRole("option", { name: new RegExp(`^${linkedPlayer}`) }).click();
		await page.keyboard.press("Escape");
		await bar(page).getByRole("button", { name: "Log session" }).click();
		await expect(page).toHaveURL(`/games/${gameId(title)}`);

		await page.goto(`/games/${gameId(title)}/sessions`);
		await settled(page);
		await expect(main(page).getByRole("button", { name: "Edit" })).toHaveCount(1);
	} finally {
		const sessions = await api.get<{ id: number }[]>(`/api/game/${gameId(title)}/sessions`);
		for (const session of sessions) await api.tidy(`/api/session/${session.id}`);
	}
});
