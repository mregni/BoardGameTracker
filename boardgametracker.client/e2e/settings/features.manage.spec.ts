import { subMonths } from "date-fns";
import type { Settings } from "../../src/models";
import { expect, test } from "../support/fixtures";
import { expected, gameNights, games, sessions } from "../support/seedData";
import {
	api,
	openContext,
	openSection,
	readSettings,
	saveSettings,
	sidebarLink,
	writeSettings,
} from "../support/settings";

let original: Settings;

test.beforeEach(async ({ page }) => {
	original = await readSettings(page);
});

test.afterEach(async ({ page }) => {
	await writeSettings(page, original);
});

test("removes the shelf of shame everywhere when it is switched off", async ({ page, readerPage }) => {
	await page.goto("/");
	await expect(sidebarLink(page, `Shelf of Shame ${expected.shameCount}`)).toHaveAttribute("href", "/shames");

	await openSection(page, "shelf-of-shame");
	await page.getByRole("switch", { name: "Enable Shelf of Shame" }).click();
	await saveSettings(page);

	await expect(sidebarLink(page, /^Shelf of Shame/)).toHaveCount(0);

	await page.goto("/");
	await expect(page.getByRole("region", { name: "Last sessions" })).toBeVisible();
	await expect(page.getByRole("link", { name: /games on the Shelf of Shame/ })).toHaveCount(0);

	await page.goto("/shames");
	await expect(page).toHaveURL("/");

	await readerPage.goto("/");
	await expect(sidebarLink(readerPage, "Dashboard")).toBeVisible();
	await expect(sidebarLink(readerPage, /^Shelf of Shame/)).toHaveCount(0);
});

test("removes game nights everywhere when they are switched off", async ({ page, readerPage }) => {
	await page.goto("/");
	await expect(sidebarLink(page, `Game nights ${gameNights.length}`)).toHaveAttribute("href", "/game-nights");
	await expect(page.getByRole("region", { name: "Next game night" })).toBeVisible();

	await openSection(page, "game-nights");
	await page.getByRole("switch", { name: "Enable game nights" }).click();
	await saveSettings(page);

	await expect(sidebarLink(page, /^Game nights/)).toHaveCount(0);

	await page.goto("/");
	await expect(page.getByRole("region", { name: "Last sessions" })).toBeVisible();
	await expect(page.getByRole("region", { name: "Next game night" })).toHaveCount(0);

	for (const path of ["/game-nights", "/game-nights/1"]) {
		await page.goto(path);
		await expect(page).toHaveURL("/");
	}

	await readerPage.goto("/");
	await expect(sidebarLink(readerPage, "Dashboard")).toBeVisible();
	await expect(sidebarLink(readerPage, /^Game nights/)).toHaveCount(0);
});

test("counts the shelf of shame with a new months threshold", async ({ page }) => {
	const months = 1;
	const since = subMonths(new Date(), months);
	const count = games.filter(
		(game) =>
			game.state === "owned" && !sessions.some((session) => session.game === game.title && session.start >= since),
	).length;
	expect(count).not.toBe(expected.shameCount);

	await openSection(page, "shelf-of-shame");
	await page.getByRole("spinbutton", { name: "Months threshold" }).fill(String(months));
	await saveSettings(page);

	await page.goto("/");
	await expect(sidebarLink(page, `Shelf of Shame ${count}`)).toBeVisible();

	await openSection(page, "shelf-of-shame");
	await expect(page.getByRole("spinbutton", { name: "Months threshold" })).toHaveValue(String(months));
});

test("refuses a months threshold below one", async ({ page }) => {
	await openSection(page, "shelf-of-shame");
	const months = page.getByRole("spinbutton", { name: "Months threshold" });

	await months.fill("0");
	await expect(months).toHaveAttribute("aria-invalid", "true");
	const saved = page.waitForRequest((request) => request.method() === "PUT", { timeout: 1000 }).catch(() => null);
	await page.getByRole("button", { name: "Save settings" }).click();

	expect(await saved).toBeNull();
	expect((await readSettings(page)).shelfOfShameMonthsLimit).toBe(original.shelfOfShameMonthsLimit);
});

test("explains the months threshold in plain words", async ({ page }) => {
	test.fail(true, "BUG: a too low months threshold shows the raw validator text instead of a translated message");
	await openSection(page, "shelf-of-shame");

	const months = page.getByRole("spinbutton", { name: "Months threshold" });

	await months.fill("0");

	await expect(months).toHaveAttribute("aria-invalid", "true");
	await expect(page.getByText(/expected number/)).toHaveCount(0, { timeout: 1000 });
});

test("asks a visitor to sign in for an rsvp when that is switched on", async ({ page, browser, baseURL }) => {
	const { gameNight } = await (await (await api(page)).get("/api/gamenight/1")).json();
	const context = await openContext(browser, baseURL);
	const visitor = await context.newPage();

	await visitor.goto(`/rsvp?linkId=${gameNight.linkId}`);
	await expect(visitor.getByRole("heading", { name: gameNight.title })).toBeVisible();
	await expect(visitor.getByText("Authentication Required")).toHaveCount(0);

	await openSection(page, "game-nights");
	await page.getByRole("switch", { name: "Require authentication for RSVPs" }).click();
	await saveSettings(page);

	await visitor.goto(`/rsvp?linkId=${gameNight.linkId}`);
	await expect(visitor.getByText("Authentication Required")).toBeVisible();
	await expect(visitor.getByText("You need to sign in to respond to this invitation.")).toBeVisible();
	await expect(visitor.getByRole("heading", { name: gameNight.title })).toHaveCount(0);

	await context.close();
});

test("keeps the update check and version track after saving", async ({ page }) => {
	await openSection(page, "advanced");
	await page.getByRole("switch", { name: "Enable update check" }).click();
	await page.getByRole("button", { name: /^Beta / }).click();
	await saveSettings(page);

	const saved = await readSettings(page);
	expect(saved.updateCheckEnabled).toBe(false);
	expect(saved.versionTrack).toBe("beta");

	await openSection(page, "advanced");
	await expect(page.getByRole("switch", { name: "Enable update check" })).not.toBeChecked();
	await expect(page.getByRole("button", { name: /^Beta / })).toHaveAttribute("aria-pressed", "true");
});
