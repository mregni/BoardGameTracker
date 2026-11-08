import { describe, expect, it, vi } from "vitest";
import type { ApiError } from "@/models";
import { apiErrorMessage, translateApiError, translateUrlError } from "./errorUtils";

const translations: Record<string, string> = {
	"error:auth.username-already-exists": "Username already exists",
	"error:manual.not-pdf": '"{{fileName}}" is not a PDF file',
	"error:something-went-wrong": "Something went wrong",
	"error:not-found": "This item no longer exists",
	"settings:save.failed": "Saving failed",
	"error:auth.oidc-failed": "Signing in failed",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string; fileName?: string }) =>
			(translations[key] ?? options?.defaultValue ?? key).replace("{{fileName}}", options?.fileName ?? "{{fileName}}"),
		exists: (key: string) => key in translations,
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
		expect(translateApiError("Some plain server text")).toBe("Some plain server text");
	});

	it("fills the parameters the server sends into the translation", () => {
		expect(translateApiError("error.manual.not-pdf", undefined, { fileName: "rules.txt" })).toBe(
			'"rules.txt" is not a PDF file',
		);
	});

	it("translates the plain texts the server sends", () => {
		expect(translateApiError("The requested resource was not found.")).toBe("This item no longer exists");
	});

	it.each(["An unexpected error occurred", "Bad Request", "One or more validation errors occurred."])(
		"uses the fallback for %s, which gives no reason",
		(message) => {
			expect(translateApiError(message, "settings:save.failed")).toBe("Saving failed");
		},
	);

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

	it("passes the parameters of client errors to the translation", () => {
		const error: ApiError = { ...clientError("error.manual.not-pdf"), params: { fileName: "rules.txt" } };
		expect(apiErrorMessage(error, "settings:save.failed")).toBe('"rules.txt" is not a PDF file');
	});

	it("uses the fallback for non-client errors", () => {
		const serverError: ApiError = { kind: "server", status: 500, message: "boom", url: "/api/x" };
		expect(apiErrorMessage(serverError, "settings:save.failed")).toBe("Saving failed");
	});

	it("uses the fallback for non-api errors", () => {
		expect(apiErrorMessage(new Error("boom"), "settings:save.failed")).toBe("Saving failed");
	});
});

describe("translateUrlError", () => {
	it("translates a known error key", () => {
		expect(translateUrlError("error.auth.username-already-exists")).toBe("Username already exists");
	});

	it.each([
		undefined,
		"",
		"error.auth.unknown-code",
		"Your account is locked, call 0800 123 456",
		"<script>alert(1)</script>",
		"auth.username-already-exists",
	])("shows the generic sign-in failure for %s", (value) => {
		expect(translateUrlError(value)).toBe("Signing in failed");
	});

	it("uses the provided fallback key", () => {
		expect(translateUrlError("plain text", "settings:save.failed")).toBe("Saving failed");
	});
});
