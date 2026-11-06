import { readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import { type Browser, type BrowserContext, test as base, expect, type Page, type TestInfo } from "@playwright/test";

type TestInfoUse = TestInfo["project"]["use"];

export const credentials = { username: "admin", password: "admin" };
export const readerCredentials = { username: "reader", password: "reader-password", role: "Reader" };
export const writerCredentials = { username: "writer", password: "writer-password", role: "User" };

const REFRESH_REUSE_MS = 5 * 60_000;
const LOGIN_SPACING_MS = 8_000;
const loginTurnFile = path.join(tmpdir(), `bgt-e2e-login-turn-${process.env.E2E_CLIENT_PORT ?? 5543}`);

export const paceLogin = async () => {
	const lastTurn = await readFile(loginTurnFile, "utf8").then(Number, () => 0);
	const now = Date.now();
	const turn = Math.max(now, lastTurn + LOGIN_SPACING_MS);
	await writeFile(loginTurnFile, String(turn));
	await new Promise((resolve) => setTimeout(resolve, turn - now));
};

const shareRefreshes = async (context: BrowserContext) => {
	let shared: { json: unknown; expires: number } | null = null;
	await context.route("**/api/auth/refresh", async (route) => {
		if (shared && Date.now() < shared.expires) {
			await route.fulfill({ json: shared.json });
			return;
		}
		const response = await route.fetch();
		shared = response.ok() ? { json: await response.json(), expires: Date.now() + REFRESH_REUSE_MS } : null;
		await route.fulfill({ response });
	});
};

const signIn = async (browser: Browser, use: TestInfoUse, login: { username: string; password: string }) => {
	const { baseURL, locale, timezoneId, viewport } = use;
	const context = await browser.newContext({ baseURL, locale, timezoneId, viewport });
	const page = await context.newPage();
	await page.goto("/login");
	await page.getByLabel("Username").fill(login.username);
	await page.getByLabel("Password").fill(login.password);
	await paceLogin();
	await page.getByRole("button", { name: "Sign in" }).click();
	await page.waitForURL((url) => url.pathname !== "/login");
	await page.close();
	await shareRefreshes(context);
	return context;
};

export const useWatchedPage = async (
	context: BrowserContext,
	allowedApiStatuses: number[],
	use: (page: Page) => Promise<void>,
	allowedImageStatuses: number[] = [],
) => {
	const page = await context.newPage();
	const problems: string[] = [];
	page.on("pageerror", (error) => {
		if (!error.message.startsWith("Transition was ")) problems.push(error.message);
	});
	page.on("console", (message) => {
		if (message.type() === "error" && !message.text().startsWith("Failed to load resource")) {
			problems.push(message.text());
		}
	});
	page.on("response", (response) => {
		const status = response.status();
		const { pathname } = new URL(response.url());
		const isBrokenImage = status >= 400 && pathname.startsWith("/images/") && !allowedImageStatuses.includes(status);
		const isFailure = status >= (pathname.startsWith("/api/") ? 400 : 500) && !allowedApiStatuses.includes(status);
		if (isBrokenImage || isFailure) {
			problems.push(`${status} ${response.url()}`);
		}
	});
	await use(page);
	await page.close();
	expect(problems).toEqual([]);
};

export const test = base.extend<
	{ allowedApiStatuses: number[]; readerPage: Page; writerPage: Page },
	{ signedInContext: BrowserContext; readerContext: BrowserContext; writerContext: BrowserContext }
>({
	allowedApiStatuses: [[401], { option: true }],
	signedInContext: [
		async ({ browser }, use, workerInfo) => {
			const context = await signIn(browser, workerInfo.project.use, credentials);
			await use(context);
			await context.close();
		},
		{ scope: "worker" },
	],
	readerContext: [
		async ({ browser }, use, workerInfo) => {
			const context = await signIn(browser, workerInfo.project.use, readerCredentials);
			await use(context);
			await context.close();
		},
		{ scope: "worker" },
	],
	writerContext: [
		async ({ browser }, use, workerInfo) => {
			const context = await signIn(browser, workerInfo.project.use, writerCredentials);
			await use(context);
			await context.close();
		},
		{ scope: "worker" },
	],
	readerPage: async ({ readerContext, allowedApiStatuses }, use) => {
		await useWatchedPage(readerContext, allowedApiStatuses, use);
	},
	writerPage: async ({ writerContext, allowedApiStatuses }, use) => {
		await useWatchedPage(writerContext, allowedApiStatuses, use);
	},
	page: async ({ signedInContext, allowedApiStatuses }, use) => {
		await useWatchedPage(signedInContext, allowedApiStatuses, use);
	},
});

export const expectVisibleImagesToLoad = async (page: Page) => {
	const images = page.getByRole("main").locator("img:visible");
	await expect(images.first()).toBeVisible();
	await expect
		.poll(() =>
			images.evaluateAll((elements) =>
				(elements as HTMLImageElement[])
					.filter((image) => image.getBoundingClientRect().top < window.innerHeight)
					.filter((image) => !image.complete || image.naturalWidth === 0)
					.map((image) => image.src),
			),
		)
		.toEqual([]);
};

export { expect };
