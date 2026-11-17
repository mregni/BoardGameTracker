import path from "node:path";
import type { BrowserContext, Page } from "@playwright/test";
import { credentials } from "../support/fixtures";
import { gameNightByTitle, inviteUrl, paceInviteRequests } from "../support/invites";
import { gameId, signInOnTouchPhone } from "../support/phone";
import { gameNights, games, players } from "../support/seedData";
import { apiFor, expect, test } from "../support/session";

const outDir = path.join("..", "docs", "src", "assets", "screenshots");

test.describe.configure({ timeout: 90_000 });
test.use({ allowedApiStatuses: [401, 404] });

const releasedVersion = (context: BrowserContext) =>
	context.route("**/api/settings/version-info", (route) =>
		route.fulfill({
			json: {
				currentVersion: "0.3.0",
				latestVersion: "0.3.0",
				updateAvailable: false,
				lastChecked: null,
				errorMessage: null,
			},
		}),
	);

test.beforeAll(async ({ signedInContext }) => {
	await releasedVersion(signedInContext);
});

const shoot = async (page: Page, name: string) => {
	await expect(page.locator("main")).toBeVisible();
	await expect(page.locator("main")).not.toContainText("Loading data...", { timeout: 20_000 });
	await page.waitForLoadState("networkidle");
	await expect
		.poll(() =>
			page.evaluate(() =>
				[...document.images]
					.filter((image) => {
						const rect = image.getBoundingClientRect();
						return rect.width > 0 && rect.height > 0 && rect.bottom > 0 && rect.top < window.innerHeight;
					})
					.some((image) => !image.complete),
			),
		)
		.toBe(false);
	await page.mouse.move(0, 0);
	await page.waitForTimeout(1_500);
	await page.screenshot({ path: path.join(outDir, `${name}.png`) });
};

const pages: [string, string][] = [
	["dashboard", "/"],
	["games", "/games"],
	["games-table", "/games?view=table"],
	["game-detail", "/games/1"],
	["players", "/players"],
	["player-detail", "/players/1"],
	["player-vs-player", "/compare"],
	["leaderboard", "/leaderboard"],
	["shelf-of-shame", "/shames"],
	["game-nights", "/game-nights"],
	["game-night-detail", "/game-nights/1"],
	["loans", "/loans"],
	["location-detail", "/locations/1"],
	["settings-general", "/settings?tab=general"],
];

for (const [name, url] of pages) {
	test(name, async ({ page }) => {
		await page.goto(url);
		await shoot(page, name);
	});
}

const scores = [42, 37, 51, 29];

const pickPlayers = async (page: Page, count: number) => {
	for (const [index, name] of players.slice(0, count).entries()) {
		await page.getByRole("combobox", { name: "Players & scores" }).click();
		await page.getByRole("option", { name: new RegExp(`^${name} `) }).click();
		await page.getByRole("spinbutton", { name: `Score of ${name}` }).fill(String(scores[index]));
	}
};

const sessionGame = games[0].title;

test("log-session", async ({ page }) => {
	await page.goto(`/sessions/new/${gameId(sessionGame)}`);
	await expect(page.getByRole("main")).toBeVisible();
	await pickPlayers(page, 4);
	await shoot(page, "log-session");
});

test("log-session-phone", async ({ browser }, testInfo) => {
	const context = await signInOnTouchPhone(browser, testInfo, credentials);
	await releasedVersion(context);
	const page = await context.newPage();
	await page.goto(`/sessions/new/${gameId(sessionGame)}`);
	await expect(page.getByRole("main")).toBeVisible();
	await pickPlayers(page, 3);
	await shoot(page, "log-session-phone");
	await context.close();
});

test("game-night-rsvps", async ({ page }) => {
	await page.goto("/game-nights/1");
	await page.getByRole("button", { name: "Manage RSVPs" }).first().click();
	await expect(page.getByRole("dialog", { name: "Manage RSVPs" })).toBeVisible();
	await shoot(page, "game-night-rsvps");
});

test("rsvp", async ({ signedInContext, guestPage }) => {
	const night = await gameNightByTitle(await apiFor(signedInContext), gameNights[0].title);
	await paceInviteRequests(guestPage);
	await guestPage.goto(inviteUrl(night.linkId));
	await shoot(guestPage, "rsvp");
});

test("settings-sso", async ({ page }) => {
	await page.goto("/settings?tab=sso");
	await expect(page.getByRole("heading", { name: "Identity provider" })).toBeVisible();
	const field = (name: string) => page.getByRole("textbox", { name, exact: true });
	await field("Button label").fill("Sign in with SSO");
	await field("Authority (issuer URL)").fill("https://auth.example.com/application/o/boardgametracker/");
	await field("Client ID").fill("boardgametracker");
	await shoot(page, "settings-sso");
});
