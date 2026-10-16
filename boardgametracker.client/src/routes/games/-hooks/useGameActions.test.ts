import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useGameActions } from "./useGameActions";

const mockNavigate = vi.fn();
const mockGoUp = vi.fn();
const mockUseGoUp = vi.fn((_parent: string) => mockGoUp);

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mockNavigate,
}));

vi.mock("@/routes/-hooks/useBack", () => ({
	useBack: (parent: string) => mockUseGoUp(parent),
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

	it("goes up to the games list", () => {
		renderActions().result.current.handleBackToGames();

		expect(mockUseGoUp).toHaveBeenCalledWith("/games");
		expect(mockGoUp).toHaveBeenCalledTimes(1);
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
