import { describe, expect, it } from "vitest";
import { getPublicUrlWarning } from "./publicUrlWarning";

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
