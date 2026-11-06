import { expect, test } from "../support/fixtures";
import { linkedPlayer } from "../support/seedData";
import { mockSettings, openSection, type SettingsTab, sidebarLink } from "../support/settings";

const sections: { title: string; description: string; tab: SettingsTab; heading: string }[] = [
	{ title: "General", description: "Language, formats, public URL", tab: "general", heading: "Language and formats" },
	{ title: "Account", description: "Profile & user management", tab: "account", heading: "My account" },
	{
		title: "Integrations",
		description: "BoardGameGeek & price watch",
		tab: "integrations",
		heading: "BoardGameGeek API",
	},
	{
		title: "Shelf of Shame",
		description: "When a game counts as unplayed",
		tab: "shelf-of-shame",
		heading: "Shelf of Shame",
	},
	{ title: "Game nights", description: "Planning and RSVPs", tab: "game-nights", heading: "Game nights" },
	{ title: "Single sign-on", description: "Identity provider", tab: "sso", heading: "Identity provider" },
	{ title: "Advanced", description: "Updates and data reset", tab: "advanced", heading: "Update check" },
];

test.use({ allowedApiStatuses: [401, 404] });

test("lists every section for an admin and opens each one", async ({ page }) => {
	await page.goto("/settings");
	const nav = page.getByRole("main").getByRole("navigation");

	await expect(nav.getByRole("button")).toHaveCount(sections.length);
	await expect(page.getByRole("heading", { name: "Language and formats" })).toBeVisible();

	for (const [index, section] of sections.entries()) {
		const button = nav.getByRole("button").nth(index);
		await expect(button).toHaveAccessibleName(`${section.title} ${section.description}`);
		await button.click();
		await expect(page).toHaveURL(`/settings?tab=${section.tab}`);
		await expect(page.getByRole("heading", { name: section.heading, level: 2 })).toBeVisible();
	}
});

test("falls back to general for an unknown section and keeps the old integrations address", async ({ page }) => {
	await page.goto("/settings?tab=nonsense");
	await expect(page.getByRole("heading", { name: "Language and formats" })).toBeVisible();

	await page.goto("/settings?tab=bgg");
	await expect(page.getByRole("heading", { name: "BoardGameGeek API" })).toBeVisible();
});

test("shows the current general settings", async ({ page, baseURL }) => {
	await openSection(page, "general");

	await expect(page.getByRole("combobox", { name: "Language" })).toHaveText("English");
	await expect(page.getByRole("textbox", { name: "Currency" })).toHaveValue("€");
	await expect(page.getByRole("textbox", { name: "Date format" })).toHaveValue("dd-MM-yyyy");
	await expect(page.getByRole("textbox", { name: "Time format" })).toHaveValue("HH:mm");
	await expect(page.getByRole("textbox", { name: "Public URL" })).toHaveValue(baseURL ?? "");
	await expect(page.getByRole("textbox", { name: "Public URL" })).toBeDisabled();
	await expect(
		page.getByText("Set by the environment variable PUBLIC_URL. Change it there to update this setting."),
	).toBeVisible();

	await page.getByRole("combobox", { name: "Language" }).click();
	await expect(page.getByRole("option")).toHaveText([
		"English",
		"Español",
		"Nederlands (België)",
		"Nederlands (Nederland)",
	]);
});

test("disables the months threshold while the shelf of shame is switched off", async ({ page }) => {
	await openSection(page, "shelf-of-shame");
	const enabled = page.getByRole("switch", { name: "Enable Shelf of Shame" });
	const months = page.getByRole("spinbutton", { name: "Months threshold" });

	await expect(enabled).toBeChecked();
	await expect(months).toHaveValue("6");
	await expect(months).toBeEnabled();

	await enabled.click();

	await expect(enabled).not.toBeChecked();
	await expect(months).toBeDisabled();
});

test("disables the rsvp switch while game nights are switched off", async ({ page }) => {
	await openSection(page, "game-nights");
	const enabled = page.getByRole("switch", { name: "Enable game nights" });
	const rsvp = page.getByRole("switch", { name: "Require authentication for RSVPs" });

	await expect(enabled).toBeChecked();
	await expect(rsvp).not.toBeChecked();
	await expect(rsvp).toBeEnabled();

	await enabled.click();

	await expect(rsvp).toBeDisabled();
});

test("shows the update settings and opens the project links", async ({ page }) => {
	await openSection(page, "advanced");

	await expect(page.getByRole("switch", { name: "Enable update check" })).toBeChecked();
	await expect(page.getByRole("button", { name: /^Stable / })).toHaveAttribute("aria-pressed", "true");
	await expect(page.getByRole("button", { name: /^Beta / })).toHaveAttribute("aria-pressed", "false");

	await page.getByRole("button", { name: /^Beta / }).click();
	await expect(page.getByRole("button", { name: /^Beta / })).toHaveAttribute("aria-pressed", "true");
	await expect(page.getByRole("button", { name: /^Stable / })).toHaveAttribute("aria-pressed", "false");

	await page.evaluate(() => {
		const opened: string[] = [];
		Object.assign(window, { opened });
		window.open = (url) => {
			opened.push(String(url));
			return null;
		};
	});
	await page.getByRole("button", { name: "Report a bug" }).click();
	await page.getByRole("button", { name: "Help us translate the app" }).click();

	expect(await page.evaluate(() => (window as unknown as { opened: string[] }).opened)).toEqual([
		"https://github.com/mregni/BoardGameTracker/issues",
		"https://crowdin.com/project/boardgametracker",
	]);
});

test("shows the running version in the sidebar", async ({ page }) => {
	await page.route("**/api/settings/version-info", (route) =>
		route.fulfill({ json: { currentVersion: "1.2.3", latestVersion: "1.2.3", updateAvailable: false } }),
	);
	await page.goto("/settings");

	await expect(page.getByRole("complementary").getByText("v1.2.3", { exact: true })).toBeVisible();
	await expect(page.getByRole("complementary").getByText("New version available")).toHaveCount(0);
});

test("announces a new version in the sidebar", async ({ page }) => {
	await page.route("**/api/settings/version-info", (route) =>
		route.fulfill({ json: { currentVersion: "1.0.0", latestVersion: "9.9.9", updateAvailable: true } }),
	);
	await page.goto("/settings");

	await expect(page.getByRole("complementary").getByText("New version available")).toBeVisible();
	await expect(page.getByRole("complementary").getByText("v9.9.9")).toBeVisible();
});

test("shows the rules chat only when the server has AI switched on", async ({ page }) => {
	await page.goto("/settings");
	await page.waitForLoadState("networkidle");
	await expect(sidebarLink(page, "Settings")).toBeVisible();
	await expect(sidebarLink(page, "Rules chat")).toHaveCount(0);

	await mockSettings(page, { ragEnabled: true });
	await page.goto("/settings");

	await expect(sidebarLink(page, "Rules chat")).toHaveAttribute("href", "/chat");
});

test.describe("on a phone", () => {
	test.beforeEach(async ({ page }) => {
		await page.setViewportSize({ width: 390, height: 844 });
	});

	test("switches sections with a select", async ({ page }) => {
		await page.goto("/settings");
		const select = page.getByRole("combobox", { name: "Settings" });

		await select.click();
		await expect(page.getByRole("option")).toHaveText(sections.map((section) => section.title));

		await page.getByRole("option", { name: "Game nights" }).click();

		await expect(page).toHaveURL("/settings?tab=game-nights");
		await expect(page.getByRole("heading", { name: "Game nights", level: 2 })).toBeVisible();
	});

	test("lists users as cards with an action menu", async ({ page }) => {
		await openSection(page, "account");
		const cards = page.getByTestId("user-card");

		await expect(cards).toHaveCount(3);
		const admin = cards.filter({ hasText: "admin" });
		await expect(admin).toContainText("Admin");
		await expect(admin).toContainText(`Player: ${linkedPlayer}Last login`);

		await admin.getByRole("button", { name: "Actions for admin" }).click();
		await expect(page.getByRole("menuitem")).toHaveText(["Edit", "Access", "Reset password"]);
		await page.keyboard.press("Escape");

		await cards.filter({ hasText: "reader" }).getByRole("button", { name: "Actions for reader" }).click();
		await expect(page.getByRole("menuitem")).toHaveText(["Edit", "Access", "Reset password", "Delete"]);
	});
});

test("locks every field that the environment sets", async ({ page }) => {
	await mockSettings(page, {
		environmentOverrides: {
			uiLanguage: "UILANGUAGE",
			currency: "CURRENCY",
			dateFormat: "DATEFORMAT",
			timeFormat: "TIMEFORMAT",
			shelfOfShameEnabled: "SHELFOFSHAMEENABLED",
			shelfOfShameMonthsLimit: "SHELFOFSHAMEMONTHS",
			gameNightsEnabled: "GAMENIGHTSENABLED",
			rsvpAuthenticationEnabled: "RSVPAUTHENTICATIONENABLED",
			updateCheckEnabled: "UPDATECHECKENABLED",
			versionTrack: "VERSIONTRACK",
		},
	});
	const hint = (variable: string) =>
		page.getByText(`Set by the environment variable ${variable}. Change it there to update this setting.`);

	await openSection(page, "general");
	await expect(page.getByRole("combobox", { name: "Language" })).toBeDisabled();
	await expect(page.getByRole("textbox", { name: "Currency" })).toBeDisabled();
	await expect(page.getByRole("textbox", { name: "Date format" })).toBeDisabled();
	await expect(page.getByRole("textbox", { name: "Time format" })).toBeDisabled();
	for (const variable of ["UILANGUAGE", "CURRENCY", "DATEFORMAT", "TIMEFORMAT"]) {
		await expect(hint(variable)).toBeVisible();
	}
	await expect(page.getByRole("textbox", { name: "Public URL" })).toBeEnabled();

	await openSection(page, "shelf-of-shame");
	await expect(page.getByRole("switch", { name: "Enable Shelf of Shame" })).toBeDisabled();
	await expect(page.getByRole("spinbutton", { name: "Months threshold" })).toBeDisabled();
	await expect(hint("SHELFOFSHAMEENABLED")).toBeVisible();
	await expect(hint("SHELFOFSHAMEMONTHS")).toBeVisible();

	await openSection(page, "game-nights");
	await expect(page.getByRole("switch", { name: "Enable game nights" })).toBeDisabled();
	await expect(page.getByRole("switch", { name: "Require authentication for RSVPs" })).toBeDisabled();
	await expect(hint("GAMENIGHTSENABLED")).toBeVisible();
	await expect(hint("RSVPAUTHENTICATIONENABLED")).toBeVisible();

	await openSection(page, "advanced");
	await expect(page.getByRole("switch", { name: "Enable update check" })).toBeDisabled();
	await expect(page.getByRole("button", { name: /^Stable / })).toBeDisabled();
	await expect(page.getByRole("button", { name: /^Beta / })).toBeDisabled();
	await expect(hint("UPDATECHECKENABLED")).toBeVisible();
	await expect(hint("VERSIONTRACK")).toBeVisible();
});

test("hides account, single sign-on and the danger zone when the server runs without sign-in", async ({ page }) => {
	await page.route("**/api/auth/status", (route) => route.fulfill({ json: { authEnabled: false } }));
	await openSection(page, "advanced");
	const nav = page.getByRole("main").getByRole("navigation");

	await expect(page.getByRole("heading", { name: "Update check" })).toBeVisible();
	await expect(nav.getByRole("button")).toHaveCount(sections.length - 2);
	await expect(nav.getByRole("button", { name: /^Account/ })).toHaveCount(0);
	await expect(nav.getByRole("button", { name: /^Single sign-on/ })).toHaveCount(0);
	await expect(page.getByRole("heading", { name: "Danger zone" })).toHaveCount(0);
});
