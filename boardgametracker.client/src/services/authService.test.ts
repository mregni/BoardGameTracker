import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ApiError } from "@/models/Common/ApiError";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import {
	disableUserCall,
	enableUserCall,
	endOidcSessionCall,
	getOidcProviderCall,
	getUserExternalLoginsCall,
	manageRoleByIdentityProviderCall,
	removeProfileImageCall,
	startOidcLinkCall,
	unlinkUserExternalLoginCall,
	uploadProfileImageCall,
} from "./authService";

const apiError = (status: number): ApiError => ({
	kind: "client",
	status,
	message: "Not Found",
	url: "auth/oidc/provider",
});

describe("getOidcProviderCall", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
	});

	it("returns the configured provider", async () => {
		const provider = { displayName: "Company login", iconUrl: null, buttonColor: null };
		axiosMock.get.mockResolvedValue({ data: provider });

		await expect(getOidcProviderCall()).resolves.toEqual(provider);
	});

	it("returns null instead of failing when no provider is configured", async () => {
		axiosMock.get.mockRejectedValue(apiError(404));

		await expect(getOidcProviderCall()).resolves.toBeNull();
	});

	it("still fails on other errors", async () => {
		axiosMock.get.mockRejectedValue(apiError(500));

		await expect(getOidcProviderCall()).rejects.toMatchObject({ status: 500 });
	});
});

describe("startOidcLinkCall", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
	});

	it("starts linking at the fixed route without a provider segment", async () => {
		axiosMock.get.mockResolvedValue({ data: { url: "https://sso.example.com/authorize?state=abc" } });

		await expect(startOidcLinkCall()).resolves.toBe("https://sso.example.com/authorize?state=abc");
		expect(axiosMock.get).toHaveBeenCalledWith("auth/oidc/link");
	});
});

describe("endOidcSessionCall", () => {
	beforeEach(() => {
		axiosMock.post.mockReset();
	});

	it("returns the identity provider sign-out address", async () => {
		axiosMock.post.mockResolvedValue({ data: { url: "https://sso.example.com/logout" } });

		await expect(endOidcSessionCall()).resolves.toBe("https://sso.example.com/logout");
		expect(axiosMock.post).toHaveBeenCalledWith("auth/oidc/logout");
	});

	it("returns null when there is no single sign-on session", async () => {
		axiosMock.post.mockResolvedValue({ data: { url: null } });

		await expect(endOidcSessionCall()).resolves.toBeNull();
	});
});

describe("admin user access calls", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
		axiosMock.post.mockReset();
		axiosMock.delete.mockReset();
	});

	it.each([
		[disableUserCall, "admin/users/u-1/disable"],
		[enableUserCall, "admin/users/u-1/enable"],
		[manageRoleByIdentityProviderCall, "admin/users/u-1/role/identity-provider"],
	])("posts to %#", async (call, url) => {
		axiosMock.post.mockResolvedValue({ data: { id: "u-1" } });

		await expect(call("u-1")).resolves.toEqual({ id: "u-1" });
		expect(axiosMock.post).toHaveBeenCalledWith(url);
	});

	it("lists and unlinks the identities of a user", async () => {
		axiosMock.get.mockResolvedValue({ data: [{ id: 3 }] });
		axiosMock.delete.mockResolvedValue({});

		await expect(getUserExternalLoginsCall("u-1")).resolves.toEqual([{ id: 3 }]);
		await unlinkUserExternalLoginCall("u-1", 3);

		expect(axiosMock.get).toHaveBeenCalledWith("admin/users/u-1/external-logins");
		expect(axiosMock.delete).toHaveBeenCalledWith("admin/users/u-1/external-logins/3");
	});
});

describe("profile picture calls", () => {
	beforeEach(() => {
		axiosMock.put.mockReset();
		axiosMock.delete.mockReset();
	});

	it("uploads the picture as a form file", async () => {
		const profile = { id: "u1", profileImage: "/images/profile/a.webp" };
		axiosMock.put.mockResolvedValue({ data: profile });
		const file = new File(["png"], "me.png", { type: "image/png" });

		await expect(uploadProfileImageCall(file)).resolves.toEqual(profile);

		const [url, body, config] = axiosMock.put.mock.calls[0];
		expect(url).toBe("auth/profile/image");
		expect((body as FormData).get("file")).toBe(file);
		expect(config).toEqual({ headers: { "Content-Type": "multipart/form-data" } });
	});

	it("removes the picture", async () => {
		axiosMock.delete.mockResolvedValue({ data: { id: "u1", profileImage: null } });

		await expect(removeProfileImageCall()).resolves.toEqual({ id: "u1", profileImage: null });
		expect(axiosMock.delete).toHaveBeenCalledWith("auth/profile/image");
	});
});
