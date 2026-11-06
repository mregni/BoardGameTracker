import type { Page } from "@playwright/test";
import { readerCredentials } from "../support/fixtures";
import { expect, paceLogin, test } from "../support/session";

const enableEmail = (page: Page) =>
	page.route("**/api/settings", async (route) => {
		const response = await route.fetch();
		await route.fulfill({ response, json: { ...(await response.json()), emailEnabled: true } });
	});

const resetUrl = "/reset-password?userId=9f1c2d3e-aaaa-bbbb-cccc-1234567890ab&token=not-a-real-token";

test("tells a visitor to ask an admin when email is not set up", async ({ guestPage: page }) => {
	await page.goto("/forgot-password");

	await expect(page.getByText("Reset password", { exact: true })).toBeVisible();
	await expect(page.getByText("Ask an admin to reset your password.")).toBeVisible();
	await expect(page.getByLabel("Username")).toHaveCount(0);

	await page.getByRole("button", { name: "Back to sign in" }).click();

	await expect(page).toHaveURL("/login");
});

test("asks for a username and confirms without revealing whether it exists", async ({ guestPage: page }) => {
	await enableEmail(page);
	await page.goto("/forgot-password");
	await expect(page.getByText("Enter your username and we'll email you a reset link")).toBeVisible();
	const request = page.waitForRequest("**/api/auth/forgot-password");

	await page.getByLabel("Username").fill(readerCredentials.username);
	await paceLogin();
	await page.getByRole("button", { name: "Send reset link" }).click();

	expect((await request).postDataJSON()).toEqual({ username: readerCredentials.username });
	await expect(
		page.getByText("If an account with that username exists and has an email, a reset link has been sent."),
	).toBeVisible();
	await expect(page.getByLabel("Username")).toHaveCount(0);

	await page.getByRole("button", { name: "Back to sign in" }).click();
	await expect(page).toHaveURL("/login");
});

test("does not ask for a reset without a username", async ({ guestPage: page }) => {
	test.fail(true, "BUG: forgot password submits an empty username and confirms a reset link was sent");
	await enableEmail(page);
	await page.goto("/forgot-password");
	await page.route("**/api/auth/forgot-password", (route) => route.fulfill({ status: 204 }));

	await page.getByRole("button", { name: "Send reset link" }).click();

	const confirmed = await page
		.getByText("a reset link has been sent")
		.waitFor({ timeout: 3_000 })
		.then(
			() => true,
			() => false,
		);
	expect(confirmed).toBe(false);
});

test("leaves the forgot password form", async ({ guestPage: page }) => {
	await enableEmail(page);
	await page.goto("/forgot-password");

	await page.getByRole("button", { name: "Back to sign in" }).click();

	await expect(page).toHaveURL("/login");
});

test("explains an incomplete reset link", async ({ guestPage: page }) => {
	await page.goto("/reset-password?token=only-a-token");

	const card = page.getByTestId("reset-link-incomplete");
	await expect(card).toContainText("This reset link is incomplete or expired");
	await expect(card).toContainText("Ask an admin to reset your password.");
	await expect(card.getByRole("button", { name: "Forgot password?" })).toHaveCount(0);

	await card.getByRole("button", { name: "Back to sign in" }).click();
	await expect(page).toHaveURL("/login");
});

test("offers a new link for an incomplete reset link when email is set up", async ({ guestPage: page }) => {
	await enableEmail(page);
	await page.goto("/reset-password");

	const card = page.getByTestId("reset-link-incomplete");
	await expect(card).toContainText("Ask for a new link to choose a new password.");

	await card.getByRole("button", { name: "Forgot password?" }).click();
	await expect(page).toHaveURL("/forgot-password");
});

test("checks the new password before sending it", async ({ guestPage: page }) => {
	await page.goto(resetUrl);
	await page.route("**/api/auth/reset-password", (route) => route.abort());
	const newPassword = page.getByLabel("New password", { exact: true });
	const confirm = page.getByLabel("Confirm new password");
	const submit = page.getByRole("button", { name: "Reset password" });

	await expect(page.getByText("Choose a new password")).toBeVisible();
	await expect(newPassword).toHaveAttribute("type", "password");

	await newPassword.fill("abc");
	await submit.click();
	await expect(page.getByText("Password must be at least 4 characters")).toBeVisible();

	await newPassword.fill("a-long-enough-password-1A");
	await confirm.fill("something-else-entirely-1A");
	await submit.click();
	await expect(page.getByText("Passwords do not match")).toBeVisible();
	await expect(page).toHaveURL(resetUrl);
});

test("refuses a reset link that is not valid", async ({ guestPage: page }) => {
	await page.goto(resetUrl);
	await page.getByLabel("New password", { exact: true }).fill("a-long-enough-password-1A");
	await page.getByLabel("Confirm new password").fill("a-long-enough-password-1A");
	await paceLogin();

	await page.getByRole("button", { name: "Reset password" }).click();

	await expect(page.getByText("This reset link is invalid or has expired.")).toBeVisible();
	await expect(page).toHaveURL(resetUrl);

	await page.getByRole("button", { name: "Back to sign in" }).click();
	await expect(page).toHaveURL("/login");
});
