import { describe, expect, it } from "vitest";
import { breaksSingleSignOn, getPublicUrlWarning, stopsSingleSignOn } from "./publicUrlWarning";

const https = { protocol: "https:", host: "games.example.com" };

describe("getPublicUrlWarning", () => {
	it("warns when the public URL is http while the app runs over https", () => {
		expect(getPublicUrlWarning("http://games.example.com", https)).toBe("insecure");
	});

	it("warns when the public URL points to another host", () => {
		expect(getPublicUrlWarning("https://dev-games.example.com", https)).toBe("host-mismatch");
	});

	it("is quiet for a matching https URL, an empty value or an unparsable value", () => {
		expect(getPublicUrlWarning("https://games.example.com/", https)).toBeNull();
		expect(getPublicUrlWarning("", https)).toBeNull();
		expect(getPublicUrlWarning("not a url", https)).toBeNull();
	});
});

describe("breaksSingleSignOn", () => {
	it("flags an http address on a real host", () => {
		expect(breaksSingleSignOn("http://192.168.1.20:8080")).toBe(true);
		expect(breaksSingleSignOn("http://games.example.com")).toBe(true);
	});

	it("allows https and loopback addresses, like the server does", () => {
		expect(breaksSingleSignOn("https://games.example.com")).toBe(false);
		expect(breaksSingleSignOn("http://localhost:5443")).toBe(false);
		expect(breaksSingleSignOn("http://app.localhost")).toBe(false);
		expect(breaksSingleSignOn("http://127.0.0.1:8080")).toBe(false);
		expect(breaksSingleSignOn("http://[::1]:8080")).toBe(false);
	});

	it("ignores a value that is not a URL yet", () => {
		expect(breaksSingleSignOn("games")).toBe(false);
	});
});

describe("stopsSingleSignOn", () => {
	const lan = "http://192.168.1.20:8080";

	it("is true when the new address breaks single sign-on and the saved one did not", () => {
		expect(
			stopsSingleSignOn("http://games.example.com", "https://games.example.com", "https://games.example.com"),
		).toBe(true);
		expect(stopsSingleSignOn("", "https://games.example.com", lan)).toBe(true);
	});

	it("is false when single sign-on keeps working", () => {
		expect(stopsSingleSignOn("https://games.example.com", "", lan)).toBe(false);
		expect(stopsSingleSignOn("", null, "https://games.example.com")).toBe(false);
	});

	it("is false when single sign-on was already stopped by the saved address", () => {
		expect(stopsSingleSignOn("http://games.example.com", "", lan)).toBe(false);
		expect(stopsSingleSignOn("", null, lan)).toBe(false);
	});
});
