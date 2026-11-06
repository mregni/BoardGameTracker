import type { BrowserContext, Page } from "@playwright/test";
import { expected, players } from "../support/seedData";
import { type Api, apiFor, expect, paceLogin, SESSION_HEADERS, signIn, test } from "../support/session";

test.describe.configure({ timeout: 60_000 });
test.use({ allowedApiStatuses: [400, 401, 404] });

const password = "first-login-password";

let api: Api;
const userIds: string[] = [];

test.beforeAll(async ({ signedInContext }) => {
	api = await apiFor(signedInContext);
});

test.afterAll(async () => {
	for (const id of userIds) await api.call("DELETE", `/api/admin/users/${id}`);
	const left: { id: number; name: string }[] = await (await api.call("GET", "/api/player")).json();
	for (const { id } of left.filter(({ name }) => !players.includes(name)))
		await api.call("DELETE", `/api/player/${id}`);
});

const createUser = async (username: string, role: string) => {
	const created = await api.call("POST", "/api/auth/register", {
		username,
		email: `${username}@example.test`,
		password,
		role,
	});
	expect(created.ok(), await created.text()).toBe(true);
	const { id } = await created.json();
	userIds.push(id);
	return { id: id as string, username, password };
};

const sessionResponses = /\/api\/auth\/(login|refresh)$/;

const promptForPlayerLink = (target: Page | BrowserContext) =>
	target.route(sessionResponses, async (route) => {
		const response = await route.fetch();
		const body = await response.json();
		await route.fulfill({ response, json: { ...body, user: { ...body.user, needsPlayerLink: true } } });
	});

test("a first login can skip, link or create a player", async ({ guestPage: page }) => {
	const user = await createUser("first-login-user", "User");
	await promptForPlayerLink(page);
	await page.goto("/");
	await signIn(page, user);
	const dialog = page.getByRole("dialog");
	const unlinked = players.slice(1);
	const userInfo = async () => (await api.call("GET", `/api/admin/users/${user.id}`)).json();

	await expect(dialog.getByRole("heading", { name: "Welcome! Are you one of these players?" })).toBeVisible();
	await expect(dialog).toContainText("You can link a player later under Settings → Account.");
	await expect(dialog.getByRole("radio", { name: /I'm new here/ })).not.toBeChecked();
	await expect(dialog.getByRole("button", { name: "This is me" })).toBeDisabled();

	const skipped = page.waitForResponse("**/api/auth/player-link/skip");
	await dialog.getByRole("button", { name: "Not now" }).click();
	expect((await skipped).status()).toBe(200);
	await expect(dialog).toHaveCount(0);
	expect((await userInfo()).playerId).toBeNull();

	await page.reload();
	await expect(dialog).toBeVisible();
	await dialog.getByRole("combobox").click();
	await expect(page.getByRole("option")).toHaveText([...unlinked].sort((a, b) => a.localeCompare(b)));
	await page.getByRole("option", { name: unlinked[0] }).click();
	await dialog.getByRole("button", { name: "This is me" }).click();

	await expect(page.getByText(`Linked to ${unlinked[0]}`)).toBeVisible();
	await expect(dialog).toHaveCount(0);
	const allPlayers = await (await api.call("GET", "/api/player")).json();
	const linked = await userInfo();
	expect(linked.playerId).toBe(allPlayers.find((player: { name: string }) => player.name === unlinked[0]).id);
	const unlink = await api.call("PUT", `/api/admin/users/${user.id}`, {
		username: user.username,
		email: linked.email,
		role: "User",
		playerId: null,
	});
	expect(unlink.ok(), await unlink.text()).toBe(true);

	await page.reload();
	await expect(dialog).toBeVisible();
	await dialog.getByText("I'm new here").click();
	await dialog.getByRole("button", { name: "Create my player" }).click();

	await expect(page.getByText("Player created and linked")).toBeVisible();
	const created = await userInfo();
	await page.unroute(sessionResponses);
	await page.goto("/players");
	await expect(page.getByRole("link", { name: new RegExp(user.username) })).toBeVisible();
	expect((await api.call("DELETE", `/api/player/${created.playerId}`)).ok()).toBe(true);
	await expect
		.poll(async () => ((await (await api.call("GET", "/api/player")).json()) as unknown[]).length)
		.toBe(expected.playerCount);
});

test("a reader cannot create a player on first login", async ({ readerContext }) => {
	const page = await readerContext.newPage();
	await promptForPlayerLink(page);
	await page.goto("/");

	const dialog = page.getByRole("dialog");
	await expect(dialog).toBeVisible();
	await expect(dialog.getByRole("radio", { name: /I'm new here/ })).toHaveCount(0);
	await expect(dialog.getByRole("button", { name: "This is me" })).toBeDisabled();
	await page.close();
});

test("skips the prompt when there is nothing to offer", async ({ readerContext }) => {
	const page = await readerContext.newPage();
	await promptForPlayerLink(page);
	await page.route("**/api/auth/player-link/suggestions", (route) =>
		route.fulfill({ json: { suggestions: [], players: [], canCreatePlayer: false } }),
	);
	const skipped = page.waitForResponse("**/api/auth/player-link/skip");
	await page.goto("/");

	expect((await skipped).status()).toBe(200);
	await expect(page.getByRole("dialog")).toHaveCount(0);
	await page.close();
});

test("refuses a disabled account", async ({ guestPage: page }) => {
	const user = await createUser("disabled-user", "User");
	expect((await api.call("POST", `/api/admin/users/${user.id}/disable`, {})).ok()).toBe(true);
	await paceLogin();

	expect((await page.request.post("/api/auth/login", { data: user })).status()).toBe(401);
});

test("tells a disabled account why it cannot sign in", async ({ guestPage: page }) => {
	const user = await createUser("disabled-message-user", "User");
	expect((await api.call("POST", `/api/admin/users/${user.id}/disable`, {})).ok()).toBe(true);
	await page.goto("/login");

	await signIn(page, user);

	await expect(page.getByText("This account is disabled. Ask an admin to enable it.")).toBeVisible();
});

test("stops accepting the access token after logging out", async ({ guestPage: page }) => {
	const user = await createUser("logout-token-user", "Reader");
	await paceLogin();
	const { accessToken } = await (await page.request.post("/api/auth/login", { data: user })).json();
	const headers = { Authorization: `Bearer ${accessToken}` };
	expect((await page.request.get("/api/auth/profile", { headers })).status()).toBe(200);

	await page.request.post("/api/auth/logout", { headers: { ...SESSION_HEADERS, ...headers } });

	test.fail(true, "BUG: the access token keeps working for up to 15 minutes after logout");
	expect((await page.request.get("/api/auth/profile", { headers })).status()).toBe(401);
});
