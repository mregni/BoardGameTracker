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

	it("omits decimals for whole amounts", () => {
		expect(formatPrice(140, "€", "en-US")).toBe("€140");
		expect(formatPrice(1400, "€", "nl-BE")).toBe("€1.400");
	});

	it("keeps two decimals when the amount is not whole", () => {
		expect(formatPrice(63.333, "€", "nl-BE")).toBe("€63,33");
	});

	it("renders a dash when there is no amount", () => {
		expect(formatPrice(null, "€", "en-US")).toBe("-");
		expect(formatPrice(undefined, "€", "en-US")).toBe("-");
	});

	it("falls back to a fixed format for invalid locales", () => {
		expect(formatPrice(12.5, "€", "not a locale")).toBe("€12.50");
	});
});
