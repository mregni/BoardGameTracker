import { describe, expect, it } from "vitest";
import { formatPlayTime } from "./compareUtils";

describe("formatPlayTime", () => {
	it("shows a dash when nothing has been played", () => {
		expect(formatPlayTime(0, "en-us")).toBe("-");
	});

	it("writes out the play time", () => {
		expect(formatPlayTime(90, "en-us")).toBe("1 hour 30 minutes");
	});
});
