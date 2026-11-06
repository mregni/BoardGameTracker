import { expect, test } from "../support/session";

const protectedPages = [
	"/",
	"/games",
	"/games/add",
	"/games/new",
	"/games/bgg",
	"/games/pick",
	"/games/import/start",
	"/games/1",
	"/games/1/sessions",
	"/games/1/update",
	"/players",
	"/players/new",
	"/players/1",
	"/players/1/sessions",
	"/sessions/new",
	"/sessions/new/1",
	"/sessions/update/1",
	"/game-nights",
	"/game-nights/1",
	"/loans",
	"/locations",
	"/locations/1",
	"/compare",
	"/leaderboard",
	"/shames",
	"/chat",
	"/settings",
	"/settings?tab=account",
	"/games?view=table",
];

const gatedByRole = [
	"/games/add",
	"/games/new",
	"/games/bgg",
	"/games/import/start",
	"/games/1/update",
	"/players/new",
	"/sessions/new",
	"/sessions/new/1",
	"/sessions/update/1",
	"/chat",
];

for (const path of protectedPages) {
	test(`sends a visitor from ${path} to the login page`, async ({ guestPage: page }) => {
		await page.goto(path);

		await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
		await expect(page).toHaveURL(/\/login\?/);
		await expect(page.getByRole("complementary")).toHaveCount(0);
		if (!gatedByRole.includes(path)) {
			expect(new URL(page.url()).searchParams.get("redirect")).toBe(path);
		}
	});
}

test("sends a visitor from an unknown page to the login page", async ({ guestPage: page }) => {
	await page.goto("/does-not-exist");

	await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
	expect(new URL(page.url()).searchParams.get("redirect")).toBe("/does-not-exist");
});

for (const [path, button] of [
	["/login", "Sign in"],
	["/forgot-password", "Back to sign in"],
	["/reset-password", "Back to sign in"],
]) {
	test(`leaves a visitor on ${path}`, async ({ guestPage: page }) => {
		await page.goto(path);

		await expect(page.getByRole("button", { name: button })).toBeVisible();
		await expect(page).toHaveURL(path);
	});
}
