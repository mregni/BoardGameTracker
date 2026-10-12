import { describe, expect, it } from "vitest";

import { formatFileSize, GetPercentage, getIntegerTicks, RoundDecimal } from "./numberUtils";

describe("numberUtils", () => {
	describe("RoundDecimal", () => {
		it("should return null for null input", () => {
			expect(RoundDecimal(null)).toBeNull();
		});

		it("should round to nearest integer by default", () => {
			expect(RoundDecimal(4.5)).toBe(5);
			expect(RoundDecimal(4.4)).toBe(4);
			expect(RoundDecimal(4.6)).toBe(5);
		});

		it("should round to specified increment", () => {
			expect(RoundDecimal(7, 5)).toBe(5);
			expect(RoundDecimal(8, 5)).toBe(10);
			expect(RoundDecimal(12, 5)).toBe(10);
			expect(RoundDecimal(13, 5)).toBe(15);
		});

		it("should round to decimal increments", () => {
			expect(RoundDecimal(4.27, 0.5)).toBe(4.5);
			expect(RoundDecimal(4.24, 0.5)).toBe(4);
			expect(RoundDecimal(4.75, 0.5)).toBe(5);
		});

		it("should handle zero", () => {
			expect(RoundDecimal(0)).toBe(0);
			expect(RoundDecimal(0, 5)).toBe(0);
		});

		it("should handle negative numbers", () => {
			expect(RoundDecimal(-4.5)).toBe(-4);
			expect(RoundDecimal(-4.6)).toBe(-5);
			expect(RoundDecimal(-7, 5)).toBe(-5);
		});

		it("should handle large numbers", () => {
			expect(RoundDecimal(1234567.89)).toBe(1234568);
			expect(RoundDecimal(1234567, 100)).toBe(1234600);
		});
	});

	describe("GetPercentage", () => {
		it("should return 0 when total is 0", () => {
			expect(GetPercentage(5, 0)).toBe(0);
			expect(GetPercentage(0, 0)).toBe(0);
		});

		it("should calculate correct percentage", () => {
			expect(GetPercentage(25, 100)).toBe(25);
			expect(GetPercentage(1, 4)).toBe(25);
			expect(GetPercentage(1, 3)).toBe(33);
		});

		it("should round to nearest integer", () => {
			expect(GetPercentage(1, 3)).toBe(33);
			expect(GetPercentage(2, 3)).toBe(67);
		});

		it("should handle 100%", () => {
			expect(GetPercentage(100, 100)).toBe(100);
			expect(GetPercentage(5, 5)).toBe(100);
		});

		it("should handle 0%", () => {
			expect(GetPercentage(0, 100)).toBe(0);
			expect(GetPercentage(0, 5)).toBe(0);
		});

		it("should handle values greater than total", () => {
			expect(GetPercentage(150, 100)).toBe(150);
			expect(GetPercentage(10, 5)).toBe(200);
		});

		it("should handle decimal values", () => {
			expect(GetPercentage(0.5, 1)).toBe(50);
			expect(GetPercentage(0.333, 1)).toBe(33);
		});
	});

	describe("formatFileSize", () => {
		it("should format bytes below a kilobyte", () => {
			expect(formatFileSize(0)).toBe("0 B");
			expect(formatFileSize(512)).toBe("512 B");
			expect(formatFileSize(1023)).toBe("1023 B");
		});

		it("should format kilobytes with one decimal", () => {
			expect(formatFileSize(1024)).toBe("1.0 KB");
			expect(formatFileSize(1536)).toBe("1.5 KB");
			expect(formatFileSize(1048575)).toBe("1024.0 KB");
		});

		it("should format megabytes with one decimal", () => {
			expect(formatFileSize(1048576)).toBe("1.0 MB");
			expect(formatFileSize(5242880)).toBe("5.0 MB");
			expect(formatFileSize(1572864)).toBe("1.5 MB");
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
