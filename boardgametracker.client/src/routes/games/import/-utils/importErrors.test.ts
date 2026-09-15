import { describe, expect, it } from "vitest";
import type { ApiError } from "@/models";
import { classifyImportError } from "./importErrors";

const apiError = (status: number | null, kind: ApiError["kind"] = "client"): ApiError => ({
	kind,
	status,
	message: "x",
	url: "game/bgg/import",
});

describe("classifyImportError", () => {
	it("maps a 429 to rate-limited", () => {
		expect(classifyImportError(apiError(429))).toBe("rate-limited");
	});

	it("maps a 504 to preparing", () => {
		expect(classifyImportError(apiError(504, "server"))).toBe("preparing");
	});

	it("maps a timeout to timeout", () => {
		expect(classifyImportError(apiError(null, "timeout"))).toBe("timeout");
	});

	it("maps 401/400/503 to unauthorized", () => {
		expect(classifyImportError(apiError(401))).toBe("unauthorized");
		expect(classifyImportError(apiError(503, "server"))).toBe("unauthorized");
	});

	it("falls back to unknown", () => {
		expect(classifyImportError(apiError(500, "server"))).toBe("unknown");
		expect(classifyImportError(new Error("boom"))).toBe("unknown");
	});
});
