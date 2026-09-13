import { describe, expect, it } from "vitest";
import { formatPrice } from "./priceUtils";

describe("formatPrice", () => {
	it("formats with two decimals and a symbol prefix", () => {
		expect(formatPrice(12.5, "€", "en-US")).toBe("€12.50");
	});

	it("uses the locale number format", () => {
		expect(formatPrice(1234.5, "€", "nl-BE")).toBe("€1.234,50");
	});

	it("separates multi-character currency codes with a space", () => {
		expect(formatPrice(12.5, "EUR", "en-US")).toBe("EUR 12.50");
	});

	it("omits the prefix when no currency is known", () => {
		expect(formatPrice(12.5, null, "en-US")).toBe("12.50");
		expect(formatPrice(12.5, "  ", "en-US")).toBe("12.50");
	});

	it("falls back to a fixed format for invalid locales", () => {
		expect(formatPrice(12.5, "€", "not a locale")).toBe("€12.50");
	});
});
