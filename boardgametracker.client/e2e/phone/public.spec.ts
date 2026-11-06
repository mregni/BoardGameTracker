import type { Browser, TestInfo } from "@playwright/test";
import { expect, paceLogin, readerCredentials, test } from "../support/fixtures";
import { apiFor, expectInsideScreen, expectNoSidewaysScroll, phone, smallPhone } from "../support/phone";
import { gameNights } from "../support/seedData";

const signedOutPhone = async (browser: Browser, info: TestInfo) => {
	const { baseURL, locale, timezoneId } = info.project.use;
	const context = await browser.newContext({ baseURL, locale, timezoneId, viewport: phone });
	return { context, page: await context.newPage() };
};

test("the login page fits a phone and a small phone", async ({ browser }, info) => {
	const { context, page } = await signedOutPhone(browser, info);
	await page.goto("/login");

	for (const size of [phone, smallPhone]) {
		await page.setViewportSize(size);
		for (const locator of [
			page.getByLabel("Username"),
			page.getByLabel("Password"),
			page.getByRole("button", { name: "Sign in" }),
		]) {
			await expect(locator).toBeVisible();
			await expectInsideScreen(page, locator);
		}
		await expectNoSidewaysScroll(page);
	}
	await expect(page.getByRole("navigation")).toHaveCount(0);
	await context.close();
});

test("a signed out phone goes to the login page and back after signing in", async ({ browser }, info) => {
	const { context, page } = await signedOutPhone(browser, info);

	await page.goto("/loans");
	await expect(page).toHaveURL(/\/login\?redirect=.*loans/);

	await page.getByLabel("Username").fill(readerCredentials.username);
	await page.getByLabel("Password").fill(readerCredentials.password);
	await paceLogin();
	await page.keyboard.press("Enter");

	await expect(page).toHaveURL("/loans");
	await expect(page.getByRole("heading", { name: "Loans", level: 1 })).toBeVisible();
	await context.close();
});

test("logging out from the More menu closes the protected pages", async ({ browser }, info) => {
	const { context, page } = await signedOutPhone(browser, info);
	await page.goto("/login");
	await page.getByLabel("Username").fill(readerCredentials.username);
	await page.getByLabel("Password").fill(readerCredentials.password);
	await paceLogin();
	await page.getByRole("button", { name: "Sign in" }).click();
	await expect(page.getByRole("navigation")).toBeVisible();

	await page.getByRole("button", { name: "More" }).click();
	await page.getByRole("button", { name: "Log out" }).click();

	await expect(page).toHaveURL("/login");
	await expect(page.getByRole("navigation")).toHaveCount(0);

	for (const path of ["/", "/games", "/settings", "/players/1"]) {
		await page.goto(path);
		await expect(page).toHaveURL(/\/login/);
		await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
	}
	await context.close();
});

test("the RSVP page fits a phone and shows the night", async ({ browser, page: adminPage }, info) => {
	const api = await apiFor(adminPage);
	const nights = await api.get<{ title: string; linkId: string }[]>("/api/gamenight");
	const night = nights.find((x) => x.title === gameNights[0].title);
	const { context, page } = await signedOutPhone(browser, info);

	await page.goto(`/rsvp?linkId=${night?.linkId}`);

	await expect(page.getByRole("heading", { name: gameNights[0].title, level: 1 })).toBeVisible();
	await expectNoSidewaysScroll(page);
	await page.setViewportSize(smallPhone);
	await expectNoSidewaysScroll(page);
	await context.close();
});

test("the RSVP page explains an unknown link on a phone", async ({ browser }, info) => {
	const { context, page } = await signedOutPhone(browser, info);

	await page.goto("/rsvp?linkId=00000000-0000-0000-0000-000000000000");

	await expect(page.getByRole("heading", { name: "This invite link doesn't work", level: 1 })).toBeVisible();
	await expectNoSidewaysScroll(page);
	await context.close();
});
