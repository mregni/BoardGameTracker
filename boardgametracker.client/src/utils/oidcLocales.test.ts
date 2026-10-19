import { describe, expect, it } from "vitest";
import baseAuth from "../../public/locales/base/auth.json";
import baseError from "../../public/locales/base/error.json";
import baseSettings from "../../public/locales/base/settings.json";
import enAuth from "../../public/locales/en-US/auth.json";
import enError from "../../public/locales/en-US/error.json";
import enSettings from "../../public/locales/en-US/settings.json";
import esAuth from "../../public/locales/es-ES/auth.json";
import esError from "../../public/locales/es-ES/error.json";
import esSettings from "../../public/locales/es-ES/settings.json";
import nlBeAuth from "../../public/locales/nl-BE/auth.json";
import nlBeError from "../../public/locales/nl-BE/error.json";
import nlBeSettings from "../../public/locales/nl-BE/settings.json";
import nlNlAuth from "../../public/locales/nl-NL/auth.json";
import nlNlError from "../../public/locales/nl-NL/error.json";
import nlNlSettings from "../../public/locales/nl-NL/settings.json";

const locales = {
	base: { auth: baseAuth, error: baseError, settings: baseSettings },
	"en-US": { auth: enAuth, error: enError, settings: enSettings },
	"es-ES": { auth: esAuth, error: esError, settings: esSettings },
	"nl-BE": { auth: nlBeAuth, error: nlBeError, settings: nlBeSettings },
	"nl-NL": { auth: nlNlAuth, error: nlNlError, settings: nlNlSettings },
};

const lookup = (node: unknown, path: string): unknown =>
	path.split(".").reduce<unknown>((current, part) => (current as Record<string, unknown> | undefined)?.[part], node);

const keys = {
	error: [
		"auth.oidc-id-token-invalid",
		"auth.oidc-username-taken",
		"auth.oidc-provider-name-invalid",
		"auth.oidc-role-not-managed",
		"auth.account-disabled",
		"auth.cannot-disable-self",
		"auth.cannot-disable-last-admin",
		"auth.oidc-https-required",
	],
	auth: ["oidc.sign-in-with-password"],
	settings: [
		"sso.fields.name.rename-warning",
		"sso.fields.default-role.label",
		"sso.fields.role-sync",
		"sso.discovery.end-session",
		"sso.discovery.none",
		"sso.setup.post-logout-redirect-uri",
		"account.users.access.title",
		"account.users.access.disable-hint",
		"account.users.access.unlink-confirm",
		"account.users.access.manage-role",
		"account.notifications.user-disabled",
		"account.notifications.external-login-unlinked",
		"sso.https-required",
		"sso.fields.enabled.https-required",
	],
} as const;

describe("OIDC phase 3 translations", () => {
	it.each(Object.entries(locales))("are present in %s", (_locale, files) => {
		for (const [file, paths] of Object.entries(keys)) {
			for (const path of paths) {
				expect(lookup(files[file as keyof typeof files], path), `${file}:${path}`).toEqual(expect.any(String));
			}
		}
	});

	it("tells an IdP user with a clashing username exactly how to continue", () => {
		const message =
			"An account with this username already exists. Sign in with your password and link SSO under Settings → Account.";

		expect(lookup(enError, "auth.oidc-username-taken")).toBe(message);
		expect(lookup(baseError, "auth.oidc-username-taken")).toBe(message);
	});
});
