import type { Page } from "@playwright/test";
import { apiFor } from "../support/api";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { gameId, playerId } from "../support/playerStats";
import { linkedPlayer } from "../support/seedData";
import { adminRowText, inUnplayedOrder, nextNight, nightOf, shelf, shelfRow, shelfRows } from "../support/shelf";

const unplanned = inUnplayedOrder(shelf).filter((game) => !nightOf(game));
const [game] = unplanned;
const planned = shelf.filter(nightOf).length;
const plannedDate = displayDate(nextNight.startDate);

const open = async (page: Page) => {
	await page.goto("/shames");
	await expect(shelfRows(page)).toHaveCount(shelf.length);
};

const plan = (page: Page) => page.getByRole("button", { name: `Plan ${game.title} for ${nextNight.title}` });
const unplan = (page: Page) => page.getByRole("button", { name: `Remove ${game.title} from the game night` });
const planLabel = (count: number) => `${count} games are coming off the shelf.`;

test("plans a game and undoes it from the message", async ({ page }) => {
	await open(page);

	await plan(page).click();

	await expect(
		page.getByText(`${game.title} added to the suggestions for ${nextNight.title} · ${plannedDate}`),
	).toBeVisible();
	await expect(unplan(page)).toHaveText(`Planned · ${plannedDate}`);
	await expect(plan(page)).toHaveCount(0);
	await expect(page.getByText(planLabel(planned + 1))).toBeVisible();

	await page.getByRole("button", { name: "Undo" }).click();

	await expect(page.getByText(`${game.title} is no longer suggested`)).toBeVisible();
	await expect(plan(page)).toBeVisible();
	await expect(page.getByText(planLabel(planned))).toBeVisible();
});

test("shows a planned game on the game night and removes it from the shelf page", async ({ page }) => {
	await open(page);
	await plan(page).click();
	await expect(unplan(page)).toBeVisible();

	await page.goto("/game-nights/1");
	await expect(page.getByRole("main").getByText(game.title).first()).toBeVisible();

	await open(page);
	await unplan(page).click();

	await expect(plan(page)).toBeVisible();
	await page.goto("/game-nights/1");
	await expect(page.getByRole("main").getByText(nextNight.title).first()).toBeVisible();
	await expect(page.getByRole("main").getByText(game.title)).toHaveCount(0);
});

test("explains when planning fails", async ({ page }) => {
	await page.route(`**/api/gamenight/1/suggestions/${gameId(game.title)}`, (route) => route.abort());
	await open(page);

	await plan(page).click();

	await expect(page.getByText("Could not add the game to the game night")).toBeVisible();
	await expect(plan(page)).toBeVisible();
	await expect(shelfRow(page, game.title)).toHaveText(adminRowText(game));
});

test("explains when removing a planned game fails", async ({ page }) => {
	const plannedGame = shelf.find(nightOf) ?? game;
	await page.route(`**/api/gamenight/1/suggestions/${gameId(plannedGame.title)}`, (route) => route.abort());
	await open(page);
	const remove = page.getByRole("button", { name: `Remove ${plannedGame.title} from the game night` });

	await remove.click();

	await expect(page.getByText("Could not undo, remove the game on the game night instead")).toBeVisible();
	await expect(remove).toBeVisible();
});

test("takes a game off the shelf once it is played and puts it back", async ({ page }) => {
	const api = await apiFor(page);
	const session = await api.post("/api/session", {
		gameId: gameId(game.title),
		start: new Date().toISOString(),
		minutes: 30,
		expansionIds: [],
		isCoop: false,
		teamWon: null,
		playerSessions: [{ playerId: playerId(linkedPlayer), score: 10, firstPlay: true, won: true }],
	});

	await page.goto("/shames");
	await expect(shelfRows(page)).toHaveCount(shelf.length - 1);
	await expect(page.getByRole("link", { name: game.title, exact: true })).toHaveCount(0);

	await api.remove(`/api/session/${session.id}`);

	await page.goto("/shames");
	await expect(shelfRow(page, game.title)).toHaveText(adminRowText(game));
});

test("offers logging a session with the game filled in", async ({ page }) => {
	await open(page);

	await page.getByRole("link", { name: `Log a session of ${game.title}` }).click();

	await expect(page).toHaveURL(`/sessions/new/${gameId(game.title)}`);
	await expect(page.getByRole("main")).toContainText(game.title);
});

test("shows a reader the planned games without a way to remove them", async ({ readerPage }) => {
	await readerPage.goto("/shames");

	await expect(readerPage.getByRole("main").getByText(`Planned · ${plannedDate}`)).toHaveCount(planned);
	await expect(readerPage.getByRole("main").getByRole("button", { name: /^Remove / })).toHaveCount(0);
});
