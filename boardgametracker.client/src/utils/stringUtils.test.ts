import { describe, expect, it } from "vitest";

import { AvatarRgb, ContrastWithWhite, StringToHsl, StringToRgb } from "./stringUtils";

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

		it("should return valid HSL format", () => {
			const result = StringToHsl("test");
			expect(result).toMatch(/^hsl\(\d+, 85%, \d+%\)$/);
		});

		it("should handle empty string", () => {
			const result = StringToHsl("");
			expect(result).toBe("hsl(0, 85%, 35%)");
		});

		it("should handle special characters", () => {
			const result = StringToHsl("test@#$%");
			expect(result).toMatch(/^hsl\(\d+, 85%, \d+%\)$/);
		});

		it("should handle unicode characters", () => {
			const result = StringToHsl("测试");
			expect(result).toMatch(/^hsl\(\d+, 85%, \d+%\)$/);
		});

		it("should handle very long strings", () => {
			const longString = "a".repeat(1000);
			const result = StringToHsl(longString);
			expect(result).toMatch(/^hsl\(\d+, 85%, \d+%\)$/);
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

		it("should return valid RGB format", () => {
			const result = StringToRgb("test");
			expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
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

		it("should handle empty string", () => {
			const result = StringToRgb("");
			expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
		});

		it("should handle special characters", () => {
			const result = StringToRgb("test@#$%");
			expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
		});
	});
});
