import { renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useTimeouts } from "./useTimeouts";

describe("useTimeouts", () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("runs a scheduled callback after its delay", () => {
		const callback = vi.fn();
		const { result } = renderHook(() => useTimeouts());

		result.current(callback, 20000);
		vi.advanceTimersByTime(20000);

		expect(callback).toHaveBeenCalledTimes(1);
	});

	it("cancels pending callbacks when the component unmounts", () => {
		const callback = vi.fn();
		const { result, unmount } = renderHook(() => useTimeouts());

		result.current(callback, 20000);
		unmount();
		vi.advanceTimersByTime(20000);

		expect(callback).not.toHaveBeenCalled();
	});
});
