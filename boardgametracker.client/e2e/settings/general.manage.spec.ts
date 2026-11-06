import { readFileSync } from "node:fs";
import type { Page } from "@playwright/test";
import type { Settings } from "../../src/models";
import { expect, test } from "../support/fixtures";
import { displayDate } from "../support/format";
import { openGames, pick } from "../support/gamesPage";
import { expected, games } from "../support/seedData";
import {
	api,
	expectingFailures,
	mockSettings,
	openSection,
	readSettings,
	saveSettings,
	sidebarLink,
	writeSettings,
} from "../support/settings";

const money = (value: number) => value.toLocaleString("en-US");
const field = (page: Page, name: string) => page.getByRole("textbox", { name, exact: true });
const common = (language: string) =>
	JSON.parse(readFileSync(`public/locales/${language}/common.json`, "utf8")) as Record<string, string>;

const trackSaves = (page: Page) => {
	const saves: unknown[] = [];
	page.on("request", (request) => {
		if (request.method() === "PUT" && new URL(request.url()).pathname === "/api/settings") {
			saves.push(request.postDataJSON());
		}
	});
	return saves;
};

let original: Settings;

test.beforeEach(async ({ page }) => {
	original = await readSettings(page);
});

test.afterEach(async ({ page }) => {
	await writeSettings(page, original);
});

test("shows a new currency on the dashboard and the games page", async ({ page }) => {
	await openSection(page, "general");
	await field(page, "Currency").fill("$");
	await saveSettings(page);

	await page.goto("/");
	await expect(page.getByRole("main").getByText(`$${money(expected.ownedValue)}`, { exact: true })).toBeVisible();
	await expect(page.getByRole("link", { name: `${expected.shameCount} games on the Shelf of Shame` })).toContainText(
		`$${money(expected.shameValue)} paid`,
	);

	await openGames(page);
	await page.getByRole("button", { name: "Table" }).click();
	await expect(page.getByText(`Total price (owned): $${money(expected.ownedValue)}`)).toBeVisible();

	await openSection(page, "general");
	await expect(field(page, "Currency")).toHaveValue("$");
});

test("shows a new date format on the dashboard and a game page", async ({ page }) => {
	const [session] = expected.lastSessions;
	const slashed = displayDate(session.start).split("-").reverse().join("/");
	const gameId = games.findIndex((game) => game.title === session.game) + 1;

	await openSection(page, "general");
	await field(page, "Date format").fill("yyyy/MM/dd");
	await saveSettings(page);

	await page.goto("/");
	await expect(page.getByRole("region", { name: "Last sessions" }).getByRole("listitem").first()).toContainText(
		`${slashed} ·`,
	);

	await page.goto(`/games/${gameId}/sessions`);
	await expect(page.getByRole("main")).toContainText(slashed);
	await expect(page.getByRole("main")).not.toContainText(displayDate(session.start));
});

test("shows a new time format on the dashboard", async ({ page }) => {
	const { gameNight } = await (await (await api(page)).get("/api/gamenight/1")).json();
	const clock = new Intl.DateTimeFormat("en-US", {
		timeZone: "Europe/Brussels",
		hour: "2-digit",
		minute: "2-digit",
		hour12: true,
	})
		.format(new Date(gameNight.startDate))
		.replace(/\s/, " ");

	await openSection(page, "general");
	await field(page, "Time format").fill("hh:mm a");
	await saveSettings(page);

	await page.goto("/");
	await expect(page.getByRole("region", { name: "Next game night" })).toContainText(clock);
});

for (const [name, language] of [
	["Nederlands (België)", "nl-BE"],
	["Nederlands (Nederland)", "nl-NL"],
	["Español", "es-ES"],
]) {
	test(`switches the interface to ${name} and back`, async ({ page }) => {
		const english = common("en-US");
		const translated = common(language);
		expect(translated.games).not.toBe(english.games);

		await openSection(page, "general");
		await pick(page, "Language", name);
		await page.getByRole("button", { name: "Save settings" }).click();

		await expect(sidebarLink(page, new RegExp(`^${translated.games}`))).toBeVisible();

		await page.goto("/games");
		await expect(sidebarLink(page, new RegExp(`^${translated.games}`))).toBeVisible();
		await expect(sidebarLink(page, new RegExp(`^${english.games}`))).toHaveCount(0);

		await page.goto("/settings");
		await expect(page.getByRole("combobox").first()).toHaveText(name);

		await writeSettings(page, original);
		await page.goto("/settings");
		await expect(sidebarLink(page, new RegExp(`^${english.games}`))).toBeVisible();
	});
}

test("refuses empty and invalid regional fields without saving", async ({ page }) => {
	const saves = trackSaves(page);
	await openSection(page, "general");

	for (const [name, value, message] of [
		["Currency", "", "Currency is required"],
		["Date format", "", "Date format is required"],
		["Date format", "not a date", "Use a date pattern such as dd/MM/yyyy or yyyy-MM-dd"],
		["Time format", "", "Time format is required"],
		["Time format", "not a time", "Use a time pattern such as HH:mm or h:mm a"],
	]) {
		const input = field(page, name);
		const before = await input.inputValue();
		await input.fill(value);
		await page.getByRole("button", { name: "Save settings" }).click();
		await expect(page.getByText(message)).toBeVisible();
		await input.fill(before);
		await expect(page.getByText(message)).toHaveCount(0);
	}

	expect(saves).toEqual([]);
});

expectingFailures("keeps the input and says so when saving fails", async ({ page }) => {
	await page.route("**/api/settings", (route) =>
		route.request().method() === "PUT"
			? route.fulfill({ status: 400, json: { title: "error.test.unknown" } })
			: route.fallback(),
	);
	await openSection(page, "general");
	await field(page, "Currency").fill("$");

	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Failed saving settings")).toBeVisible();
	await expect(field(page, "Currency")).toHaveValue("$");
	expect((await readSettings(page)).currency).toBe(original.currency);
});

expectingFailures("does not leave an unhandled rejection when saving fails", async ({ page }) => {
	const errors: string[] = [];
	page.on("pageerror", (error) => errors.push(error.message));
	await page.route("**/api/settings", (route) =>
		route.request().method() === "PUT"
			? route.fulfill({ status: 400, json: { title: "error.test.unknown" } })
			: route.fallback(),
	);
	await openSection(page, "general");

	await page.getByRole("button", { name: "Save settings" }).click();

	await expect(page.getByText("Failed saving settings")).toBeVisible();
	expectingFailures.fail(
		true,
		"BUG: a refused save rejects unhandled in the form submit, so the page reports an error",
	);
	expect(errors).toEqual([]);
});

test.describe("with a public url that is not set by the environment", () => {
	const url = (page: Page) => field(page, "Public URL");
	const provider = {
		id: 1,
		displayName: "Company login",
		enabled: true,
	};

	test.beforeEach(async ({ page }) => {
		await mockSettings(page, { publicUrl: "", environmentOverrides: {} });
		await page.route("**/api/settings", (route) =>
			route.request().method() === "PUT"
				? route.fulfill({ json: { ...original, publicUrl: route.request().postDataJSON().publicUrl } })
				: route.fallback(),
		);
	});

	test("explains an empty address", async ({ page }) => {
		await openSection(page, "general");

		await expect(url(page)).toBeEnabled();
		await expect(page.getByTestId("public-url-not-set")).toHaveText(
			"Not set. Invitation and password-reset emails are not sent, and single sign-on uses the address you open the app on.",
		);
		await expect(page.getByText(/^Set by the environment variable/)).toHaveCount(0);
	});

	test("refuses an address that is not http(s)", async ({ page }) => {
		const saves = trackSaves(page);
		await openSection(page, "general");

		await url(page).fill("ftp://games.example.com");
		await page.getByRole("button", { name: "Save settings" }).click();

		await expect(page.getByText("Enter a valid http(s) URL")).toBeVisible();
		expect(saves).toEqual([]);
	});

	test("warns about another host than the current one", async ({ page, baseURL }) => {
		await openSection(page, "general");

		await url(page).fill("https://games.example.com");

		await expect(page.getByRole("alert")).toContainText(
			`The public URL points to a different host than ${new URL(baseURL ?? "").host}.`,
		);

		await url(page).fill(baseURL ?? "");
		await expect(page.getByRole("alert")).toHaveCount(0);
	});

	test("asks before an http address stops single sign-on", async ({ page }) => {
		const saves = trackSaves(page);
		await page.route("**/api/admin/oidc-providers", (route) => route.fulfill({ json: [provider] }));
		await openSection(page, "general");
		await url(page).fill("http://games.example.com");
		await expect(page.getByText("Single sign-on stops working with an http address.")).toBeVisible();

		await page.getByRole("button", { name: "Save settings" }).click();
		const dialog = page.getByRole("dialog");
		await expect(dialog.getByRole("heading", { name: "Stop single sign-on?" })).toBeVisible();
		await expect(dialog).toContainText(
			"Single sign-on stops for everyone, on every address, until the public URL is https again.",
		);

		await dialog.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog).toBeHidden();
		expect(saves).toEqual([]);

		await page.getByRole("button", { name: "Save settings" }).click();
		await dialog.getByRole("button", { name: "Save and stop single sign-on" }).click();

		await expect(page.getByText("Settings saved")).toBeVisible();
		expect(saves).toEqual([expect.objectContaining({ publicUrl: "http://games.example.com" })]);
	});
});
