import { expect, test } from "../support/session";

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

test("keeps the session after a reload", async ({ page }) => {
	await page.goto("/games");
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();

	await page.reload();

	await expect(page).toHaveURL("/games");
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
});

test("shows who is signed in", async ({ readerPage: page }) => {
	await page.goto("/");

	await expect(page.getByRole("complementary").getByText("reader", { exact: true })).toBeVisible();
});
