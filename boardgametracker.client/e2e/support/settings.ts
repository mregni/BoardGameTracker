import { type Browser, expect, type Page } from "@playwright/test";
import type { Settings } from "../../src/models";
import { test } from "./fixtures";

export type SettingsTab =
	| "general"
	| "account"
	| "integrations"
	| "shelf-of-shame"
	| "game-nights"
	| "sso"
	| "advanced";

export const expectingFailures = test.extend({
	page: async ({ signedInContext }, use) => {
		const page = await signedInContext.newPage();
		await use(page);
		await page.close();
	},
});

export const openSection = async (page: Page, tab: SettingsTab) => {
	await page.goto(`/settings?tab=${tab}`);
	await expect(page.getByRole("heading", { name: "Settings", level: 1 })).toBeVisible();
};

export const api = async (page: Page) => {
	if (page.url() === "about:blank") await openSection(page, "general");
	const { accessToken } = await page.evaluate(() =>
		fetch("/api/auth/refresh", { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" } }).then((response) =>
			response.json(),
		),
	);
	const headers = { Authorization: `Bearer ${accessToken}` };
	return {
		get: (path: string) => page.request.get(path, { headers }),
		post: (path: string, data?: unknown) => page.request.post(path, { headers, data }),
		put: (path: string, data?: unknown) => page.request.put(path, { headers, data }),
		delete: (path: string) => page.request.delete(path, { headers }),
	};
};

export const readSettings = async (page: Page): Promise<Settings> =>
	(await (await api(page)).get("/api/settings")).json();

export const writeSettings = async (page: Page, settings: Settings) => {
	const response = await (await api(page)).put("/api/settings", settings);
	expect(response.ok(), await response.text()).toBe(true);
};

export const mockSettings = (page: Page, patch: Partial<Settings>) =>
	page.route("**/api/settings", async (route) => {
		if (route.request().method() !== "GET") {
			await route.fallback();
			return;
		}
		const response = await route.fetch();
		await route.fulfill({ response, json: { ...(await response.json()), ...patch } });
	});

export const saveSettings = async (page: Page) => {
	await page.getByRole("button", { name: "Save settings" }).click();
	await expect(page.getByText("Settings saved")).toBeVisible();
};

export const sidebarLink = (page: Page, name: string | RegExp) =>
	page.getByRole("complementary").getByRole("link", { name });

export const openContext = (browser: Browser, baseURL: string | undefined) =>
	browser.newContext({ baseURL, locale: "en-US", timezoneId: "Europe/Brussels" });

export const submitLogin = async (page: Page, username: string, password: string) => {
	await page.goto("/login");
	await page.getByLabel("Username").fill(username);
	await page.getByLabel("Password").fill(password);
	await page.getByRole("button", { name: "Sign in" }).click();
};

export const signInAs = async (browser: Browser, baseURL: string | undefined, username: string, password: string) => {
	const context = await openContext(browser, baseURL);
	const page = await context.newPage();
	await submitLogin(page, username, password);
	await page.waitForURL((url) => url.pathname !== "/login");
	return page;
};

export interface ManagedUser {
	id: string;
	username: string;
	playerId: number | null;
}

export const findUser = async (page: Page, username: string): Promise<ManagedUser | undefined> => {
	const users: ManagedUser[] = await (await (await api(page)).get("/api/admin/users")).json();
	return users.find((user) => user.username === username);
};

export const createUser = async (page: Page, username: string, password: string, role = "User") => {
	const response = await (await api(page)).post("/api/auth/register", {
		username,
		email: `${username}@example.test`,
		password,
		role,
	});
	expect(response.ok(), await response.text()).toBe(true);
};

export const removeUser = async (page: Page, username: string) => {
	const user = await findUser(page, username);
	if (user) await (await api(page)).delete(`/api/admin/users/${user.id}`);
};
