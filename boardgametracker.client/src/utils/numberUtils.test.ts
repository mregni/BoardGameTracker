import { describe, expect, it } from "vitest";

import {
	formatDecimal,
	formatDecimalInput,
	formatFileSize,
	formatInteger,
	formatNumber,
	formatPercent,
	formatPrice,
	formatPriceOrEmpty,
	formatRatio,
	getIntegerTicks,
	parseDecimal,
	toIsoCurrency,
} from "./numberUtils";

const NBSP = " ";

describe("numberUtils", () => {
	describe("formatDecimal", () => {
		it.each([
			["en-US", "8.1"],
			["nl-BE", "8,1"],
			["nl-NL", "8,1"],
			["es-ES", "8,1"],
		])("uses the %s decimal separator", (locale, expected) => {
			expect(formatDecimal(8.1, locale)).toBe(expected);
		});

		it("rounds to the maximum number of fraction digits", () => {
			expect(formatDecimal(8.149, "en-US")).toBe("8.1");
			expect(formatDecimal(8.15, "nl-BE", 1)).toBe("8,2");
			expect(formatDecimal(2.456, "es-ES", 2)).toBe("2,46");
		});

		it("drops trailing zeros unless a minimum is asked for", () => {
			expect(formatDecimal(8, "nl-BE")).toBe("8");
			expect(formatDecimal(8, "nl-BE", 1, 1)).toBe("8,0");
		});

		it("formats negatives", () => {
			expect(formatDecimal(-3.25, "en-US", 2)).toBe("-3.25");
			expect(formatDecimal(-3.25, "nl-NL", 2)).toBe("-3,25");
		});

		it("formats zero and never shows a negative zero", () => {
			expect(formatDecimal(0, "en-US")).toBe("0");
			expect(formatDecimal(-0.04, "nl-BE")).toBe("0");
			expect(formatDecimal(-0, "es-ES")).toBe("0");
		});

		it("renders a dash for missing values", () => {
			expect(formatDecimal(null, "en-US")).toBe("-");
			expect(formatDecimal(undefined, "nl-BE")).toBe("-");
			expect(formatDecimal(Number.NaN, "es-ES")).toBe("-");
		});
	});

	describe("formatInteger", () => {
		it.each([
			["en-US", "12,345"],
			["nl-BE", "12.345"],
			["nl-NL", "12.345"],
			["es-ES", "12.345"],
		])("groups thousands for %s", (locale, expected) => {
			expect(formatInteger(12345, locale)).toBe(expected);
		});

		it("rounds to a whole number", () => {
			expect(formatInteger(4.5, "en-US")).toBe("5");
			expect(formatInteger(-4.6, "nl-BE")).toBe("-5");
			expect(formatInteger(null, "en-US")).toBe("-");
		});
	});

	describe("formatPercent", () => {
		it.each([
			["en-US", "66.7%"],
			["nl-BE", "66,7%"],
			["nl-NL", "66,7%"],
			["es-ES", `66,7${NBSP}%`],
		])("formats a fraction for %s", (locale, expected) => {
			expect(formatPercent(2 / 3, locale, 1)).toBe(expected);
		});

		it("rounds to whole percentages by default", () => {
			expect(formatPercent(0.255, "en-US")).toBe("26%");
			expect(formatPercent(0, "nl-BE")).toBe("0%");
			expect(formatPercent(-0.001, "en-US")).toBe("0%");
			expect(formatPercent(-0.25, "en-US")).toBe("-25%");
			expect(formatPercent(null, "en-US")).toBe("-");
		});
	});

	describe("formatRatio", () => {
		it("divides and formats as a percentage", () => {
			expect(formatRatio(1, 3, "en-US")).toBe("33%");
			expect(formatRatio(1, 3, "nl-BE", 1)).toBe("33,3%");
		});

		it("returns zero percent when the total is zero", () => {
			expect(formatRatio(5, 0, "en-US")).toBe("0%");
		});
	});

	describe("formatNumber", () => {
		it("keeps up to three fraction digits by default", () => {
			expect(formatNumber(1234.5678, "en-US")).toBe("1,234.568");
			expect(formatNumber(1234.5678, "nl-BE")).toBe("1.234,568");
		});

		it("falls back to en-US for an invalid locale", () => {
			expect(formatNumber(1.5, "not a locale")).toBe("1.5");
		});
	});

	describe("formatPriceOrEmpty", () => {
		it("gives a dash for a missing or zero price and the price otherwise", () => {
			expect(formatPriceOrEmpty(null, "EUR", "en-US")).toBe("–");
			expect(formatPriceOrEmpty(0, "EUR", "en-US")).toBe("–");
			expect(formatPriceOrEmpty(12, "EUR", "en-US")).toBe(formatPrice(12, "EUR", "en-US"));
		});
	});

	describe("formatPrice", () => {
		const nbsp = (value: string) => value.replace(/\s/g, "\u00a0");

		it("formats with two decimals and a symbol prefix", () => {
			expect(formatPrice(12.5, "€", "en-US")).toBe("€12.50");
		});

		it.each([
			["nl-BE", "€ 1.234,50"],
			["nl-NL", "€ 1.234,50"],
			["es-ES", "1234,50 €"],
			["en-US", "€1,234.50"],
		])("places the euro sign the way %s does", (locale, expected) => {
			expect(nbsp(formatPrice(1234.5, "€", locale))).toBe(nbsp(expected));
		});

		it("shows an ISO code setting with its symbol in the culture's place", () => {
			expect(formatPrice(12.5, "EUR", "en-US")).toBe("€12.50");
			expect(nbsp(formatPrice(169, "eur", "es-ES"))).toBe(nbsp("169 €"));
			expect(formatPrice(12.5, "USD", "en-US")).toBe("$12.50");
		});

		it("keeps an unknown symbol as a prefix", () => {
			expect(formatPrice(12.5, "kr", "en-US")).toBe("kr 12.50");
			expect(formatPrice(12.5, "#", "en-US")).toBe("#12.50");
		});

		it("maps known symbols and three-letter codes to ISO currencies", () => {
			expect(toIsoCurrency("€")).toBe("EUR");
			expect(toIsoCurrency(" £ ")).toBe("GBP");
			expect(toIsoCurrency("chf")).toBe("CHF");
			expect(toIsoCurrency("kr")).toBeNull();
			expect(toIsoCurrency(null)).toBeNull();
		});

		it("omits the prefix when no currency is known", () => {
			expect(formatPrice(12.5, null, "en-US")).toBe("12.50");
			expect(formatPrice(12.5, "  ", "en-US")).toBe("12.50");
		});

		it("omits decimals for whole amounts", () => {
			expect(formatPrice(140, "€", "en-US")).toBe("€140");
			expect(nbsp(formatPrice(1400, "€", "nl-BE"))).toBe(nbsp("€ 1.400"));
			expect(nbsp(formatPrice(0, "€", "nl-BE"))).toBe(nbsp("€ 0"));
		});

		it("keeps two decimals when the amount is not whole", () => {
			expect(nbsp(formatPrice(63.333, "€", "nl-BE"))).toBe(nbsp("€ 63,33"));
			expect(formatPrice(-4.5, "$", "en-US")).toBe("-$4.50");
		});

		it("renders a dash when there is no amount", () => {
			expect(formatPrice(null, "€", "en-US")).toBe("-");
			expect(formatPrice(undefined, "€", "en-US")).toBe("-");
		});

		it("falls back to en-US for invalid locales", () => {
			expect(formatPrice(12.5, "€", "not a locale")).toBe("€12.50");
		});
	});

	describe("formatDecimalInput", () => {
		it("shows the value with the culture separator and no grouping", () => {
			expect(formatDecimalInput(8.1, "nl-BE")).toBe("8,1");
			expect(formatDecimalInput(8.1, "en-US")).toBe("8.1");
			expect(formatDecimalInput(1234.56, "nl-NL")).toBe("1234,56");
			expect(formatDecimalInput(1234.56, "es-ES")).toBe("1234,56");
		});

		it("is empty for missing values", () => {
			expect(formatDecimalInput(null, "nl-BE")).toBe("");
			expect(formatDecimalInput(undefined, "nl-BE")).toBe("");
		});
	});

	describe("parseDecimal", () => {
		it.each([
			["8,1", 8.1],
			["8.1", 8.1],
			[" 2,45 ", 2.45],
			["-3,5", -3.5],
			["-3.5", -3.5],
			["0", 0],
			[",5", 0.5],
			["7,", 7],
			["1.234,5", 1234.5],
			["1,234.5", 1234.5],
			["1 234,5", 1234.5],
			["12", 12],
		])("parses %s", (input, expected) => {
			expect(parseDecimal(input)).toBe(expected);
		});

		it.each([[""], ["   "], ["abc"], ["8,1x"], ["-"], [","], [null], [undefined]])("returns null for %s", (input) => {
			expect(parseDecimal(input)).toBeNull();
		});

		it.each([
			["en-US", "1,250", 1250],
			["en-US", "1.5", 1.5],
			["en-US", "1,234.5", 1234.5],
			["nl-BE", "1.250", 1250],
			["nl-BE", "1,5", 1.5],
			["nl-BE", "3.25", 3.25],
			["en-US", "1,234,567", 1234567],
			["nl-BE", "1.234,5", 1234.5],
			[undefined, "1,250", 1.25],
			[undefined, "1.250", 1.25],
		])("parses with locale %s: %s", (locale, input, expected) => {
			expect(parseDecimal(input, locale)).toBe(expected);
		});
	});

	describe("formatFileSize", () => {
		it("should format bytes below a kilobyte", () => {
			expect(formatFileSize(0, "en-US")).toBe("0 B");
			expect(formatFileSize(512, "en-US")).toBe("512 B");
			expect(formatFileSize(1023, "en-US")).toBe("1,023 B");
		});

		it("should format kilobytes with one decimal", () => {
			expect(formatFileSize(1024, "en-US")).toBe("1.0 KB");
			expect(formatFileSize(1536, "nl-BE")).toBe("1,5 KB");
			expect(formatFileSize(1048575, "en-US")).toBe("1,024.0 KB");
		});

		it("should format megabytes with one decimal", () => {
			expect(formatFileSize(1048576, "en-US")).toBe("1.0 MB");
			expect(formatFileSize(5242880, "es-ES")).toBe("5,0 MB");
			expect(formatFileSize(1572864, "nl-NL")).toBe("1,5 MB");
		});
	});

	describe("getIntegerTicks", () => {
		it("should give one tick per whole number for small counts", () => {
			expect(getIntegerTicks(2)).toEqual([0, 1, 2]);
			expect(getIntegerTicks(5)).toEqual([0, 1, 2, 3, 4, 5]);
		});

		it("should keep at least one step when there is no data", () => {
			expect(getIntegerTicks(0)).toEqual([0, 1]);
		});

		it("should use whole steps for larger counts", () => {
			expect(getIntegerTicks(12)).toEqual([0, 3, 6, 9, 12]);
			expect(getIntegerTicks(11)).toEqual([0, 3, 6, 9, 12]);
		});
	});
});
