import type { BrowserContext, Page } from "@playwright/test";
import { test as base, expect, paceLogin, useWatchedPage } from "./fixtures";

export const test = base.extend<{ guestPage: Page }>({
	guestPage: async ({ browser, allowedApiStatuses }, use, testInfo) => {
		const { baseURL, locale, timezoneId, viewport } = testInfo.project.use;
		const context = await browser.newContext({ baseURL, locale, timezoneId, viewport });
		await useWatchedPage(context, allowedApiStatuses, use);
		await context.close();
	},
});

export { expect, paceLogin };

export const spaced = (spacingMs: number) => {
	let nextTurn = 0;
	return async () => {
		const now = Date.now();
		const wait = Math.max(0, nextTurn - now);
		nextTurn = Math.max(nextTurn, now) + spacingMs;
		await new Promise((resolve) => setTimeout(resolve, wait));
	};
};

export const SESSION_HEADERS = { "X-Requested-With": "XMLHttpRequest" };

export const apiFor = async (context: BrowserContext) => {
	const refreshed = await context.request.post("/api/auth/refresh", { headers: SESSION_HEADERS });
	expect(refreshed.ok()).toBe(true);
	const { accessToken } = await refreshed.json();
	const headers = { Authorization: `Bearer ${accessToken}` };
	return {
		headers,
		call: (method: string, path: string, data?: unknown) => context.request.fetch(path, { method, data, headers }),
	};
};

export type Api = Awaited<ReturnType<typeof apiFor>>;

export const signIn = async (page: Page, login: { username: string; password: string }) => {
	await page.getByLabel("Username").fill(login.username);
	await page.getByLabel("Password").fill(login.password);
	await paceLogin();
	await page.getByRole("button", { name: "Sign in" }).click();
};

export const changeSettings = async (api: Api, changes: Record<string, unknown>) => {
	const settings = await (await api.call("GET", "/api/settings")).json();
	const response = await api.call("PUT", "/api/settings", { ...settings, ...changes });
	expect(response.ok(), await response.text()).toBe(true);
};
