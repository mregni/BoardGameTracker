import { credentials } from "../support/fixtures";
import { expect, paceLogin, SESSION_HEADERS, signIn, test } from "../support/session";

const invalid = "Invalid username or password";

test.use({ allowedApiStatuses: [400, 401, 404] });

test("shows the sign in form", async ({ guestPage: page }) => {
	await page.goto("/login");
	await page.waitForLoadState("networkidle");

	await expect(page.getByText("Sign in to BoardGameTracker")).toBeVisible();
	await expect(page.getByLabel("Username")).toHaveAttribute("placeholder", "Enter your username");
	await expect(page.getByLabel("Password")).toHaveAttribute("type", "password");
	await expect(page.getByRole("button", { name: "Forgot password?" })).toHaveCount(0);
});

test("offers a password reset when email is set up", async ({ guestPage: page }) => {
	await page.route("**/api/settings", async (route) => {
		const response = await route.fetch();
		await route.fulfill({ response, json: { ...(await response.json()), emailEnabled: true } });
	});
	await page.goto("/login");

	await page.getByRole("button", { name: "Forgot password?" }).click();

	await expect(page).toHaveURL("/forgot-password");
});

test("refuses a wrong password, then signs in and returns to the requested page", async ({ guestPage: page }) => {
	await page.goto("/compare?left=2&right=3");
	await expect(page).toHaveURL(/\/login\?/);

	await signIn(page, { ...credentials, password: "not-the-password" });

	await expect(page.getByText(invalid)).toBeVisible();
	await expect(page).toHaveURL(/\/login\?/);

	await page.getByLabel("Password").fill(credentials.password);
	await paceLogin();
	await page.getByLabel("Password").press("Enter");

	await expect(page).toHaveURL("/compare?left=2&right=3");
	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
});

test("does not sign in with empty fields", async ({ guestPage: page }) => {
	await page.goto("/login");

	await paceLogin();
	await page.getByRole("button", { name: "Sign in" }).click();

	await expect(page.getByText(invalid)).toBeVisible();
	await expect(page).toHaveURL("/login");
});

for (const { name, status, title, message } of [
	{
		name: "too many attempts",
		status: 429,
		title: "",
		message: "Too many sign-in attempts. Wait a minute and try again.",
	},
	{
		name: "a locked account",
		status: 401,
		title: "error.auth.account-locked-out",
		message: "Too many failed attempts. Try again later.",
	},
	{ name: "a server error", status: 500, title: "", message: "A server error occurred. Please try again later." },
]) {
	test.describe(`with status ${status}`, () => {
		test.use({ allowedApiStatuses: [401, 404, status] });

		test(`explains ${name}`, async ({ guestPage: page }) => {
			await page.route("**/api/auth/login", (route) => route.fulfill({ status, json: { title } }));
			await page.goto("/login");

			await signIn(page, credentials);

			await expect(page.getByText(message)).toBeVisible();
			await expect(page).toHaveURL("/login");
		});
	});
}

test("logs out every tab and leaves nothing that still works", async ({ guestPage: page }) => {
	const context = page.context();
	await page.goto("/games");
	await signIn(page, credentials);
	await expect(page).toHaveURL("/games");
	const otherTab = await context.newPage();
	await otherTab.goto("/loans");
	await expect(otherTab.getByRole("button", { name: "Log out" })).toBeVisible();
	const refreshCookies = await context.cookies();
	const cookie = refreshCookies.map(({ name, value }) => `${name}=${value}`).join("; ");

	await page.getByRole("button", { name: "Log out" }).click();

	await expect(page).toHaveURL("/login");
	await expect(otherTab).toHaveURL(/\/login\?/);
	expect(new URL(otherTab.url()).searchParams.get("redirect")).toBe("/loans");
	await expect(page.getByLabel("Username")).toBeVisible();

	await page.goBack();
	await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
	await expect(page.getByRole("button", { name: "Log out" })).toHaveCount(0);

	await page.goto("/games");
	await expect(page).toHaveURL(/\/login\?/);

	const refresh = await page.request.post("/api/auth/refresh", { headers: { ...SESSION_HEADERS, Cookie: cookie } });
	expect(refresh.status()).toBe(401);
	expect((await context.cookies()).filter((candidate) => candidate.value.length > 0)).toEqual([]);
});
