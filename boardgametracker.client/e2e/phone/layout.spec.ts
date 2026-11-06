import type { Locator, Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { gameLink, inTitleOrder } from "../support/gamesPage";
import {
	expectContentClearsBars,
	expectInsideScreen,
	expectNoSidewaysScroll,
	gameId,
	locationId,
	phone,
	playerId,
	settled,
	smallPhone,
	tablet,
} from "../support/phone";
import {
	expected,
	gameNights,
	games,
	linkedPlayer,
	loans,
	locations,
	mostPlayedGame,
	pastGameNight,
	players,
	sessions,
	standingsOf,
} from "../support/seedData";

const titles = inTitleOrder(games.map((game) => game.title));
const firstGame = gameId(mostPlayedGame.title);
const kermit = playerId(linkedPlayer);
const clubhouse = locations[0];
const kermitSessions = sessions.filter((session) => session.scores.some((score) => score.player === linkedPlayer));
const firstGameSessions = sessions
	.filter((session) => session.game === mostPlayedGame.title)
	.sort((a, b) => b.start.getTime() - a.start.getTime());
const firstNight = gameNights[0];
const activeLoans = loans.filter((loan) => !loan.returned);
const nightId = (index: number) => index + 1;
const pastNightId = gameNights.length + 1;

const sessionCard = (page: Page, index: number) =>
	page.getByTestId("session-card-list").locator("> li:visible").nth(index);

const settingsTab = (tab: string, label: string, heading: string) => ({
	path: `/settings?tab=${tab}`,
	heading: "Settings",
	allowed404: tab === "sso" || tab === "account",
	content: (page: Page) => [
		page.getByRole("combobox", { name: "Settings" }).filter({ hasText: label }),
		page.getByRole("heading", { name: heading, level: 2, exact: true }),
	],
});

type PageCase = {
	path: string;
	heading: string;
	allowed404?: boolean;
	content: (page: Page) => Locator[];
};

const pages: PageCase[] = [
	{
		path: "/",
		heading: `Welcome back, ${linkedPlayer}`,
		content: (page) => [
			page.getByRole("heading", { name: firstNight.title, level: 2, exact: true }),
			page.getByRole("heading", { name: "Top players", level: 2, exact: true }),
			page.getByRole("heading", { name: "Recently added", level: 3, exact: true }),
		],
	},
	{
		path: "/games",
		heading: "Games",
		content: (page) => [
			page.getByText(`${expected.gameCount} games`),
			gameLink(page, titles[0]),
			gameLink(page, titles[titles.length - 1]),
		],
	},
	{
		path: "/games?view=table",
		heading: "Games",
		content: (page) => [
			page.getByRole("button", { name: "Table", pressed: true }),
			page.getByRole("main").getByRole("listitem").filter({ hasText: titles[0] }),
			page
				.getByRole("main")
				.getByRole("listitem")
				.filter({ hasText: titles[titles.length - 1] }),
		],
	},
	{
		path: `/games/${firstGame}`,
		heading: mostPlayedGame.title,
		content: (page) => [
			page.getByRole("button", { name: "Log session" }),
			page.getByRole("heading", { name: "Recent sessions", level: 2, exact: true }),
			page.getByRole("heading", { name: "Top players", level: 2, exact: true }),
		],
	},
	{
		path: `/games/${firstGame}/sessions`,
		heading: `${mostPlayedGame.title} sessions`,
		content: (page) => [
			sessionCard(page, 0).filter({ hasText: displayDate(firstGameSessions[0].start) }),
			sessionCard(page, firstGameSessions.length - 1).filter({
				hasText: displayDate(firstGameSessions[firstGameSessions.length - 1].start),
			}),
		],
	},
	{
		path: "/games/add",
		heading: "Add game",
		content: (page) => [page.getByRole("button", { name: /^Manual input/ })],
	},
	{
		path: "/games/new",
		heading: "Add game",
		content: (page) => [
			page.getByRole("textbox", { name: "Game title" }),
			page.getByRole("switch", { name: "Can be played co-op" }),
			page.getByRole("main").getByRole("button", { name: "Add game" }),
		],
	},
	{
		path: `/games/${firstGame}/update`,
		heading: "Edit game",
		content: (page) => [
			page.getByRole("textbox", { name: "Game title" }),
			page.getByRole("switch", { name: "Game has scoring" }),
			page.getByRole("button", { name: "Cancel" }),
		],
	},
	{
		path: "/games/pick",
		heading: "What do we play?",
		content: (page) => [
			page.getByRole("button", { name: "Spin the wheel" }),
			page.getByRole("switch", { name: "Unplayed games first" }),
		],
	},
	{
		path: "/players",
		heading: "Players",
		content: (page) => players.map((name) => page.getByRole("main").getByText(name, { exact: true })),
	},
	{
		path: "/players/new",
		heading: "Add player",
		content: (page) => [page.getByRole("textbox", { name: "Name" }), page.getByRole("button", { name: "Add player" })],
	},
	{
		path: `/players/${kermit}`,
		heading: linkedPlayer,
		content: (page) => [
			page.getByText(String(kermitSessions.length), { exact: true }).nth(0),
			page.getByRole("heading", { name: "Head-to-head", level: 2, exact: true }),
			page.getByRole("heading", { name: "Achievements", level: 2, exact: true }),
			page.getByRole("button", { name: /^Show \d+ locked$/ }),
		],
	},
	{
		path: `/players/${kermit}/sessions`,
		heading: `${linkedPlayer} sessions`,
		content: (page) => [
			page.getByRole("main").getByRole("button", { name: "Edit" }).nth(0),
			page
				.getByRole("main")
				.getByRole("button", { name: "Delete" })
				.nth(kermitSessions.length - 1),
		],
	},
	{
		path: "/shames",
		heading: "Shelf of Shame",
		content: (page) => [
			page.getByRole("heading", { name: "Money on the shelf", level: 2, exact: true }),
			page.getByRole("button", { name: `Plan ${games[18].title} for ${firstNight.title}` }),
		],
	},
	{
		path: "/compare",
		heading: "Player vs player",
		content: (page) => [
			page.getByRole("combobox", { name: `Change left player, now ${players[0]}` }),
			page.getByRole("combobox", { name: `Change right player, now ${players[1]}` }),
			page.getByRole("heading", { name: "Recent sessions together", level: 2, exact: true }),
		],
	},
	{
		path: "/leaderboard",
		heading: "Leaderboard",
		content: (page) => [
			page.getByRole("radio", { name: "All time" }),
			page.getByRole("heading", { name: "Ranking", level: 2, exact: true }),
			page.getByRole("main").getByText(standingsOf(sessions)[0].name).nth(0),
		],
	},
	{
		path: "/game-nights",
		heading: "Game nights",
		content: (page) => [
			page.getByRole("heading", { name: firstNight.title, level: 3, exact: true }),
			page.getByRole("heading", { name: gameNights[1].title, level: 3, exact: true }),
			page.getByRole("button", { name: `Actions for ${firstNight.title}` }),
		],
	},
	{
		path: `/game-nights/${nightId(0)}`,
		heading: firstNight.title,
		content: (page) => [
			page.getByText(`${Object.keys(firstNight.rsvps).length} invited`),
			page.getByRole("button", { name: "Manage RSVPs" }),
			page.getByRole("button", { name: new RegExp(`^${firstNight.suggestedGames[2]}`) }),
		],
	},
	{
		path: `/game-nights/${pastNightId}`,
		heading: pastGameNight.title,
		content: (page) => [
			page.getByRole("heading", { name: "Played", level: 2, exact: true }),
			page.getByRole("button", { name: "Edit who's here" }),
		],
	},
	{
		path: "/loans",
		heading: "Loans",
		content: (page) => [
			page.getByRole("button", { name: `${activeLoans.length} on loan` }),
			...activeLoans.map((loan) => page.getByRole("button", { name: `Mark returned ${loan.game}` })),
			page.getByRole("button", { name: "Show history" }),
		],
	},
	{
		path: "/locations",
		heading: "Locations",
		content: (page) => locations.map((name) => page.getByRole("main").getByText(name, { exact: true })),
	},
	{
		path: `/locations/${locationId(clubhouse)}`,
		heading: clubhouse,
		content: (page) => [
			page.getByRole("button", { name: `Edit ${clubhouse}` }),
			page.getByRole("button", { name: `Delete ${clubhouse}` }),
			page.getByRole("heading", { name: "Recent sessions", level: 2, exact: true }),
		],
	},
	settingsTab("general", "General", "Language and formats"),
	settingsTab("account", "Account", "User management"),
	settingsTab("integrations", "Integrations", "BoardGameGeek API"),
	settingsTab("shelf-of-shame", "Shelf of Shame", "Shelf of Shame"),
	settingsTab("game-nights", "Game nights", "Game nights"),
	settingsTab("sso", "Single sign-on", "Register at the provider"),
	settingsTab("advanced", "Advanced", "Danger zone"),
];

for (const { path, heading, allowed404, content } of pages) {
	test.describe(path, () => {
		test.use({ allowedApiStatuses: allowed404 ? [401, 404] : [401] });

		test(`phone ${path} shows its content inside the screen and clear of the bars`, async ({ page }) => {
			await page.setViewportSize(phone);
			await page.goto(path);
			await settled(page);

			await expect(page.getByRole("heading", { name: heading, level: 1, exact: true })).toBeVisible();
			await expectNoSidewaysScroll(page);
			for (const locator of content(page)) {
				await expect(locator).toBeVisible();
				await expectInsideScreen(page, locator);
			}
			await expectContentClearsBars(page);
		});

		test(`small phone ${path} fits`, async ({ page }) => {
			await page.setViewportSize(smallPhone);
			await page.goto(path);
			await settled(page);

			await expect(page.getByRole("heading", { name: heading, level: 1, exact: true })).toBeVisible();
			await expectContentClearsBars(page);
			await expectNoSidewaysScroll(page);
		});

		test(`tablet ${path} fits`, async ({ page }) => {
			await page.setViewportSize(tablet);
			await page.goto(path);
			await settled(page);

			await expect(page.getByRole("heading", { name: heading, level: 1, exact: true })).toBeVisible();
			await expectNoSidewaysScroll(page);
		});
	});
}

test("offers logging a session but not adding a game on the phone dashboard", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto("/");
	const main = page.getByRole("main");

	await expect(main.getByRole("link", { name: "Log session" })).toBeVisible();
	await expect(main.getByRole("link", { name: "Add game" })).toBeHidden();
});

test("shows the sessions of a game as cards instead of a table", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto(`/games/${firstGame}/sessions`);
	const main = page.getByRole("main");

	await expect(main.getByRole("heading", { level: 1 })).toBeVisible();
	await expect(main.getByRole("table")).toBeHidden();
	await expect(main.getByRole("button", { name: "Edit" })).toHaveCount(firstGameSessions.length);
	await expect(main.getByRole("button", { name: "Delete" })).toHaveCount(firstGameSessions.length);
	for (const [index, session] of firstGameSessions.entries()) {
		const card = sessionCard(page, index);
		await expect(card).toContainText(displayDate(session.start));
		for (const { player, score } of session.scores) {
			await expect(card).toContainText(player);
			await expect(card).toContainText(String(score));
		}
	}
});

test("shows the sessions of a player as cards with the right count", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto(`/players/${kermit}/sessions`);
	const main = page.getByRole("main");

	await expect(main.getByRole("table")).toBeHidden();
	await expect(main.getByRole("button", { name: "Edit" })).toHaveCount(kermitSessions.length);
});

test("shows the games table as a list of cards with every game", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto("/games?view=table");
	const list = page.getByRole("main").getByRole("listitem");

	await expect(page.getByRole("main").getByRole("table")).toBeHidden();
	await expect(list).toHaveCount(expected.gameCount);
	await expect(list.getByRole("link")).toHaveText(titles);
	for (const [index, title] of titles.entries()) {
		const price = games.find((game) => game.title === title)?.buyingPrice;
		if (price) await expect(list.nth(index).getByRole("button", { name: `€${price}`, exact: true })).toBeVisible();
	}
});

test("the sidebar replaces the bottom bar from tablet width", async ({ page }) => {
	await page.setViewportSize(tablet);
	await page.goto("/games");
	await settled(page);
	const sidebarLink = page.getByRole("link", { name: "Leaderboard" });

	await expect(sidebarLink).toBeVisible();
	await expect(page.getByRole("button", { name: "More" })).toHaveCount(0);
	await sidebarLink.click();
	await expect(page).toHaveURL("/leaderboard");
});

test("the game grid uses more columns on a tablet than on a phone", async ({ page }) => {
	const columns = async () => {
		await page.goto("/games");
		await settled(page);
		const boxes = await page
			.getByRole("main")
			.getByRole("link")
			.evaluateAll((links) => links.map((link) => link.getBoundingClientRect().top));
		return boxes.filter((top) => top === boxes[0]).length;
	};

	await page.setViewportSize(phone);
	const onPhone = await columns();
	await page.setViewportSize(tablet);

	expect(await columns()).toBeGreaterThan(onPhone);
});

test("reloading the edit page of a session opens the form", async ({ page }) => {
	await page.setViewportSize(phone);
	await page.goto("/sessions/update/1");

	test.fail(true, "BUG: a reload of the session edit page shows the error page because the loader runs before auth");
	await expect(page.getByRole("heading", { name: "Something went wrong" })).toHaveCount(0);
	await expect(page.getByRole("spinbutton", { name: "Duration" })).toBeVisible({ timeout: 3000 });
});
