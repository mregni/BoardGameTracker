import { describe, expect, it } from "vitest";
import { effectivePublicUrl } from "./publicUrl";

const origin = "https://games.example.com";

describe("effectivePublicUrl", () => {
	it("uses the configured public URL without a trailing slash", () => {
		expect(effectivePublicUrl("https://bgt.example.org/", origin)).toBe("https://bgt.example.org");
	});

	it("falls back to the address the page was opened on when nothing is configured", () => {
		expect(effectivePublicUrl("", origin)).toBe(origin);
		expect(effectivePublicUrl(undefined, origin)).toBe(origin);
		expect(effectivePublicUrl("  ", origin)).toBe(origin);
	});

	it("treats the seeded default as not configured", () => {
		expect(effectivePublicUrl("http://localhost:5444", origin)).toBe(origin);
		expect(effectivePublicUrl("http://localhost:5444/", origin)).toBe(origin);
	});
});
