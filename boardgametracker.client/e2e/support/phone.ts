import type { APIRequestContext, Browser, Locator, Page, TestInfo } from "@playwright/test";
import { expect } from "./fixtures";
import { games, locations, players } from "./seedData";

export const phone = { width: 375, height: 812 };
export const smallPhone = { width: 320, height: 568 };
export const tablet = { width: 768, height: 1024 };

export const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
export const playerId = (name: string) => players.indexOf(name) + 1;
export const locationId = (name: string) => locations.indexOf(name) + 1;

export const settled = async (page: Page) => {
	await expect(page.getByRole("main")).toBeVisible();
	await page.waitForLoadState("networkidle");
};

export const expectNoSidewaysScroll = async (page: Page) => {
	const widths = await page.evaluate(() => ({
		content: document.documentElement.scrollWidth,
		screen: document.documentElement.clientWidth,
	}));
	expect(widths.content).toBeLessThanOrEqual(widths.screen);
};

export const expectInsideScreen = async (page: Page, locator: Locator) => {
	await locator.scrollIntoViewIfNeeded();
	const box = await locator.boundingBox();
	const screen = page.viewportSize();
	expect(box).not.toBeNull();
	expect(box?.x ?? -1).toBeGreaterThanOrEqual(0);
	expect((box?.x ?? 0) + (box?.width ?? 0)).toBeLessThanOrEqual(screen?.width ?? 0);
};

export const contentEndAboveBars = (page: Page) =>
	page.evaluate(() => {
		window.scrollTo(0, document.documentElement.scrollHeight);
		const height = window.innerHeight;
		const width = window.innerWidth;
		const isFixed = (element: Element) => getComputedStyle(element).position === "fixed";
		const inFixed = (element: Element) => {
			for (let node: Element | null = element; node; node = node.parentElement) {
				if (isFixed(node)) return true;
			}
			return false;
		};
		const barTop = Math.min(
			height,
			...[...document.body.querySelectorAll("*")]
				.filter((element) => isFixed(element))
				.map((element) => element.getBoundingClientRect())
				.filter((rect) => rect.width >= width * 0.9 && rect.height < 200 && rect.bottom >= height - 90)
				.map((rect) => rect.top),
		);
		const contentEnd = Math.max(
			...[...document.querySelectorAll("main *")]
				.filter((element) => element.childElementCount === 0 && !inFixed(element) && !element.closest("[aria-hidden]"))
				.map((element) => element.getBoundingClientRect())
				.filter((rect) => rect.height > 0 && rect.width > 0)
				.map((rect) => rect.bottom),
		);
		return barTop - contentEnd;
	});

export const expectContentClearsBars = async (page: Page) => {
	await expect.poll(() => contentEndAboveBars(page)).toBeGreaterThanOrEqual(0);
};

export const footerOf = (dialog: Locator) => dialog.locator('[data-slot="dialog-footer"]');

export const dialogAtRest = async (dialog: Locator) => {
	await expect(dialog).toBeVisible();
	await dialog.evaluate((element) => Promise.all(element.getAnimations().map((animation) => animation.finished)));
};

export const expectFooterInView = async (page: Page, dialog: Locator) => {
	await dialogAtRest(dialog);
	const box = await footerOf(dialog).boundingBox();
	expect(box).not.toBeNull();
	expect((box?.y ?? 0) + (box?.height ?? 0)).toBeLessThanOrEqual(page.viewportSize()?.height ?? 0);
};

export const expectDialogFits = async (dialog: Locator) => {
	await dialogAtRest(dialog);
	const sizes = await dialog.evaluate((element) => {
		const scroller = element.querySelector(":scope > div");
		const rect = element.getBoundingClientRect();
		return {
			left: rect.left,
			right: rect.right,
			top: rect.top,
			bottom: rect.bottom,
			screenWidth: window.innerWidth,
			screenHeight: window.innerHeight,
			contentWidth: scroller?.scrollWidth ?? 0,
			boxWidth: scroller?.clientWidth ?? 0,
		};
	});
	expect(sizes.left).toBeGreaterThanOrEqual(0);
	expect(sizes.right).toBeLessThanOrEqual(sizes.screenWidth);
	expect(sizes.top).toBeGreaterThanOrEqual(0);
	expect(sizes.bottom).toBeLessThanOrEqual(sizes.screenHeight);
	expect(sizes.contentWidth).toBeLessThanOrEqual(sizes.boxWidth);
};

export const signInOnTouchPhone = async (
	browser: Browser,
	info: TestInfo,
	login: { username: string; password: string },
) => {
	const { baseURL, locale, timezoneId } = info.project.use;
	const context = await browser.newContext({
		baseURL,
		locale,
		timezoneId,
		viewport: phone,
		hasTouch: true,
		isMobile: true,
	});
	const page = await context.newPage();
	await page.goto("/login");
	await page.getByLabel("Username").fill(login.username);
	await page.getByLabel("Password").fill(login.password);
	await page.getByRole("button", { name: "Sign in" }).tap();
	await page.waitForURL((url) => url.pathname !== "/login");
	await page.close();
	return context;
};

const failIfRefused = async (response: Awaited<ReturnType<APIRequestContext["get"]>>) => {
	expect(response.ok(), await response.text()).toBe(true);
	return response;
};

export const apiFor = async (page: Page) => {
	const seen = page.waitForRequest((request) => request.headers().authorization !== undefined);
	await page.goto("/locations");
	const Authorization = (await seen).headers().authorization;
	const headers = { Authorization };
	const { request } = page;
	return {
		get: async <T>(path: string) => (await failIfRefused(await request.get(path, { headers }))).json() as Promise<T>,
		post: async (path: string, data: unknown) =>
			(await failIfRefused(await request.post(path, { headers, data }))).json() as Promise<{ id: number }>,
		put: async (path: string, data: unknown) => failIfRefused(await request.put(path, { headers, data })),
		remove: async (path: string) => failIfRefused(await request.delete(path, { headers })),
		status: async (path: string) => (await request.get(path, { headers })).status(),
		tidy: async (path: string) => request.delete(path, { headers }),
	};
};
