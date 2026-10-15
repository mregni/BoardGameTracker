import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useGoBack } from "./useGoBack";

const mocks = vi.hoisted(() => ({
	back: vi.fn(),
	canGoBack: vi.fn(),
	navigate: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: mocks.back, canGoBack: mocks.canGoBack } }),
	useNavigate: () => mocks.navigate,
}));

describe("useGoBack", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("goes back in history when there is a previous page", () => {
		mocks.canGoBack.mockReturnValue(true);
		const { result } = renderHook(() => useGoBack("/games/3"));

		result.current();

		expect(mocks.back).toHaveBeenCalledOnce();
		expect(mocks.navigate).not.toHaveBeenCalled();
	});

	it("opens the fallback page when the page was opened directly", () => {
		mocks.canGoBack.mockReturnValue(false);
		const { result } = renderHook(() => useGoBack("/games/3"));

		result.current();

		expect(mocks.back).not.toHaveBeenCalled();
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/3" });
	});
});
