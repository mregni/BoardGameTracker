import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useShortDuration } from "./useShortDuration";

describe("useShortDuration", () => {
	it("shows minutes below an hour", () => {
		const { result } = renderHook(() => useShortDuration());
		expect(result.current(57.4)).toBe("duration.minutes");
	});

	it("shows whole hours without minutes", () => {
		const { result } = renderHook(() => useShortDuration());
		expect(result.current(120)).toBe("duration.hours");
	});

	it("shows hours and minutes", () => {
		const { result } = renderHook(() => useShortDuration());
		expect(result.current(9500)).toBe("duration.hours-minutes");
	});

	it("never shows a negative duration", () => {
		const { result } = renderHook(() => useShortDuration());
		expect(result.current(-5)).toBe("duration.minutes");
	});
});
