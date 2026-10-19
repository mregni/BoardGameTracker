import { describe, expect, it } from "vitest";
import { getGameNightByLink } from "./gameNights";
import { getGameNightManuals } from "./manuals";

describe("public game night queries", () => {
	it("keep the global error toast quiet, the RSVP page explains an unknown link itself", () => {
		expect(getGameNightByLink("abc").meta).toEqual({ silent: true });
		expect(getGameNightManuals("abc").meta).toEqual({ silent: true });
	});
});
