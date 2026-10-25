import { describe, expect, it } from "vitest";
import { resolveSettingsCategory, settingsSearchSchema } from "./settingsCategory";

describe("resolveSettingsCategory", () => {
	it("opens the tab from the URL", () => {
		expect(resolveSettingsCategory("advanced", true)).toBe("advanced");
	});

	it("falls back to the first tab the user may see", () => {
		expect(resolveSettingsCategory(undefined, true)).toBe("general");
		expect(resolveSettingsCategory(undefined, false)).toBe("account");
	});

	it("keeps users without settings permission out of the admin tabs", () => {
		expect(resolveSettingsCategory("advanced", false)).toBe("account");
		expect(resolveSettingsCategory("sso", false)).toBe("sso");
	});

	it("keeps old integration links working", () => {
		expect(settingsSearchSchema.parse({ tab: "bgg" })).toEqual({ tab: "integrations" });
		expect(settingsSearchSchema.parse({ tab: "integrations" })).toEqual({ tab: "integrations" });
		expect(settingsSearchSchema.parse({ tab: "nope" })).toEqual({ tab: undefined });
		expect(settingsSearchSchema.parse({})).toEqual({});
	});
});
