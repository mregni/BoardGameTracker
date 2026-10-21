import { describe, expect, it } from "vitest";
import { effectivePublicUrl, isPublicUrlConfigured, publicHostLoginUrl } from "./publicUrl";

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

describe("publicHostLoginUrl", () => {
	it("points to the login page of an https public URL on another origin", () => {
		expect(publicHostLoginUrl("https://bgt.example.org/base/", "http://localhost:5444", "/games?x=1")).toBe(
			"https://bgt.example.org/base/login?redirect=%2Fgames%3Fx%3D1&sso=true",
		);
	});

	it("keeps only a local redirect path", () => {
		expect(publicHostLoginUrl("https://bgt.example.org", "http://localhost:5444", "//evil.example")).toBe(
			"https://bgt.example.org/login?redirect=%2F&sso=true",
		);
		expect(publicHostLoginUrl("https://bgt.example.org", "http://localhost:5444", undefined)).toBe(
			"https://bgt.example.org/login?redirect=%2F&sso=true",
		);
	});

	it.each([
		[null, origin],
		["", origin],
		["not a url", origin],
		["http://192.168.1.20:5444", "http://localhost:5444"],
		["https://games.example.com/", origin],
		["https://GAMES.example.com:443", origin],
	])("stays on this address for %s", (configured, current) => {
		expect(publicHostLoginUrl(configured, current, "/games")).toBeNull();
	});
});
