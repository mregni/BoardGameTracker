import { describe, expect, it } from "vitest";
import {
	clampValue,
	computeSegments,
	createDefaultFormatLabel,
	defaultFormatValue,
	sanitize,
	toPercent,
} from "./progressMath";

describe("sanitize", () => {
	it.each([
		[5, 5],
		[0, 0],
		[-3, 0],
		[Number.NaN, 0],
		[Number.POSITIVE_INFINITY, 0],
		[Number.NEGATIVE_INFINITY, 0],
		[0.25, 0.25],
	])("turns %s into %s", (input, expected) => {
		expect(sanitize(input)).toBe(expected);
	});
});

describe("clampValue", () => {
	it("keeps a value inside the range", () => {
		expect(clampValue(3, 10)).toBe(3);
	});

	it("caps a value above max", () => {
		expect(clampValue(12, 10)).toBe(10);
	});

	it("raises a negative value to zero", () => {
		expect(clampValue(-4, 10)).toBe(0);
	});

	it("returns zero when max is zero or invalid", () => {
		expect(clampValue(4, 0)).toBe(0);
		expect(clampValue(4, -1)).toBe(0);
		expect(clampValue(4, Number.NaN)).toBe(0);
	});
});

describe("toPercent", () => {
	it.each([
		[38, 100, 38],
		[1, 3, 33.33],
		[2, 3, 66.67],
		[1, 8, 12.5],
		[5, 5, 100],
		[0, 5, 0],
		[7, 5, 100],
		[-2, 5, 0],
		[3, 0, 0],
		[3, -5, 0],
		[Number.NaN, 5, 0],
	])("value %s of max %s is %s%%", (value, max, expected) => {
		expect(toPercent(value, max)).toBe(expected);
	});
});

describe("computeSegments", () => {
	const segment = (label: string, value: number) => ({ label, value });

	it("uses the total as max when no max is given", () => {
		const result = computeSegments([segment("a", 1), segment("b", 3)]);

		expect(result.max).toBe(4);
		expect(result.total).toBe(4);
		expect(result.overflow).toBe(false);
		expect(result.rest).toBe(0);
		expect(result.items.map((item) => item.percent)).toEqual([25, 75]);
	});

	it("leaves room for the rest when the total is below max", () => {
		const result = computeSegments([segment("a", 45), segment("b", 90)], 240);

		expect(result.items.map((item) => item.percent)).toEqual([18.75, 37.5]);
		expect(result.rest).toBe(43.75);
		expect(result.overflow).toBe(false);
	});

	it("keeps zero, negative and invalid segments as hidden zero-width items", () => {
		const result = computeSegments([
			segment("a", 0),
			segment("b", 2),
			segment("c", -1),
			segment("d", Number.NaN),
			segment("e", 2),
		]);

		expect(result.items.map((item) => item.segment.label)).toEqual(["a", "b", "c", "d", "e"]);
		expect(result.items.map((item) => item.visible)).toEqual([false, true, false, false, true]);
		expect(result.items.map((item) => item.percent)).toEqual([0, 50, 0, 0, 50]);
		expect(result.items.map((item) => item.value)).toEqual([0, 2, 0, 0, 2]);
		expect(result.total).toBe(4);
	});

	it("caps segments at max and flags the overflow", () => {
		const result = computeSegments([segment("a", 60), segment("b", 30), segment("c", 30)], 100);

		expect(result.overflow).toBe(true);
		expect(result.total).toBe(120);
		expect(result.items.map((item) => item.shown)).toEqual([60, 30, 10]);
		expect(result.items.map((item) => item.value)).toEqual([60, 30, 30]);
		expect(result.items.map((item) => item.percent)).toEqual([60, 30, 10]);
		expect(result.rest).toBe(0);
	});

	it("hides segments that start after max", () => {
		const result = computeSegments([segment("a", 100), segment("b", 20)], 100);

		expect(result.items.map((item) => item.visible)).toEqual([true, false]);
		expect(result.items.map((item) => item.shown)).toEqual([100, 0]);
		expect(result.items.map((item) => item.value)).toEqual([100, 20]);
		expect(result.overflow).toBe(true);
	});

	it("falls back to the total when max is zero or negative", () => {
		expect(computeSegments([segment("a", 2)], 0).max).toBe(2);
		expect(computeSegments([segment("a", 2)], -10).overflow).toBe(false);
	});

	it("returns only hidden items when everything is zero", () => {
		const result = computeSegments([segment("a", 0)]);

		expect(result.items.map((item) => [item.visible, item.percent])).toEqual([[false, 0]]);
		expect(result.max).toBe(0);
		expect(result.rest).toBe(0);
	});

	it("returns a full rest when there is a max but nothing to show", () => {
		const result = computeSegments([segment("a", 0)], 10);

		expect(result.items.map((item) => item.visible)).toEqual([false]);
		expect(result.rest).toBe(100);
	});

	it("rounds percentages to two decimals", () => {
		const result = computeSegments([segment("a", 1), segment("b", 1), segment("c", 1)]);

		expect(result.items.map((item) => item.percent)).toEqual([33.33, 33.33, 33.33]);
	});
});

describe("defaultFormatValue", () => {
	it("rounds to two decimals", () => {
		expect(defaultFormatValue(3)).toBe("3");
		expect(defaultFormatValue(1.23456)).toBe("1.23");
	});
});

describe("createDefaultFormatLabel", () => {
	it("lists every non-zero segment and the max", () => {
		const format = createDefaultFormatLabel();

		expect(
			format({
				segments: [
					{ label: "Coming", value: 3 },
					{ label: "Maybe", value: 1 },
					{ label: "Unknown", value: 0 },
					{ label: "Declined", value: 1 },
				],
				total: 5,
				max: 5,
				overflow: false,
			}),
		).toBe("Coming 3, Maybe 1, Declined 1 of 5");
	});

	it("returns an empty string when there is nothing to describe", () => {
		expect(createDefaultFormatLabel()({ segments: [], total: 0, max: 0, overflow: false })).toBe("");
	});

	it("uses the given value formatter", () => {
		const format = createDefaultFormatLabel((value) => `€${value}`);

		expect(format({ segments: [{ label: "Never played", value: 200 }], total: 200, max: 1240, overflow: false })).toBe(
			"Never played €200 of €1240",
		);
	});
});
