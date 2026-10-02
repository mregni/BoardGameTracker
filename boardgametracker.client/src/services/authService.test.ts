import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ApiError } from "@/models/Common/ApiError";

const axiosMock = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { getOidcProviderCall } from "./authService";

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
		const provider = { name: "keycloak", displayName: "Company login", iconUrl: null, buttonColor: null };
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
