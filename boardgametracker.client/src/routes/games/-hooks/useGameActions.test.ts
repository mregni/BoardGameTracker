import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useGameActions } from "./useGameActions";

const mockNavigate = vi.fn();
const mockBack = vi.fn();
const mockCanGoBack = vi.fn();

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mockNavigate,
	useRouter: () => ({ history: { back: mockBack, canGoBack: mockCanGoBack } }),
}));

const renderActions = () =>
	renderHook(() =>
		useGameActions({
			gameId: 3,
			deleteGame: vi.fn(),
			deleteExpansion: vi.fn(),
			onDeleteModalClose: vi.fn(),
			onExpansionModalOpen: vi.fn(),
		}),
	);

describe("useGameActions handleBackToGames", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("returns to the previous page when there is one", () => {
		mockCanGoBack.mockReturnValue(true);

		renderActions().result.current.handleBackToGames();

		expect(mockBack).toHaveBeenCalledTimes(1);
		expect(mockNavigate).not.toHaveBeenCalled();
	});

	it("falls back to the games list without history", () => {
		mockCanGoBack.mockReturnValue(false);

		renderActions().result.current.handleBackToGames();

		expect(mockNavigate).toHaveBeenCalledWith({ to: "/games" });
		expect(mockBack).not.toHaveBeenCalled();
	});
});

describe("useGameActions handleDelete", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("leaves the navigation to the delete so it only happens after a successful delete", async () => {
		const deleteGame = vi.fn(async (onDeleted?: () => Promise<unknown>) => {
			await onDeleted?.();
		});
		const onDeleteModalClose = vi.fn();
		const { result } = renderHook(() =>
			useGameActions({
				gameId: 3,
				deleteGame,
				deleteExpansion: vi.fn(),
				onDeleteModalClose,
				onExpansionModalOpen: vi.fn(),
			}),
		);

		await result.current.handleDelete();

		expect(mockNavigate).toHaveBeenCalledWith({ to: "/games" });
		expect(onDeleteModalClose).toHaveBeenCalledTimes(1);
	});

	it("stays on the page when the delete does not succeed", async () => {
		const { result } = renderHook(() =>
			useGameActions({
				gameId: 3,
				deleteGame: vi.fn().mockResolvedValue(undefined),
				deleteExpansion: vi.fn(),
				onDeleteModalClose: vi.fn(),
				onExpansionModalOpen: vi.fn(),
			}),
		);

		await result.current.handleDelete();

		expect(mockNavigate).not.toHaveBeenCalled();
	});
});
