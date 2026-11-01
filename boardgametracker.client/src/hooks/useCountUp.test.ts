import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useCountUp } from "./useCountUp";

const allowMotion = (allowed: boolean) =>
	vi.spyOn(window, "matchMedia").mockImplementation(
		(query) =>
			({
				matches: allowed,
				media: query,
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
			}) as unknown as MediaQueryList,
	);

describe("useCountUp", () => {
	afterEach(() => {
		vi.useRealTimers();
		vi.restoreAllMocks();
	});

	it("shows the target straight away when motion is reduced", () => {
		allowMotion(false);
		const { result } = renderHook(() => useCountUp(12));

		expect(result.current).toBe(12);
	});

	it("counts from zero up to the target", () => {
		allowMotion(true);
		vi.useFakeTimers({ toFake: ["requestAnimationFrame", "cancelAnimationFrame", "performance"] });
		const { result } = renderHook(() => useCountUp(12));

		expect(result.current).toBe(0);
		act(() => vi.advanceTimersByTime(1000));
		expect(result.current).toBe(12);
	});
});
