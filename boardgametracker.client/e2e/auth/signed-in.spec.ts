import { credentials } from "../support/fixtures";
import { loans } from "../support/seedData";
import { expect, signIn, test } from "../support/session";

const [, overdueLoan] = loans;

test.use({ allowedApiStatuses: [401, 404] });

test("sends a signed in user away from the login page", async ({ page }) => {
	await page.goto("/login");

	await expect(page).toHaveURL("/");
	await expect(page.getByRole("region", { name: "Top players" })).toBeVisible();
});

test("follows the redirect of the login page when already signed in", async ({ page }) => {
	await page.goto("/login?redirect=/loans");

	await expect(page).toHaveURL("/loans");
});

for (const target of ["https://evil.example/", "//evil.example/", "/\\evil.example/", "javascript:alert(1)"]) {
	test(`ignores a redirect to ${target}`, async ({ page }) => {
		await page.goto(`/login?redirect=${encodeURIComponent(target)}`);

		await expect(page).toHaveURL("/");
	});
}

test("keeps the session after a reload", async ({ guestPage: page }) => {
	await page.goto("/games");
	await signIn(page, credentials);
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();

	await page.reload();

	await expect(page).toHaveURL("/games");
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
});

test("refreshes an expired access token and repeats the request", async ({ guestPage: page }) => {
	await page.goto("/");
	await signIn(page, credentials);
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
	let expired = true;
	await page.route("**/api/loans", async (route) => {
		if (expired) {
			expired = false;
			await route.fulfill({ status: 401, json: {} });
		} else {
			await route.fallback();
		}
	});
	const refreshed = page.waitForResponse("**/api/auth/refresh");

	await page
		.getByRole("complementary")
		.getByRole("link", { name: /^Loans/ })
		.click();

	expect((await refreshed).status()).toBe(200);
	await expect(page.getByRole("main").getByRole("heading", { name: overdueLoan.player, level: 2 })).toBeVisible();
});

test("goes to the login page when the session cannot be refreshed", async ({ guestPage: page }) => {
	await page.goto("/");
	await signIn(page, credentials);
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
	await page.route("**/api/loans", (route) => route.fulfill({ status: 401, json: {} }));
	await page.route("**/api/auth/refresh", (route) => route.fulfill({ status: 401, json: {} }));

	await page
		.getByRole("complementary")
		.getByRole("link", { name: /^Loans/ })
		.click();

	await expect(page).toHaveURL(/\/login\?/);
	expect(new URL(page.url()).searchParams.get("redirect")).toBe("/loans");
});

test("shows who is signed in", async ({ readerPage: page }) => {
	await page.goto("/");

	await expect(page.getByRole("complementary").getByText("reader", { exact: true })).toBeVisible();
});
