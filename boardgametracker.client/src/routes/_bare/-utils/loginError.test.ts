import { describe, expect, it } from "vitest";
import type { ApiError } from "@/models";
import { loginErrorKey } from "./loginError";

const apiError = (
	status: number | null,
	kind: ApiError["kind"],
	message = "An unexpected error occurred",
): ApiError => ({
	kind,
	status,
	message,
	url: "auth/login",
});

describe("loginErrorKey", () => {
	it("translates the reason code the server sends", () => {
		expect(loginErrorKey(apiError(401, "client", "error.auth.account-locked-out"))).toBe(
			"error:auth.account-locked-out",
		);
		expect(loginErrorKey(apiError(401, "client", "error.auth.invalid-credentials"))).toBe(
			"error:auth.invalid-credentials",
		);
	});

	it("reports the rate limiter's empty 429 as too many attempts", () => {
		expect(loginErrorKey(apiError(429, "client"))).toBe("error:auth.too-many-requests");
	});

	it("reports network, timeout and server failures as such instead of bad credentials", () => {
		expect(loginErrorKey(apiError(null, "network", "Network error"))).toBe("error:network");
		expect(loginErrorKey(apiError(null, "timeout", "Request timed out"))).toBe("error:timeout");
		expect(loginErrorKey(apiError(500, "server", "error.unexpected"))).toBe("error:server");
	});

	it("falls back to the default message for anything else", () => {
		expect(loginErrorKey(apiError(400, "client"))).toBeNull();
		expect(loginErrorKey(new Error("boom"))).toBeNull();
	});
});
