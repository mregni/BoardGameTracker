import { describe, expect, it } from "vitest";
import { isPriceError, isPriceSettingsError, priceErrorKey } from "./GamePrice";

describe("GamePrice status helpers", () => {
	it("treats only invalid settings and a rejected key as settings errors", () => {
		expect(isPriceSettingsError("misconfigured")).toBe(true);
		expect(isPriceSettingsError("unauthorized")).toBe(true);
		expect(isPriceSettingsError("unreachable")).toBe(false);
		expect(isPriceSettingsError("parseError")).toBe(false);
		expect(isPriceSettingsError(undefined)).toBe(false);
	});

	it("flags error statuses and builds their translation key", () => {
		expect(isPriceError("unreachable")).toBe(true);
		expect(isPriceError("pending")).toBe(false);
		expect(priceErrorKey("unauthorized")).toBe("game:price.unavailable.unauthorized");
	});
});
