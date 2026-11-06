import type { Page } from "@playwright/test";
import { expect, test, writerCredentials } from "../support/fixtures";
import { api, openContext, openSection } from "../support/settings";

test.use({ allowedApiStatuses: [400, 401, 404] });

const label = "Test login";
const authority = "https://sso.example.test/realms/games";

const field = (page: Page, name: string) => page.getByRole("textbox", { name, exact: true });

const fillForm = async (page: Page, issuer = authority) => {
	await field(page, "Button label").fill(label);
	await field(page, "Authority (issuer URL)").fill(issuer);
	await field(page, "Client ID").fill("board-games");
	await field(page, "Client secret (optional)").fill("client-secret");
};

const trackCreates = (page: Page) => {
	const creates: unknown[] = [];
	page.on("request", (request) => {
		if (request.method() === "POST" && new URL(request.url()).pathname === "/api/admin/oidc-providers") {
			creates.push(request.postDataJSON());
		}
	});
	return creates;
};

const createProvider = async (page: Page) => {
	const response = await (await api(page)).post("/api/admin/oidc-providers", {
		displayName: label,
		authority,
		clientId: "board-games",
		clientSecret: "",
		scopes: "openid profile email",
		enabled: true,
		autoProvisionUsers: true,
		defaultRole: "User",
	});
	expect(response.ok(), await response.text()).toBe(true);
};

test.afterEach(async ({ page }) => {
	const client = await api(page);
	const providers: { id: number }[] = await (await client.get("/api/admin/oidc-providers")).json();
	for (const { id } of providers) await client.delete(`/api/admin/oidc-providers/${id}`);
});

test("refuses a form with missing or invalid fields", async ({ page }) => {
	const creates = trackCreates(page);
	await openSection(page, "sso");

	await page.getByRole("button", { name: "Create provider" }).click();

	await expect(page.getByText("A button label is required")).toBeVisible();
	await expect(page.getByText("Enter the provider's https URL")).toBeVisible();
	await expect(page.getByText("The client ID is required")).toBeVisible();

	await field(page, "Scopes").fill("");
	await expect(page.getByText("At least the openid scope is required")).toBeVisible();

	await field(page, "Authority (issuer URL)").fill("not a url");
	await expect(page.getByText("Enter the provider's https URL")).toBeVisible();
	await field(page, "Authority (issuer URL)").fill(authority);
	await expect(page.getByText("Enter the provider's https URL")).toHaveCount(0);

	expect(creates).toEqual([]);
});

test("refuses plain http for a provider on another machine", async ({ page }) => {
	await openSection(page, "sso");
	await fillForm(page, "http://sso.example.test");

	await page.getByRole("button", { name: "Create provider" }).click();

	await expect(
		page.getByText("The provider URL must use https (http is only allowed for local addresses)"),
	).toBeVisible();
	await expect(page.getByRole("button", { name: "Create provider" })).toBeVisible();
});

test("creates a provider, shows it on the login page and removes it", async ({ page, browser, baseURL }) => {
	const creates = trackCreates(page);
	const context = await openContext(browser, baseURL);
	const visitor = await context.newPage();
	await visitor.goto("/login");
	await visitor.waitForLoadState("networkidle");
	await expect(visitor.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
	await expect(visitor.getByText(label)).toHaveCount(0);

	await openSection(page, "sso");
	await fillForm(page);
	await page.getByRole("button", { name: "Create provider" }).click();

	await expect(page.getByText("Provider saved")).toBeVisible();
	await expect(page.getByText(`Provider ${label} is configured. Changes apply to the next sign-in.`)).toBeVisible();
	await expect(page.getByRole("button", { name: "Save provider" })).toBeVisible();
	await expect(page.getByRole("switch", { name: "Enabled" })).toBeChecked();
	expect(creates).toEqual([
		expect.objectContaining({
			displayName: label,
			authority,
			clientId: "board-games",
			clientSecret: "client-secret",
			scopes: "openid profile email",
			defaultRole: "User",
			autoProvisionUsers: true,
		}),
	]);

	await openSection(page, "sso");
	await expect(field(page, "Button label")).toHaveValue(label);
	await expect(field(page, "Authority (issuer URL)")).toHaveValue(authority);
	await expect(field(page, "Client secret (optional)")).toHaveValue("");
	await expect(field(page, "Client secret (optional)")).toHaveAttribute(
		"placeholder",
		"Stored — leave empty to keep it",
	);

	await visitor.reload();
	await expect(visitor.getByRole("button", { name: new RegExp(label) })).toBeVisible();

	await page.getByRole("button", { name: "Remove provider" }).click();
	const dialog = page.getByRole("dialog");
	await expect(dialog).toContainText(
		"Removing the provider also unlinks every identity provider account. People keep their local password if they have one.",
	);
	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();
	await expect(page.getByRole("button", { name: "Save provider" })).toBeVisible();

	await page.getByRole("button", { name: "Remove provider" }).click();
	await dialog.getByRole("button", { name: "Delete" }).click();

	await expect(page.getByText("Provider removed")).toBeVisible();
	await expect(page.getByRole("button", { name: "Create provider" })).toBeVisible();

	await visitor.reload();
	await visitor.waitForLoadState("networkidle");
	await expect(visitor.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
	await expect(visitor.getByRole("button", { name: new RegExp(label) })).toHaveCount(0);
	await context.close();
});

test("edits a provider, warns about a new authority and hides a disabled provider", async ({
	page,
	browser,
	baseURL,
}) => {
	await createProvider(page);
	await openSection(page, "sso");
	const warning = page.getByText(/^Changing the address unlinks every account linked to this provider/);

	await expect(warning).toHaveCount(0);
	await field(page, "Authority (issuer URL)").fill(`${authority}/other`);
	await expect(warning).toBeVisible();
	await field(page, "Authority (issuer URL)").fill(`${authority.toUpperCase()}/`);
	await expect(warning).toHaveCount(0);
	await field(page, "Authority (issuer URL)").fill(authority);

	await field(page, "Button label").fill("Renamed login");
	await page.getByRole("switch", { name: "Enabled" }).click();
	await page.getByRole("button", { name: "Save provider" }).click();

	await expect(page.getByText("Provider saved")).toBeVisible();
	await expect(page.getByText("Provider Renamed login is configured.", { exact: false })).toBeVisible();

	await openSection(page, "sso");
	await expect(field(page, "Button label")).toHaveValue("Renamed login");
	await expect(page.getByRole("switch", { name: "Enabled" })).not.toBeChecked();

	const context = await openContext(browser, baseURL);
	const visitor = await context.newPage();
	await visitor.goto("/login");
	await visitor.waitForLoadState("networkidle");
	await expect(visitor.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
	await expect(visitor.getByRole("button", { name: /Renamed login/ })).toHaveCount(0);
	await context.close();
});

test("offers linking on the account page and shows the access section for users", async ({ page }) => {
	await createProvider(page);
	await openSection(page, "account");

	await expect(page.getByRole("heading", { name: "Single sign-on" })).toBeVisible();
	await expect(page.getByText("No identity provider account linked yet.")).toBeVisible();
	await expect(page.getByRole("button", { name: `Link ${label}` })).toBeEnabled();

	await page
		.getByRole("row", { name: /^w writer/ })
		.getByRole("button", { name: "Access" })
		.click();
	const dialog = page.getByRole("dialog");

	await expect(dialog.getByText("No identity provider account is linked.")).toBeVisible();
	await expect(dialog.getByText(/blocks sign-in with a password and with single sign-on/)).toBeVisible();
});

test("shows linked logins and unlinks after confirming, with the logins mocked", async ({ page }) => {
	await createProvider(page);
	let unlinked = 0;
	await page.route("**/api/auth/external-logins", (route) =>
		route.fulfill({
			json: [
				{
					id: 7,
					provider: label,
					providerDisplayName: "kermit@example.test",
					providerKey: "subject-1",
					linkedAt: "2026-03-01T10:15:00Z",
				},
			],
		}),
	);
	await page.route("**/api/auth/external-logins/7", (route) => {
		unlinked += 1;
		return route.fulfill({ status: 204 });
	});
	await openSection(page, "account");

	await expect(page.getByText(label, { exact: true })).toBeVisible();
	await expect(page.getByText("kermit@example.test · linked on 01-03-2026 11:15")).toBeVisible();
	await expect(page.getByRole("button", { name: `Link ${label}` })).toHaveCount(0);
	await expect(page.getByText("Your sign-on picture is used until you upload one.")).toBeVisible();

	await page.getByRole("button", { name: "Unlink" }).click();
	const dialog = page.getByRole("dialog");
	await expect(dialog.getByRole("heading", { name: `Unlink ${label}?` })).toBeVisible();
	await dialog.getByRole("button", { name: "Cancel" }).click();
	expect(unlinked).toBe(0);

	await page.getByRole("button", { name: "Unlink" }).click();
	await dialog.getByRole("button", { name: "Unlink" }).click();

	await expect.poll(() => unlinked).toBe(1);
});

test("locks an enabled provider while https is missing until it is switched off", async ({ page }) => {
	await createProvider(page);
	await page.route("**/api/admin/oidc-providers/setup", async (route) => {
		const response = await route.fetch();
		await route.fulfill({ response, json: { ...(await response.json()), httpsRequired: true } });
	});
	await openSection(page, "sso");
	const save = page.getByRole("button", { name: "Save provider" });

	await expect(
		page.getByText(`Provider ${label} is configured but inactive: it needs an https public URL.`),
	).toBeVisible();
	await expect(page.getByRole("alert")).toContainText("Until then you can only");
	await expect(save).toBeDisabled();

	await page.getByRole("switch", { name: "Enabled" }).click();

	await expect(save).toBeEnabled();
	await expect(page.getByRole("switch", { name: "Enabled" })).toBeDisabled();
});

test("manages the linked login of a user, with the logins mocked", async ({ page }) => {
	await createProvider(page);
	const login = {
		id: 7,
		provider: label,
		providerDisplayName: "writer@example.test",
		providerKey: "subject-1",
		linkedAt: "2026-03-01T10:15:00Z",
	};
	const calls: string[] = [];
	await page.route("**/api/admin/users/*/external-logins", (route) => route.fulfill({ json: [login] }));
	await page.route("**/api/admin/users/*/external-logins/7", (route) => {
		calls.push("unlink");
		return route.fulfill({ status: 204 });
	});
	await page.route("**/api/admin/users/*/role/identity-provider", async (route) => {
		calls.push("role");
		const response = await route.fetch({
			method: "GET",
			url: route.request().url().replace("/role/identity-provider", ""),
		});
		await route.fulfill({ json: await response.json() });
	});
	await openSection(page, "account");

	await page
		.getByRole("row")
		.filter({ hasText: writerCredentials.username })
		.getByRole("button", { name: "Access" })
		.click();
	const dialog = page.getByRole("dialog", { name: "Sign-in access" });

	await expect(dialog.getByText(`${label} · writer@example.test`)).toBeVisible();
	await expect(dialog.getByText("subject subject-1 · linked on 01-03-2026 11:15")).toBeVisible();
	await expect(dialog.getByText("The role was set here and the identity provider does not change it.")).toBeVisible();

	await dialog.getByRole("button", { name: "Let the identity provider manage the role" }).click();
	await expect.poll(() => calls).toEqual(["role"]);

	await dialog.getByRole("button", { name: "Unlink" }).click();
	const confirm = page.getByRole("dialog", { name: `Unlink ${label}?` });
	await expect(confirm).toContainText(
		`This identity provider account can no longer sign in as “${writerCredentials.username}” until it is linked again.`,
	);
	await confirm.getByRole("button", { name: "Cancel" }).click();
	expect(calls).toEqual(["role"]);

	await dialog.getByRole("button", { name: "Unlink" }).click();
	await confirm.getByRole("button", { name: "Unlink" }).click();
	await expect.poll(() => calls).toEqual(["role", "unlink"]);
});
