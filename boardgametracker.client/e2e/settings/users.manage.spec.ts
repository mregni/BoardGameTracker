import type { Page } from "@playwright/test";
import { credentials, expect, readerCredentials, test, writerCredentials } from "../support/fixtures";
import { pick } from "../support/gamesPage";
import { expected, linkedPlayer, players } from "../support/seedData";
import {
	api,
	createUser,
	findUser,
	openContext,
	openSection,
	removeUser,
	sidebarLink,
	signInAs,
	submitLogin,
} from "../support/settings";

test.use({ allowedApiStatuses: [400, 401, 404] });
test.describe.configure({ timeout: 60_000 });

const tempUser = "temp-user";
const tempPassword = "temp-password";

const row = (page: Page, username: string) =>
	page.getByRole("row").filter({ has: page.getByRole("cell", { name: username }) });
const cell = (page: Page, username: string, index: number) => row(page, username).getByRole("cell").nth(index);
const field = (page: Page, name: string) => page.getByRole("textbox", { name, exact: true });

const openAddUser = async (page: Page) => {
	await openSection(page, "account");
	await page.getByRole("button", { name: "Add user" }).click();
	return page.getByRole("dialog");
};

const fillNewUser = async (page: Page, username = tempUser, password = tempPassword) => {
	await field(page, "Username").fill(username);
	await field(page, "Email").fill(`${username}@example.test`);
	await field(page, "Password").fill(password);
};

const removeCreatedPlayers = async (page: Page) => {
	const client = await api(page);
	const all: { id: number; name: string }[] = await (await client.get("/api/player")).json();
	for (const player of all.filter((x) => x.name.startsWith("temp-"))) {
		await client.delete(`/api/player/${player.id}`);
	}
};

test.afterEach(async ({ page }) => {
	await removeUser(page, tempUser);
	await removeCreatedPlayers(page);
});

test("lists every user with role, player and last login", async ({ page }) => {
	await openSection(page, "account");

	await expect(page.getByRole("row")).toHaveCount(4);
	await expect(cell(page, credentials.username, 1)).toHaveText("Admin");
	await expect(cell(page, credentials.username, 2)).toHaveText(linkedPlayer);
	await expect(cell(page, credentials.username, 3)).toHaveText(/^\d{2}-\d{2}-\d{4} \d{2}:\d{2}$/);
	await expect(cell(page, readerCredentials.username, 1)).toHaveText("Reader");
	await expect(cell(page, readerCredentials.username, 2)).toHaveText("—");
	await expect(cell(page, writerCredentials.username, 1)).toHaveText("User");
	await expect(row(page, credentials.username).getByRole("button")).toHaveText(["Edit", "Access", "Reset password"]);
	await expect(row(page, writerCredentials.username).getByRole("button")).toHaveText([
		"Edit",
		"Access",
		"Reset password",
		"Delete",
	]);
});

test("sorts the users by name and role", async ({ page }) => {
	await openSection(page, "account");
	const names = page.getByRole("rowgroup").nth(1).getByRole("row").locator("td:first-child");
	const usernames = [credentials.username, readerCredentials.username, writerCredentials.username].sort();

	await page.getByRole("button", { name: "Username" }).click();
	await expect(names).toHaveText(usernames.map((name) => new RegExp(`${name}$`)));

	await page.getByRole("button", { name: "Username" }).click();
	await expect(names).toHaveText([...usernames].reverse().map((name) => new RegExp(`${name}$`)));
});

test("refuses a new user without the required fields", async ({ page }) => {
	const dialog = await openAddUser(page);

	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(dialog.getByRole("heading", { name: "Add user" })).toBeVisible();
	await expect(dialog.getByText("Required", { exact: false })).toHaveCount(3);
	await expect(dialog.getByRole("textbox").first()).toBeFocused();

	await field(page, "Password").fill("abc");
	await expect(dialog.getByText(/^Password must be at least/)).toBeVisible();

	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
	await expect(page.getByRole("row")).toHaveCount(4);
});

test("refuses a username that already exists", async ({ page }) => {
	const dialog = await openAddUser(page);
	await fillNewUser(page, writerCredentials.username);

	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(dialog.getByText("Username already exists")).toBeVisible();
	await expect(page.getByRole("row", { includeHidden: true })).toHaveCount(4);
});

test("adds a user without a player and removes it again", async ({ page }) => {
	const dialog = await openAddUser(page);
	await fillNewUser(page);
	await pick(page, "Role", "Reader");
	await pick(page, "Player", "No player");
	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(page.getByText("User created")).toBeVisible();
	await expect(dialog).toBeHidden();
	await expect(cell(page, tempUser, 1)).toHaveText("Reader");
	await expect(cell(page, tempUser, 2)).toHaveText("—");
	await expect(cell(page, tempUser, 3)).toHaveText("Never");

	await row(page, tempUser).getByRole("button", { name: "Delete" }).click();
	const confirm = page.getByRole("dialog");
	await expect(confirm).toContainText(`Are you sure you want to delete "${tempUser}"? This cannot be undone.`);
	await confirm.getByRole("button", { name: "Cancel" }).click();
	await expect(row(page, tempUser)).toHaveCount(1);

	await row(page, tempUser).getByRole("button", { name: "Delete" }).click();
	await confirm.getByRole("button", { name: "Delete" }).click();

	await expect(page.getByText("User deleted")).toBeVisible();
	await expect(row(page, tempUser)).toHaveCount(0);
	await expect(page.getByRole("row")).toHaveCount(4);
});

test("adds a user with a new player", async ({ page }) => {
	const dialog = await openAddUser(page);
	await fillNewUser(page);
	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(page.getByText("User created")).toBeVisible();
	await expect(cell(page, tempUser, 1)).toHaveText("User");
	await expect(cell(page, tempUser, 2)).toContainText(tempUser);

	await page.goto("/players");
	await expect(page.getByRole("link", { name: new RegExp(tempUser) })).toBeVisible();
	await expect(sidebarLink(page, `Players ${expected.playerCount + 1}`)).toBeVisible();
});

test("adds a user linked to an existing player", async ({ page }) => {
	const player = players[1];
	const dialog = await openAddUser(page);
	await fillNewUser(page);
	await pick(page, "Player", "Link an existing player");
	await page.getByRole("combobox", { name: "Linked player" }).click();

	await expect(page.getByRole("option", { name: linkedPlayer, exact: true })).toHaveCount(0);

	await page.getByRole("option", { name: player, exact: true }).click();
	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	await expect(page.getByText("User created")).toBeVisible();
	await expect(cell(page, tempUser, 2)).toContainText(player);
	await page.goto("/players");
	await expect(sidebarLink(page, `Players ${expected.playerCount}`)).toBeVisible();
});

test("edits name, email, role and player of a user", async ({ page }) => {
	const renamed = "temp-renamed";
	await createUser(page, tempUser, tempPassword);
	try {
		await openSection(page, "account");

		await row(page, tempUser).getByRole("button", { name: "Edit" }).click();
		const dialog = page.getByRole("dialog");
		await expect(dialog.getByRole("heading", { name: "Edit user" })).toBeVisible();
		await expect(field(page, "Username")).toHaveValue(tempUser);
		await expect(field(page, "Email")).toHaveValue(`${tempUser}@example.test`);
		await expect(page.getByRole("combobox", { name: "Role" })).toHaveText("User");
		await expect(page.getByRole("combobox", { name: "Linked player" })).toHaveText("Not linked");

		await field(page, "Username").fill(renamed);
		await field(page, "Email").fill("renamed@example.test");
		await pick(page, "Role", "Reader");
		await page.getByRole("combobox", { name: "Linked player" }).click();
		await expect(page.getByRole("option", { name: `${linkedPlayer} (Linked to ${credentials.username})` })).toHaveCount(
			0,
		);
		await page.getByRole("option", { name: players[2], exact: true }).click();
		await dialog.getByRole("button", { name: "Save" }).click();

		await expect(page.getByText("User saved")).toBeVisible();
		await expect(dialog).toBeHidden();
		await expect(cell(page, renamed, 1)).toHaveText("Reader");
		await expect(cell(page, renamed, 2)).toContainText(players[2]);
		await expect(row(page, tempUser)).toHaveCount(0);

		await row(page, renamed).getByRole("button", { name: "Edit" }).click();
		await expect(field(page, "Email")).toHaveValue("renamed@example.test");
		await dialog.getByRole("button", { name: "Cancel" }).click();
	} finally {
		await removeUser(page, renamed);
	}
});

test("refuses an edit to an existing or empty username", async ({ page }) => {
	await createUser(page, tempUser, tempPassword);
	await openSection(page, "account");
	await row(page, tempUser).getByRole("button", { name: "Edit" }).click();
	const dialog = page.getByRole("dialog");

	await field(page, "Username").fill(readerCredentials.username);
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog.getByText("Username already exists")).toBeVisible();

	await field(page, "Username").fill("");
	await dialog.getByRole("button", { name: "Save" }).click();
	await expect(dialog.getByText("Username is required")).toBeVisible();
});

test("disables a user, ends the session and enables the user again", async ({ page, browser, baseURL }) => {
	await createUser(page, tempUser, tempPassword);
	const own = await signInAs(browser, baseURL, tempUser, tempPassword);
	const ownClient = await api(own);
	try {
		await openSection(page, "account");
		await row(page, tempUser).getByRole("button", { name: "Access" }).click();
		const dialog = page.getByRole("dialog", { name: "Sign-in access" });

		await expect(dialog.getByText("Can sign in")).toBeVisible();
		await expect(dialog.getByText("Disabling ends every session and blocks sign-in.")).toBeVisible();

		await dialog.getByRole("button", { name: "Disable" }).click();
		const confirm = page.getByRole("dialog", { name: `Disable ${tempUser}?` });
		await expect(confirm).toContainText("They are signed out on every device and can't sign in until you enable them.");
		await confirm.getByRole("button", { name: "Cancel" }).click();
		await expect(dialog.getByText("Can sign in")).toBeVisible();

		await dialog.getByRole("button", { name: "Disable" }).click();
		await confirm.getByRole("button", { name: "Disable" }).click();

		await expect(dialog.getByText("Disabled", { exact: true })).toBeVisible();
		await expect(dialog.getByRole("button", { name: "Disable" })).toHaveCount(0);
		await dialog.getByText("Close", { exact: true }).click();
		await expect(row(page, tempUser).getByText("Disabled")).toBeVisible();

		expect((await ownClient.get("/api/auth/profile")).status()).toBe(401);
		await own.goto("/settings");
		await expect(own).toHaveURL(/\/login/);
		const refused = own.waitForResponse("**/api/auth/login");
		await submitLogin(own, tempUser, tempPassword);
		expect((await refused).status()).toBe(401);
		await expect(own).toHaveURL(/\/login/);

		await row(page, tempUser).getByRole("button", { name: "Access" }).click();
		await dialog.getByRole("button", { name: "Enable" }).click();
		await expect(dialog.getByText("Can sign in")).toBeVisible();
		await dialog.getByText("Close", { exact: true }).click();
		await expect(row(page, tempUser).getByText("Disabled")).toHaveCount(0);

		await submitLogin(own, tempUser, tempPassword);
		await expect(sidebarLink(own, "Settings")).toBeVisible();
	} finally {
		await own.context().close();
	}
});

test("tells a disabled user why the sign-in is refused", async ({ page, browser, baseURL }) => {
	await createUser(page, tempUser, tempPassword);
	const user = await findUser(page, tempUser);
	await (await api(page)).post(`/api/admin/users/${user?.id}/disable`);
	const context = await openContext(browser, baseURL);
	const visitor = await context.newPage();

	try {
		await submitLogin(visitor, tempUser, tempPassword);

		await expect(visitor.getByText("This account is disabled. Ask an admin to enable it.")).toBeVisible();
	} finally {
		await context.close();
	}
});

test("resets a password to a temporary one that works", async ({ page, browser, baseURL }) => {
	await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
	await createUser(page, tempUser, tempPassword);
	await openSection(page, "account");
	await row(page, tempUser).getByRole("button", { name: "Reset password" }).click();
	const dialog = page.getByRole("dialog");

	await expect(dialog.getByRole("heading", { name: `Reset ${tempUser}'s password?` })).toBeVisible();
	await expect(dialog).toContainText("Their current password stops working.");
	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();

	await row(page, tempUser).getByRole("button", { name: "Reset password" }).click();
	await dialog.getByRole("button", { name: "Reset password" }).click();

	await expect(dialog.getByRole("heading", { name: "Temporary password" })).toBeVisible();
	await expect(dialog).toContainText(`Password reset for "${tempUser}". Save this password — it won't be shown again.`);
	const password = (await dialog.locator("code").textContent()) ?? "";
	expect(password.length).toBeGreaterThanOrEqual(8);

	await dialog.getByRole("button", { name: "Copy to clipboard" }).click();
	await expect(dialog.getByRole("button", { name: "Copied" })).toBeVisible();
	expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(password);

	await dialog.getByText("Close", { exact: true }).click();
	await expect(dialog).toBeHidden();

	const visitorContext = await openContext(browser, baseURL);
	const visitor = await visitorContext.newPage();
	await submitLogin(visitor, tempUser, tempPassword);
	await expect(visitor.getByText("Invalid username or password")).toBeVisible();
	await submitLogin(visitor, tempUser, password);
	await expect(sidebarLink(visitor, "Settings")).toBeVisible();
	await visitorContext.close();
});

test("ends the sessions of a user whose password is reset", async ({ page, browser, baseURL }) => {
	await createUser(page, tempUser, tempPassword);
	const user = await findUser(page, tempUser);
	const own = await signInAs(browser, baseURL, tempUser, tempPassword);
	try {
		expect((await (await api(page)).post(`/api/auth/reset-password/${user?.id}`)).ok()).toBe(true);

		await own.goto("/settings");

		await expect(own).toHaveURL(/\/login/);
	} finally {
		await own.context().close();
	}
});

test("protects the own account", async ({ page }) => {
	const self = await findUser(page, credentials.username);
	await openSection(page, "account");

	await expect(row(page, credentials.username).getByRole("button", { name: "Delete" })).toHaveCount(0);

	await row(page, credentials.username).getByRole("button", { name: "Access" }).click();
	const access = page.getByRole("dialog", { name: "Sign-in access" });
	await expect(access.getByText("Can sign in")).toBeVisible();
	await expect(access.getByRole("button", { name: "Disable" })).toHaveCount(0);
	await access.getByText("Close", { exact: true }).click();

	await row(page, credentials.username).getByRole("button", { name: "Edit" }).click();
	const edit = page.getByRole("dialog", { name: "Edit user" });
	await pick(page, "Role", "Reader");
	await edit.getByRole("button", { name: "Save" }).click();

	await expect(edit.getByText("Cannot remove Admin role from the last admin account")).toBeVisible();
	await edit.getByRole("button", { name: "Cancel" }).click();
	await expect(cell(page, credentials.username, 1)).toHaveText("Admin");

	const client = await api(page);
	expect((await client.delete(`/api/admin/users/${self?.id}`)).status()).toBe(400);
	expect((await client.post(`/api/admin/users/${self?.id}/disable`)).status()).toBe(400);
});

test("refuses an email address that is not one", async ({ page }) => {
	const dialog = await openAddUser(page);
	await field(page, "Username").fill(tempUser);
	await field(page, "Email").fill("not an email");
	await field(page, "Password").fill(tempPassword);

	const sent = page
		.waitForRequest((request) => request.url().endsWith("/api/auth/register"), { timeout: 1500 })
		.catch(() => null);
	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	test.fail(true, "BUG: the add user dialog accepts any text as email address");
	expect(await sent).toBeNull();
});

test("tells the password rule of the server when adding a user", async ({ page }) => {
	await openAddUser(page);

	await field(page, "Password").fill("abc");

	await expect(page.getByRole("dialog").getByText("Password must be at least 8 characters")).toBeVisible();
});

test("stops a password of 7 characters, which the server refuses too", async ({ page }) => {
	const password = "seven-7";
	const dialog = await openAddUser(page);
	await fillNewUser(page, tempUser, password);
	const sent = page
		.waitForRequest((request) => request.url().endsWith("/api/auth/register"), { timeout: 1500 })
		.catch(() => null);

	await dialog.getByRole("button", { name: "Add", exact: true }).click();

	expect(await sent).toBeNull();
	await expect(dialog).toBeVisible();
	const refused = await (await api(page)).post("/api/auth/register", {
		username: tempUser,
		email: `${tempUser}@example.test`,
		password,
		role: "User",
	});
	expect(refused.status()).toBe(400);
});

test("says so when resetting a password or disabling a user fails", async ({ page }) => {
	await page.route("**/api/auth/reset-password/*", (route) =>
		route.fulfill({ status: 400, json: { title: "error.test.unknown" } }),
	);
	await page.route("**/api/admin/users/*/disable", (route) =>
		route.fulfill({ status: 400, json: { title: "error.test.unknown" } }),
	);
	await openSection(page, "account");
	const username = writerCredentials.username;

	await row(page, username).getByRole("button", { name: "Reset password" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Reset password" }).click();
	await expect(page.getByText("Failed to reset password")).toBeVisible();

	await row(page, username).getByRole("button", { name: "Access" }).click();
	const access = page.getByRole("dialog", { name: "Sign-in access" });
	await access.getByRole("button", { name: "Disable" }).click();
	await page
		.getByRole("dialog", { name: `Disable ${username}?` })
		.getByRole("button", { name: "Disable" })
		.click();

	await expect(page.getByText("Failed to disable the user")).toBeVisible();
	await expect(access.getByText("Can sign in")).toBeVisible();
});

test("says so when deleting a user fails", async ({ page }) => {
	await page.route("**/api/admin/users/*", (route) =>
		route.request().method() === "DELETE"
			? route.fulfill({ status: 400, json: { title: "error.test.unknown" } })
			: route.fallback(),
	);
	await openSection(page, "account");

	await row(page, writerCredentials.username).getByRole("button", { name: "Delete" }).click();
	await page.getByRole("dialog").getByRole("button", { name: "Delete" }).click();

	await expect(page.getByText("Failed to delete user")).toBeVisible();
	await page.goto("/settings?tab=account");
	await expect(row(page, writerCredentials.username)).toHaveCount(1);
});
