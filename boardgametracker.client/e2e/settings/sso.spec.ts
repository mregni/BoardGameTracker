import type { Page } from "@playwright/test";
import { expect, test } from "../support/fixtures";
import { openSection } from "../support/settings";

const issuer = "https://sso.example.test/realms/games";
const discovery = {
	issuer,
	issuerMatchesAuthority: true,
	authorizationEndpoint: `${issuer}/authorize`,
	tokenEndpoint: `${issuer}/token`,
	userInfoEndpoint: `${issuer}/userinfo`,
	endSessionEndpoint: null,
	signingAlgorithms: ["RS256", "ES256"],
	signingSupported: true,
	signingCertificateExpiredOn: null,
};

const authority = (page: Page) => page.getByRole("textbox", { name: "Authority (issuer URL)" });
const testButton = (page: Page) => page.getByRole("button", { name: "Test discovery" });

const runDiscovery = async (page: Page, json: unknown) => {
	await page.route("**/api/admin/oidc-providers/test-discovery", (route) => route.fulfill({ json }));
	await openSection(page, "sso");
	await authority(page).fill(issuer);
	await testButton(page).click();
};

test("offers an empty form with the default claims", async ({ page }) => {
	await openSection(page, "sso");

	await expect(page.getByRole("heading", { name: "Identity provider" })).toBeVisible();
	await expect(page.getByText(/^Let people sign in through your identity provider/)).toBeVisible();
	for (const [name, value] of [
		["Button label", ""],
		["Authority (issuer URL)", ""],
		["Client ID", ""],
		["Client secret (optional)", ""],
		["Scopes", "openid profile email"],
		["Username claim", "preferred_username"],
		["Email claim", "email"],
		["Display name claim", "name"],
		["Avatar claim", "picture"],
		["Groups/roles claim", "groups"],
		["Admin group value", "bgt-admins"],
	]) {
		await expect(page.getByRole("textbox", { name, exact: true })).toHaveValue(value);
	}
	await expect(page.getByRole("textbox", { name: "Client secret (optional)" })).toHaveAttribute(
		"placeholder",
		"Leave empty for a public client",
	);
	await expect(page.getByRole("combobox", { name: "Default role" })).toHaveText("User");
	await expect(page.getByRole("switch", { name: "Create accounts on first sign-in" })).toBeChecked();
	await expect(page.getByRole("switch", { name: "Enabled" })).toHaveCount(0);
	await expect(page.getByRole("button", { name: "Create provider" })).toBeEnabled();
	await expect(page.getByRole("button", { name: "Remove provider" })).toHaveCount(0);

	await page.getByRole("combobox", { name: "Default role" }).click();
	await expect(page.getByRole("option")).toHaveText(["User", "Reader"]);
});

test("warns when the role sync falls back to its defaults", async ({ page }) => {
	await openSection(page, "sso");
	const warning = page.getByTestId("sso-role-sync-defaults");

	await expect(warning).toHaveCount(0);

	await page.getByRole("textbox", { name: "Groups/roles claim" }).fill("");
	await expect(warning).toHaveText(
		"An empty groups claim or admin group falls back to the default (groups and bgt-admins), so role sync stays on.",
	);

	await page.getByRole("textbox", { name: "Groups/roles claim" }).fill("roles");
	await expect(warning).toHaveCount(0);
});

test("shows the redirect addresses and copies them", async ({ page, baseURL }) => {
	await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
	await openSection(page, "sso");
	const uris = page.getByTestId("sso-redirect-uris");
	const expectedUris = [
		`${baseURL}/api/auth/oidc/callback`,
		`${baseURL}/api/auth/oidc/link-callback`,
		`${baseURL}/login`,
	];

	await expect(page.getByRole("heading", { name: "Register at the provider" })).toBeVisible();
	await expect(uris.locator("code")).toHaveText(expectedUris);
	await expect(page.getByText(/^No public URL is configured/)).toHaveCount(0);

	for (const [index, uri] of expectedUris.entries()) {
		await uris.getByRole("button", { name: "Copy" }).nth(index).click();
		await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(uri);
	}
});

test("warns when no public address is configured", async ({ page, baseURL }) => {
	await page.route("**/api/admin/oidc-providers/setup", async (route) => {
		const response = await route.fetch();
		await route.fulfill({
			response,
			json: { ...(await response.json()), publicUrlConfigured: false, publicBaseUrl: baseURL },
		});
	});
	await openSection(page, "sso");

	await expect(
		page.getByText(
			`No public URL is configured, so the redirect URIs are derived from the current request (${baseURL}).`,
			{
				exact: false,
			},
		),
	).toBeVisible();
});

test("locks the form hints while https is missing", async ({ page }) => {
	await page.route("**/api/admin/oidc-providers/setup", async (route) => {
		const response = await route.fetch();
		await route.fulfill({ response, json: { ...(await response.json()), httpsRequired: true } });
	});
	await openSection(page, "sso");

	await expect(page.getByRole("alert")).toContainText(
		"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
	);
	await expect(page.getByText("Available once the public URL uses https.")).toBeVisible();
	for (const copy of await page.getByRole("button", { name: "Copy" }).all()) {
		await expect(copy).toBeDisabled();
	}
});

test("enables the discovery test only with an authority", async ({ page }) => {
	await openSection(page, "sso");

	await expect(testButton(page)).toBeDisabled();

	await authority(page).fill("   ");
	await expect(testButton(page)).toBeDisabled();

	await authority(page).fill(issuer);
	await expect(testButton(page)).toBeEnabled();
});

test.describe("when the provider cannot be reached", () => {
	test.use({ allowedApiStatuses: [400, 401, 404] });

	test("shows why the discovery test failed", async ({ page }) => {
		await openSection(page, "sso");
		await authority(page).fill("https://sso.invalid");

		await testButton(page).click();

		await expect(
			page.getByText("The identity provider could not be reached or has no discovery document at this address.", {
				exact: false,
			}),
		).toBeVisible();
		await expect(page.getByText("Authorization:")).toHaveCount(0);
	});
});

test("shows the endpoints of a working provider", async ({ page }) => {
	await runDiscovery(page, discovery);

	await expect(page.getByText("The provider answered and its issuer matches the authority.")).toBeVisible();
	await expect(page.getByText(`Authorization: ${discovery.authorizationEndpoint}`)).toBeVisible();
	await expect(page.getByText(`Token: ${discovery.tokenEndpoint}`)).toBeVisible();
	await expect(page.getByText(`User info: ${discovery.userInfoEndpoint}`)).toBeVisible();
	await expect(page.getByText("Sign-out: not offered")).toBeVisible();
	await expect(page.getByText("Signing algorithms: RS256, ES256")).toBeVisible();
	await expect(page.getByText(/^The provider only signs tokens/)).toHaveCount(0);
});

test("warns about another issuer", async ({ page }) => {
	await runDiscovery(page, { ...discovery, issuer: "https://other.example.test", issuerMatchesAuthority: false });

	await expect(
		page.getByText(
			"The provider answered, but reports issuer https://other.example.test. Use that value as the authority.",
		),
	).toBeVisible();
});

test("warns about unsupported signing and an expired certificate", async ({ page }) => {
	await runDiscovery(page, {
		...discovery,
		signingAlgorithms: ["HS256"],
		signingSupported: false,
		signingCertificateExpiredOn: "2026-02-03T12:00:00Z",
	});

	await expect(page.getByText(/^The provider only signs tokens with HS256/)).toBeVisible();
	await expect(page.getByText(/^The provider's signing certificate expired on 03-02-2026\./)).toBeVisible();
});
