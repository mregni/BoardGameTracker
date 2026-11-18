import { describe, expect, it } from "vitest";

import { AvatarRgb, ContrastWithWhite, StringToHsl, StringToRgb, trimTrailingSlashes } from "./stringUtils";

describe("stringUtils", () => {
	describe("StringToHsl", () => {
		it("should return default red hue for undefined input", () => {
			expect(StringToHsl(undefined)).toBe("hsl(0, 85%, 35%)");
		});

		it("should return consistent HSL for the same string", () => {
			const result1 = StringToHsl("test");
			const result2 = StringToHsl("test");
			expect(result1).toBe(result2);
		});

		it("should return different HSL for different strings", () => {
			const result1 = StringToHsl("hello");
			const result2 = StringToHsl("world");
			expect(result1).not.toBe(result2);
		});

		it("should handle empty string", () => {
			const result = StringToHsl("");
			expect(result).toBe("hsl(0, 85%, 35%)");
		});

		it.each([
			["a plain string", "test"],
			["special characters", "test@#$%"],
			["unicode characters", "测试"],
			["very long strings", "a".repeat(1000)],
		])("should return valid HSL format for %s", (_, input) => {
			expect(StringToHsl(input)).toMatch(/^hsl\(\d+, 85%, \d+%\)$/);
		});
	});

	describe("AvatarRgb", () => {
		it("keeps white initials at 4.5:1 or more on every hue", () => {
			for (let hue = 0; hue < 360; hue += 1) {
				expect(ContrastWithWhite(AvatarRgb(hue)), `hue ${hue}`).toBeGreaterThanOrEqual(4.5);
			}
		});

		it("darkens the greens and teals that used to fail", () => {
			expect(ContrastWithWhite([132, 165, 13])).toBeLessThan(4.5);
			expect(ContrastWithWhite(AvatarRgb(73))).toBeGreaterThanOrEqual(4.5);
			expect(ContrastWithWhite(AvatarRgb(108))).toBeGreaterThanOrEqual(4.5);
		});

		it("keeps the original colour where it already passed", () => {
			expect(AvatarRgb(0)).toEqual([165, 13, 13]);
			expect(StringToHsl(undefined)).toBe("hsl(0, 85%, 35%)");
		});

		it("gives the avatar and the fallback image the same colour", () => {
			const [r, g, b] = AvatarRgb(Number(StringToHsl("Pandemic").match(/^hsl\((\d+)/)?.[1]));
			expect(StringToRgb("Pandemic")).toBe(`rgb(${r}, ${g}, ${b})`);
		});
	});

	describe("StringToRgb", () => {
		it("should return black for undefined input", () => {
			expect(StringToRgb(undefined)).toBe("#000000");
		});

		it("should return consistent RGB for the same string", () => {
			const result1 = StringToRgb("test");
			const result2 = StringToRgb("test");
			expect(result1).toBe(result2);
		});

		it("should return different RGB for different strings", () => {
			const result1 = StringToRgb("hello");
			const result2 = StringToRgb("world");
			expect(result1).not.toBe(result2);
		});

		it.each([
			["a plain string", "test"],
			["empty string", ""],
			["special characters", "test@#$%"],
		])("should return valid RGB format for %s", (_, input) => {
			expect(StringToRgb(input)).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
		});

		it("should return RGB values within valid range (0-255)", () => {
			const result = StringToRgb("test");
			const match = result.match(/rgb\((\d+), (\d+), (\d+)\)/);
			expect(match).not.toBeNull();
			if (match) {
				const r = parseInt(match[1], 10);
				const g = parseInt(match[2], 10);
				const b = parseInt(match[3], 10);
				expect(r).toBeGreaterThanOrEqual(0);
				expect(r).toBeLessThanOrEqual(255);
				expect(g).toBeGreaterThanOrEqual(0);
				expect(g).toBeLessThanOrEqual(255);
				expect(b).toBeGreaterThanOrEqual(0);
				expect(b).toBeLessThanOrEqual(255);
			}
		});
	});
});

describe("trimTrailingSlashes", () => {
	it.each([
		["https://id.example.com///", "https://id.example.com"],
		["/games/", "/games"],
		["/", ""],
		["no-slash", "no-slash"],
		["", ""],
	])("trims %j to %j", (input, expected) => {
		expect(trimTrailingSlashes(input)).toBe(expected);
	});
});
