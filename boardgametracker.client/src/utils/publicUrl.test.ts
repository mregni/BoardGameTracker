import { describe, expect, it } from "vitest";
import { effectivePublicUrl, isPublicUrlConfigured } from "./publicUrl";

const origin = "https://games.example.com";

describe("effectivePublicUrl", () => {
	it("uses the configured public URL without a trailing slash", () => {
		expect(effectivePublicUrl("https://bgt.example.org/", origin)).toBe("https://bgt.example.org");
	});

	it("falls back to the address the page was opened on when nothing is configured", () => {
		expect(effectivePublicUrl("", origin)).toBe(origin);
		expect(effectivePublicUrl(undefined, origin)).toBe(origin);
		expect(effectivePublicUrl("  ", origin)).toBe(origin);
		expect(effectivePublicUrl("/", origin)).toBe(origin);
	});

	it("keeps an explicitly configured localhost address", () => {
		expect(effectivePublicUrl("http://localhost:5444", origin)).toBe("http://localhost:5444");
	});
});

describe("isPublicUrlConfigured", () => {
	it("is only false for a blank value, like the server", () => {
		expect(isPublicUrlConfigured("")).toBe(false);
		expect(isPublicUrlConfigured(null)).toBe(false);
		expect(isPublicUrlConfigured(" / ")).toBe(false);
		expect(isPublicUrlConfigured("http://localhost:5444")).toBe(true);
		expect(isPublicUrlConfigured("https://games.example.com")).toBe(true);
	});
});
