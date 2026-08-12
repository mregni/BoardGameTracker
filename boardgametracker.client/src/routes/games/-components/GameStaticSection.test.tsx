import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, GameType } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameStaticSection } from "./GameStaticSection";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mockNavigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

const createGame = (overrides: Partial<Game> = {}): Game => ({
	id: 3,
	title: "Catan",
	description: "Trade and build settlements",
	yearPublished: 1995,
	image: "catan.jpg",
	shopUrl: null,
	language: null,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 30,
	maxPlayTime: 60,
	minAge: null,
	rating: null,
	weight: null,
	bggId: null,
	type: GameType.Base,
	state: GameState.Owned,
	isLoaned: false,
	baseGameId: null,
	baseGame: null,
	expansions: [],
	categories: [],
	mechanics: [],
	people: [],
	hasScoring: true,
	buyingPrice: 45,
	additionDate: null,
	...overrides,
});

describe("GameStaticSection", () => {
	const defaultProps = {
		game: createGame(),
		playCount: 12,
		currency: "€",
		uiLanguage: "en-US",
		dateFormat: "yyyy-MM-dd",
		manualCount: 2,
		ragEnabled: true,
		onOpenManuals: vi.fn(),
		onOpenExpansions: vi.fn(),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Player count statistic", () => {
		it("should render a range when min and max players are set", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			expect(screen.getByText("players")).toBeInTheDocument();
			expect(screen.getByText("2 - 4")).toBeInTheDocument();
		});

		it("should render a single value when only min players is set", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} game={createGame({ minPlayers: 3, maxPlayers: null })} />);

			expect(screen.getByText("3")).toBeInTheDocument();
		});

		it("should hide the statistic when min and max players are null", () => {
			renderWithTheme(
				<GameStaticSection {...defaultProps} game={createGame({ minPlayers: null, maxPlayers: null })} />,
			);

			expect(screen.queryByText("players")).not.toBeInTheDocument();
		});
	});

	describe("Duration statistic", () => {
		it("should render a range when min and max play time are set", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			expect(screen.getByText("duration")).toBeInTheDocument();
			expect(screen.getByText("30 - 60")).toBeInTheDocument();
		});

		it("should render a single value when only max play time is set", () => {
			renderWithTheme(
				<GameStaticSection {...defaultProps} game={createGame({ minPlayTime: null, maxPlayTime: 90 })} />,
			);

			expect(screen.getByText("90")).toBeInTheDocument();
		});

		it("should hide the statistic when min and max play time are null", () => {
			renderWithTheme(
				<GameStaticSection {...defaultProps} game={createGame({ minPlayTime: null, maxPlayTime: null })} />,
			);

			expect(screen.queryByText("duration")).not.toBeInTheDocument();
		});
	});

	describe("Rulebook chat button", () => {
		it("should render an enabled chat button when rag is enabled and manuals exist", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			expect(screen.getByRole("button", { name: "ask-button" })).not.toBeDisabled();
		});

		it("should render a disabled chat button when there are no manuals", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} manualCount={0} />);

			expect(screen.getByRole("button", { name: "ask-button" })).toBeDisabled();
		});

		it("should not render the chat button when rag is disabled", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} ragEnabled={false} />);

			expect(screen.queryByRole("button", { name: "ask-button" })).not.toBeInTheDocument();
		});
	});

	describe("Categories", () => {
		it("should render a badge per category", () => {
			const game = createGame({
				categories: [
					{ id: "c1", name: "Strategy" },
					{ id: "c2", name: "Family" },
				],
			});
			renderWithTheme(<GameStaticSection {...defaultProps} game={game} />);

			expect(screen.getByText("Strategy")).toBeInTheDocument();
			expect(screen.getByText("Family")).toBeInTheDocument();
		});

		it("should navigate to the games list filtered by category on badge click", async () => {
			const user = userEvent.setup();
			const game = createGame({ categories: [{ id: "c1", name: "Strategy" }] });
			renderWithTheme(<GameStaticSection {...defaultProps} game={game} />);

			await user.click(screen.getByText("Strategy"));

			expect(mockNavigate).toHaveBeenCalledTimes(1);
			const call = mockNavigate.mock.calls[0][0];
			expect(call.to).toBe("/games");
			expect(call.search()).toEqual({ category: "Strategy" });
		});
	});

	describe("In collection statistic", () => {
		it("should render when the game has an addition date", () => {
			renderWithTheme(
				<GameStaticSection {...defaultProps} game={createGame({ additionDate: new Date(2026, 0, 15) })} />,
			);

			expect(screen.getByText("statistics:in-collection")).toBeInTheDocument();
		});

		it("should not render when the addition date is null", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			expect(screen.queryByText("statistics:in-collection")).not.toBeInTheDocument();
		});
	});

	describe("Statistic callbacks", () => {
		it("should call onOpenManuals when the manuals statistic is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			await user.click(screen.getByText("game:manuals.title"));

			expect(defaultProps.onOpenManuals).toHaveBeenCalledTimes(1);
			expect(defaultProps.onOpenExpansions).not.toHaveBeenCalled();
		});

		it("should call onOpenExpansions when the expansions statistic is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			await user.click(screen.getByText("game:expansions.title"));

			expect(defaultProps.onOpenExpansions).toHaveBeenCalledTimes(1);
			expect(defaultProps.onOpenManuals).not.toHaveBeenCalled();
		});
	});
});
