import { describe, expect, it, vi } from "vitest";
import type { ApiError } from "@/models";
import { apiErrorMessage, translateApiError } from "./errorUtils";

const translations: Record<string, string> = {
	"error:auth.username-already-exists": "Username already exists",
	"error:something-went-wrong": "Something went wrong",
	"settings:save.failed": "Saving failed",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) => translations[key] ?? options?.defaultValue ?? key,
	},
}));

const clientError = (message: string, status = 400): ApiError => ({ kind: "client", status, message, url: "/api/x" });

describe("translateApiError", () => {
	it("translates backend reason codes through the error namespace", () => {
		expect(translateApiError("error.auth.username-already-exists")).toBe("Username already exists");
	});

	it("falls back to the generic message for unknown reason codes", () => {
		expect(translateApiError("error.auth.unknown-code")).toBe("Something went wrong");
	});

	it("uses the provided fallback key for unknown reason codes", () => {
		expect(translateApiError("error.auth.unknown-code", "settings:save.failed")).toBe("Saving failed");
	});

	it("returns plain messages untouched", () => {
		expect(translateApiError("Invalid request.")).toBe("Invalid request.");
	});

	it("falls back when the message is empty", () => {
		expect(translateApiError(undefined)).toBe("Something went wrong");
		expect(translateApiError("")).toBe("Something went wrong");
	});
});

describe("apiErrorMessage", () => {
	it("translates client errors", () => {
		expect(apiErrorMessage(clientError("error.auth.username-already-exists"), "settings:save.failed")).toBe(
			"Username already exists",
		);
	});

	it("uses the fallback for non-client errors", () => {
		const serverError: ApiError = { kind: "server", status: 500, message: "boom", url: "/api/x" };
		expect(apiErrorMessage(serverError, "settings:save.failed")).toBe("Saving failed");
	});

	it("uses the fallback for non-api errors", () => {
		expect(apiErrorMessage(new Error("boom"), "settings:save.failed")).toBe("Saving failed");
	});
});
