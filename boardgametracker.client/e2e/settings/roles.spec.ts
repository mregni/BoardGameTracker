import type { Page } from "@playwright/test";
import { expect, readerCredentials, test, writerCredentials } from "../support/fixtures";
import { api, openContext, sidebarLink } from "../support/settings";

test.use({ allowedApiStatuses: [401, 404] });

const accounts = [
	{ role: "Reader", username: readerCredentials.username, fixture: "readerPage" },
	{ role: "User", username: writerCredentials.username, fixture: "writerPage" },
] as const;

const expectOnlyAccount = async (page: Page, username: string) => {
	const main = page.getByRole("main");
	const nav = main.getByRole("navigation");
	await expect(main.getByRole("heading", { name: "My account", level: 2 })).toBeVisible();
	await expect(main).toContainText(new RegExp(String.raw`Username\s*${username}`));
	await expect(nav.getByRole("button")).toHaveCount(1);
	await expect(nav.getByRole("button", { name: "Account Profile & password", exact: true })).toBeVisible();
	await expect(main.getByRole("heading", { name: "User management" })).toHaveCount(0);
	await expect(main.getByRole("button", { name: "Save settings" })).toHaveCount(0);
};

for (const { role, username, fixture } of accounts) {
	test.describe(role, () => {
		test("sees only the own account in the settings", async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/settings");

			await expectOnlyAccount(page, username);
			await expect(sidebarLink(page, "Settings")).toHaveAttribute("href", "/settings");
		});

		test("cannot open an admin section by its address", async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			for (const tab of ["general", "integrations", "shelf-of-shame", "game-nights", "advanced"]) {
				await page.goto(`/settings?tab=${tab}`);
				await expectOnlyAccount(page, username);
				await expect(page.getByRole("heading", { name: "Danger zone" })).toHaveCount(0);
			}
		});

		test("does not get stuck on the single sign-on section", async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			await page.goto("/settings?tab=sso");

			test.fail(true, "BUG: ?tab=sso keeps a non-admin on an endless spinner instead of opening the account section");
			await expectOnlyAccount(page, username);
		});

		test("is refused by the admin endpoints", async ({ readerPage, writerPage }) => {
			const page = fixture === "readerPage" ? readerPage : writerPage;
			const client = await api(page);
			const settings = await (await client.get("/api/settings")).json();

			const responses = [
				await client.put("/api/settings", { ...settings, currency: "X" }),
				await client.get("/api/settings/integrations/status"),
				await client.post("/api/settings/integrations/bgg/test"),
				await client.get("/api/admin/users"),
				await client.get("/api/admin/oidc-providers"),
				await client.get("/api/admin/oidc-providers/setup"),
				await client.post("/api/auth/register", {
					username: "intruder",
					email: "intruder@example.test",
					password: "intruder-password",
					role: "Admin",
				}),
				await client.post("/api/maintenance/reset"),
				await client.post("/api/maintenance/factory-reset"),
				await client.post("/api/update/check"),
			];

			expect(responses.map((response) => response.status())).toEqual(responses.map(() => 403));
			expect((await (await client.get("/api/settings")).json()).currency).toBe(settings.currency);
		});
	});
}

test("sends a visitor who is not signed in to the login page", async ({ browser, baseURL }) => {
	const context = await openContext(browser, baseURL);
	const page = await context.newPage();

	await page.goto("/settings");

	await expect(page).toHaveURL(/\/login/);
	await expect(page.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
	await context.close();
});
