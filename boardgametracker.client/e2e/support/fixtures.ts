import { type BrowserContext, test as base, expect, type Page } from "@playwright/test";

export const credentials = { username: "admin", password: "admin" };

export const test = base.extend<object, { signedInContext: BrowserContext }>({
	signedInContext: [
		async ({ browser }, use, workerInfo) => {
			const { baseURL, locale, timezoneId, viewport } = workerInfo.project.use;
			const context = await browser.newContext({ baseURL, locale, timezoneId, viewport });
			const page = await context.newPage();
			await page.goto("/login");
			await page.getByLabel("Username").fill(credentials.username);
			await page.getByLabel("Password").fill(credentials.password);
			await page.getByRole("button", { name: "Sign in" }).click();
			await page.waitForURL((url) => url.pathname !== "/login");
			await page.close();
			await use(context);
			await context.close();
		},
		{ scope: "worker" },
	],
	page: async ({ signedInContext }, use) => {
		const page = await signedInContext.newPage();
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
			if (response.status() >= 500) problems.push(`${response.status()} ${response.url()}`);
		});
		await use(page);
		await page.close();
		expect(problems).toEqual([]);
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
