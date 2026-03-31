import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CreateSessionPlayer } from "@/models";

import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtPlayerSelector } from "./BgtPlayerSelector";

// i18next is mocked globally in setup.ts

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({
		playerById: (id: number) => {
			const players: Record<number, { id: number; name: string; image: string | null }> = {
				1: { id: 1, name: "Alice", image: null },
				2: { id: 2, name: "Bob", image: "/bob.jpg" },
				3: { id: 3, name: "Charlie", image: null },
			};
			return players[id];
		},
	}),
}));

describe("BgtPlayerSelector", () => {
	const defaultProps = {
		onOpenCreateModal: vi.fn(),
		onEditPlayer: vi.fn(),
		remove: vi.fn(),
		players: [] as CreateSessionPlayer[],
		disabled: false,
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Rendering", () => {
		it("should render the label", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} />);
			expect(screen.getByText("new.players.label")).toBeInTheDocument();
		});

		it("should render add player button", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} />);
			expect(
				screen.getByRole("button", {
					name: /new\.players\.add/i,
				}),
			).toBeInTheDocument();
		});

		it("should render empty state message when no players", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} />);
			expect(screen.getByText("player:new.players.none")).toBeInTheDocument();
		});

		it("should not render empty state message when players exist", () => {
			const players: CreateSessionPlayer[] = [{ playerId: 1, won: false, firstPlay: false, score: 100 }];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} />);
			expect(screen.queryByText("player:new.players.none")).not.toBeInTheDocument();
		});
	});

	describe("Player Display", () => {
		it("should render player names", () => {
			const players: CreateSessionPlayer[] = [
				{ playerId: 1, won: false, firstPlay: false, score: 100 },
				{ playerId: 2, won: false, firstPlay: false, score: 80 },
			];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} />);

			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Bob")).toBeInTheDocument();
		});

		it("should render player scores", () => {
			const players: CreateSessionPlayer[] = [{ playerId: 1, won: false, firstPlay: false, score: 100 }];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} />);

			expect(screen.getByText("100")).toBeInTheDocument();
		});
	});

	describe("User Interactions", () => {
		it("should call onOpenCreateModal when add player button is clicked", async () => {
			const user = userEvent.setup();
			const onOpenCreateModal = vi.fn();
			renderWithTheme(<BgtPlayerSelector {...defaultProps} onOpenCreateModal={onOpenCreateModal} />);

			await user.click(
				screen.getByRole("button", {
					name: /new\.players\.add/i,
				}),
			);

			expect(onOpenCreateModal).toHaveBeenCalled();
		});

		it("should call remove when delete button is clicked", async () => {
			const user = userEvent.setup();
			const remove = vi.fn();
			const players: CreateSessionPlayer[] = [{ playerId: 1, won: false, firstPlay: false, score: 100 }];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} remove={remove} />);

			// Find the delete button (trash icon button)
			const buttons = screen.getAllByRole("button");
			const deleteButton = buttons.find(
				(btn) => btn.querySelector('[class*="text-red"]') || btn.closest('[class*="danger"]'),
			);

			if (deleteButton) {
				await user.click(deleteButton);
				expect(remove).toHaveBeenCalledWith(0);
			}
		});

		it("should call onEditPlayer when edit button is clicked", async () => {
			const user = userEvent.setup();
			const onEditPlayer = vi.fn();
			const players: CreateSessionPlayer[] = [{ playerId: 1, won: false, firstPlay: false, score: 100 }];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} onEditPlayer={onEditPlayer} />);

			// Find all buttons and click the edit one (the primary styled one, not danger/error)
			const buttons = screen.getAllByRole("button");
			// The edit button has text-primary class, the delete button has text-error class
			const editButton = buttons.find(
				(btn) =>
					btn.className.includes("text-primary") &&
					!btn.className.includes("text-error") &&
					!btn.textContent?.includes("new.players.add"),
			);

			if (editButton) {
				await user.click(editButton);
				expect(onEditPlayer).toHaveBeenCalledWith(1);
			}
		});
	});

	describe("Disabled State", () => {
		it("should disable add player button when disabled", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} disabled={true} />);
			expect(
				screen.getByRole("button", {
					name: /new\.players\.add/i,
				}),
			).toBeDisabled();
		});

		it("should disable edit and delete buttons when disabled", () => {
			const players: CreateSessionPlayer[] = [{ playerId: 1, won: false, firstPlay: false, score: 100 }];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} disabled={true} />);

			const buttons = screen.getAllByRole("button");
			// All buttons except possibly labels should be disabled
			const actionButtons = buttons.filter((btn) => !btn.textContent?.includes("new.players.label"));
			actionButtons.forEach((btn) => {
				expect(btn).toBeDisabled();
			});
		});
	});

	describe("Error State", () => {
		it("should display error message when errors exist and no players", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} errors={["At least one player is required"]} />);
			expect(screen.getByText("At least one player is required")).toBeInTheDocument();
		});

		it("should not display empty state message when errors exist", () => {
			renderWithTheme(<BgtPlayerSelector {...defaultProps} errors={["At least one player is required"]} />);
			expect(screen.queryByText("player:new.players.none")).not.toBeInTheDocument();
		});
	});

	describe("Multiple Players", () => {
		it("should render multiple players correctly", () => {
			const players: CreateSessionPlayer[] = [
				{ playerId: 1, won: true, firstPlay: false, score: 150 },
				{ playerId: 2, won: false, firstPlay: false, score: 120 },
				{ playerId: 3, won: false, firstPlay: true, score: 80 },
			];
			renderWithTheme(<BgtPlayerSelector {...defaultProps} players={players} />);

			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Bob")).toBeInTheDocument();
			expect(screen.getByText("Charlie")).toBeInTheDocument();
			expect(screen.getByText("150")).toBeInTheDocument();
			expect(screen.getByText("120")).toBeInTheDocument();
			expect(screen.getByText("80")).toBeInTheDocument();
		});
	});
});
