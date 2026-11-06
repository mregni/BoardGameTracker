import type { Browser, Page } from "@playwright/test";
import type { Settings } from "../../src/models";
import { expect, test } from "../support/fixtures";
import { api, expectingFailures, openContext, openSection, readSettings, writeSettings } from "../support/settings";

test.use({ allowedApiStatuses: [401, 404] });

const localUrl = "http://localhost:9";

const trackSaves = (page: Page) => {
	const saves: { bggApiKey: string | null; changeDetectionApiKey: string | null }[] = [];
	page.on("request", (request) => {
		if (request.method() === "PUT" && new URL(request.url()).pathname === "/api/settings") {
			saves.push(request.postDataJSON());
		}
	});
	return saves;
};

const visitorSettings = async (browser: Browser, baseURL: string | undefined): Promise<Settings> => {
	const context = await openContext(browser, baseURL);
	const settings = await (await context.request.get("/api/settings")).json();
	await context.close();
	return settings;
};

let original: Settings;

test.beforeEach(async ({ page }) => {
	original = await readSettings(page);
});

test.afterEach(async ({ page }) => {
	await writeSettings(page, { ...original, bggApiKey: null, changeDetectionApiKey: null, changeDetectionBaseUrl: "" });
});

test("saves a key without showing it and removes it again, with the bgg check mocked", async ({ page }) => {
	await page.route("**/api/settings/integrations/status", (route) =>
		route.fulfill({
			json: [
				{ integration: "bgg", status: "unauthorized", messageKey: "unauthorized", checkedAt: new Date().toISOString() },
				{
					integration: "changeDetection",
					status: "notConfigured",
					messageKey: "not-configured",
					checkedAt: new Date().toISOString(),
				},
			],
		}),
	);
	await openSection(page, "integrations");
	await page.getByRole("textbox", { name: "BGG API key" }).fill("secret-bgg-key");

	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Settings saved", { exact: true })).toBeVisible();
	await expect(page.getByText("Settings saved, but BoardGameGeek isn't connected")).toBeVisible();

	const saved = await readSettings(page);
	expect(saved.bggStatus.isConfigured).toBe(true);
	expect(saved.bggApiKey).toBe("");

	await openSection(page, "integrations");
	await expect(page.getByRole("textbox", { name: "BGG API key" })).toHaveValue("");
	await expect(page.getByRole("button", { name: "Test connection" }).first()).toBeEnabled();

	await page.getByRole("button", { name: "Remove API key" }).click();
	await page.getByRole("button", { name: "Save settings" }).click();

	await expect.poll(async () => (await readSettings(page)).bggStatus.isConfigured).toBe(false);
	await expect(page.getByRole("button", { name: "Remove API key" })).toHaveCount(0);
});

test("keeps a saved key when the field stays empty", async ({ page, browser, baseURL }) => {
	await page.route("**/api/settings/integrations/status", (route) => route.fulfill({ json: [] }));
	await writeSettings(page, { ...original, bggApiKey: "secret-bgg-key" });
	await openSection(page, "integrations");

	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Settings saved")).toBeVisible();
	expect((await readSettings(page)).bggStatus.isConfigured).toBe(true);
	expect((await visitorSettings(browser, baseURL)).bggApiKey).toBe("");
});

test("refuses a changedetection address that is not http(s)", async ({ page }) => {
	const saves = trackSaves(page);
	await openSection(page, "integrations");

	await page.getByRole("textbox", { name: "Base URL" }).fill("ftp://changes.example.test");
	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Enter a valid http(s) URL")).toBeVisible();
	expect(saves).toEqual([]);
});

expectingFailures("refuses plain http for a changedetection address on another machine", async ({ page }) => {
	await openSection(page, "integrations");

	await page.getByRole("textbox", { name: "Base URL" }).fill("http://changes.example.test");
	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Use an https address for changedetection.io", { exact: false })).toBeVisible();
	expect((await readSettings(page)).changeDetectionBaseUrl).toBe("");
});

test("reports a changedetection service that does not answer, and hides the address from others", async ({
	page,
	readerPage,
	browser,
	baseURL,
}) => {
	await openSection(page, "integrations");
	await page.getByRole("textbox", { name: "Base URL" }).fill(localUrl);
	await page.getByRole("textbox", { name: "API key", exact: true }).fill("secret-change-key");

	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Settings saved, but changedetection.io isn't connected")).toBeVisible();

	await openSection(page, "integrations");
	await expect(page.getByRole("textbox", { name: "Base URL" })).toHaveValue(localUrl);
	await expect(page.getByRole("textbox", { name: "API key", exact: true })).toHaveValue("");

	const test = page.getByRole("button", { name: "Test connection" }).nth(1);
	await expect(test).toBeEnabled();
	await test.click();

	const badge = page.getByRole("status").nth(1);
	await expect(badge).toContainText("Unreachable");
	await expect(badge).toContainText("The service could not be reached");

	const settings = await (await (await api(readerPage)).get("/api/settings")).json();
	expect(settings.changeDetectionBaseUrl).toBe("");
	expect(settings.changeDetectionApiKey).toBe("");

	const visitor = await visitorSettings(browser, baseURL);
	expect(visitor.changeDetectionBaseUrl).toBe("");
	expect(visitor.changeDetectionApiKey).toBe("");
});
