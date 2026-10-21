import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { useBottomNavStore, useHideBottomNav, useIsBottomNavHidden } from "./useHideBottomNav";

describe("useHideBottomNav", () => {
	beforeEach(() => {
		useBottomNavStore.setState({ hideRequests: 0 });
	});

	it("shows the nav when nothing asks to hide it", () => {
		const { result } = renderHook(() => useIsBottomNavHidden());

		expect(result.current).toBe(false);
	});

	it("hides the nav while mounted and shows it again on unmount", () => {
		const visibility = renderHook(() => useIsBottomNavHidden());
		const hider = renderHook(() => useHideBottomNav());

		expect(visibility.result.current).toBe(true);

		hider.unmount();

		expect(visibility.result.current).toBe(false);
	});

	it("keeps the nav visible when hide is false", () => {
		renderHook(() => useHideBottomNav(false));

		expect(useBottomNavStore.getState().hideRequests).toBe(0);
	});

	it("follows the hide flag when it changes", () => {
		const { rerender } = renderHook(({ hide }) => useHideBottomNav(hide), { initialProps: { hide: true } });
		expect(useBottomNavStore.getState().hideRequests).toBe(1);

		rerender({ hide: false });
		expect(useBottomNavStore.getState().hideRequests).toBe(0);

		rerender({ hide: true });
		expect(useBottomNavStore.getState().hideRequests).toBe(1);
	});

	it("keeps the nav hidden until every requester has unmounted", () => {
		const first = renderHook(() => useHideBottomNav());
		const second = renderHook(() => useHideBottomNav());

		first.unmount();
		expect(useBottomNavStore.getState().hideRequests).toBe(1);

		second.unmount();
		expect(useBottomNavStore.getState().hideRequests).toBe(0);
	});

	it("never counts below zero", () => {
		act(() => useBottomNavStore.getState().releaseHide());

		expect(useBottomNavStore.getState().hideRequests).toBe(0);
	});
});
