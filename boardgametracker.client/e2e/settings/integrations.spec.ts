import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { mockSettings, openSection } from "../support/settings";

const checkedAt = "2026-03-01T10:15:00Z";
const checkedLocally = "11:15";
const configured = { isConfigured: true, source: "database", isReadOnly: false };

const status = (integration: "bgg" | "changeDetection", state: string, messageKey: string) => ({
	integration,
	status: state,
	messageKey,
	checkedAt,
});

const mockStatuses = (page: Page, bgg: unknown, changeDetection: unknown) =>
	page.route("**/api/settings/integrations/status", (route) => route.fulfill({ json: [bgg, changeDetection] }));

const notConfigured = status("changeDetection", "notConfigured", "not-configured");

test("shows both integrations as not configured", async ({ page }) => {
	await openSection(page, "integrations");
	const statuses = page.getByRole("status");
	const tests = page.getByRole("button", { name: "Test connection" });

	await expect(statuses).toHaveText(["Not configured", "Not configured"]);
	await expect(tests.nth(0)).toBeDisabled();
	await expect(tests.nth(0)).toHaveAttribute("title", "Save an API key to test the connection");
	await expect(tests.nth(1)).toBeDisabled();
	await expect(tests.nth(1)).toHaveAttribute("title", "Save the base URL and an API key to test the connection");
	await expect(page.getByRole("button", { name: "Remove API key" })).toHaveCount(0);
	await expect(page.getByRole("textbox", { name: "BGG API key" })).toHaveValue("");
	await expect(page.getByRole("textbox", { name: "BGG API key" })).toHaveAttribute("type", "password");
	await expect(page.getByRole("textbox", { name: "Base URL" })).toHaveValue("");
	await expect(page.getByRole("textbox", { name: "API key", exact: true })).toHaveAttribute("type", "password");
	await expect(page.getByRole("link", { name: "request page" })).toHaveAttribute(
		"href",
		"https://boardgamegeek.com/applications",
	);
});

for (const [state, label, messageKey, reason] of [
	["unauthorized", "Rejected", "unauthorized", "The API key was rejected"],
	["unreachable", "Unreachable", "unreachable", "The service could not be reached"],
	["unreachable", "Unreachable", "timeout", "The service did not answer within 10 seconds"],
	["rateLimited", "Rate limited", "rate-limited", "Too many requests, try again in a few minutes"],
	["unreachable", "Unreachable", "misconfigured", "The base URL or API key is not valid"],
	["unreachable", "Unreachable", "unexpected-response", "The service sent an unexpected response"],
]) {
	test(`shows ${label} with the reason ${messageKey}`, async ({ page }) => {
		await mockSettings(page, { bggStatus: configured });
		await mockStatuses(page, status("bgg", state, messageKey), notConfigured);
		await openSection(page, "integrations");

		const badge = page.getByRole("status").first();
		await expect(badge).toContainText(label);
		await expect(badge).toContainText(`checked ${checkedLocally}`);
		await expect(badge).toContainText(reason);
	});
}

test("shows a working connection without a reason", async ({ page }) => {
	await mockSettings(page, { bggStatus: configured });
	await mockStatuses(page, status("bgg", "ok", "ok"), notConfigured);
	await openSection(page, "integrations");

	await expect(page.getByRole("status").first()).toHaveText(`Connected checked ${checkedLocally}`, {
		useInnerText: true,
	});
});

test("tests the connection again and shows the new result", async ({ page }) => {
	await mockSettings(page, { bggStatus: configured });
	await mockStatuses(page, status("bgg", "ok", "ok"), notConfigured);
	let answer = () => {};
	const answered = new Promise<void>((resolve) => {
		answer = resolve;
	});
	await page.route("**/api/settings/integrations/bgg/test", async (route) => {
		await answered;
		await route.fulfill({ json: status("bgg", "unauthorized", "unauthorized") });
	});
	await openSection(page, "integrations");
	const badge = page.getByRole("status").first();
	await expect(badge).toContainText("Connected");

	await page.getByRole("button", { name: "Test connection" }).first().click();

	await expect(page.getByRole("button", { name: "Testing…" })).toBeDisabled();
	await expect(badge).toContainText("Checking…");

	answer();

	await expect(badge).toContainText("Rejected");
	await expect(badge).toContainText("The API key was rejected");
	await expect(page.getByRole("button", { name: "Test connection" }).first()).toBeEnabled();
});

test("reports when the connection test cannot run", async ({ page }) => {
	await mockSettings(page, { bggStatus: configured });
	await mockStatuses(page, status("bgg", "ok", "ok"), notConfigured);
	await page.route("**/api/settings/integrations/bgg/test", (route) => route.abort());
	await openSection(page, "integrations");

	await page.getByRole("button", { name: "Test connection" }).first().click();

	await expect(page.getByText("Couldn't run the connection test")).toBeVisible();
	await expect(page.getByRole("status").first()).toContainText("Connected");
});

test("offers to remove a saved key and waits for save", async ({ page }) => {
	await mockSettings(page, { bggStatus: configured, changeDetectionStatus: configured });
	await mockStatuses(page, status("bgg", "ok", "ok"), status("changeDetection", "ok", "ok"));
	await openSection(page, "integrations");
	const remove = page.getByRole("button", { name: "Remove API key" });

	await expect(remove).toHaveCount(2);
	await expect(page.getByText("Save settings to apply the removal.")).toHaveCount(0);

	await remove.nth(0).click();
	await expect(page.getByText("Save settings to apply the removal.")).toHaveCount(1);

	await remove.nth(1).click();
	await expect(page.getByText("Save settings to apply the removal.")).toHaveCount(2);
});

test("hides the key field when the environment sets the key", async ({ page }) => {
	await mockSettings(page, { bggStatus: { isConfigured: true, source: "environment", isReadOnly: true } });
	await mockStatuses(page, status("bgg", "ok", "ok"), notConfigured);
	await openSection(page, "integrations");

	await expect(page.getByText("The API key is read only.")).toBeVisible();
	await expect(
		page.getByText("The API key is managed via the BGG_API_KEY environment variable and cannot be changed here."),
	).toBeVisible();
	await expect(page.getByRole("textbox", { name: "BGG API key" })).toHaveCount(0);
	await expect(page.getByRole("button", { name: "Test connection" }).first()).toBeEnabled();
});
