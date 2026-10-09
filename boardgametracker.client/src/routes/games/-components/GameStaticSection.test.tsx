import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState } from "@/models";
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
	changeDetectionWatchId: null,
	language: null,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 30,
	maxPlayTime: 60,
	minAge: null,
	rating: null,
	weight: null,
	bggId: null,
	state: GameState.Owned,
	isLoaned: false,
	expansions: [],
	categories: [],
	mechanics: [],
	people: [],
	hasScoring: true,
	buyingPrice: 45,
	soldPrice: null,
	additionDate: null,
	...overrides,
});

describe("GameStaticSection", () => {
	const defaultProps = {
		game: createGame(),
		manualCount: 2,
		ragEnabled: true,
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Description", () => {
		it("should render the game description", () => {
			renderWithTheme(<GameStaticSection {...defaultProps} />);

			expect(screen.getByText("Trade and build settlements")).toBeInTheDocument();
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

	describe("Track price button", () => {
		it("should offer price tracking when allowed", async () => {
			const user = userEvent.setup();
			const onTrackPrice = vi.fn();
			renderWithTheme(<GameStaticSection {...defaultProps} canTrackPrice onTrackPrice={onTrackPrice} />);

			await user.click(screen.getByRole("button", { name: /track-price\.button/ }));

			expect(onTrackPrice).toHaveBeenCalledTimes(1);
		});
	});

	describe("Categories", () => {
		it("should render a badge per category", () => {
			const game = createGame({
				categories: [
					{ id: 1, name: "Strategy" },
					{ id: 2, name: "Family" },
				],
			});
			renderWithTheme(<GameStaticSection {...defaultProps} game={game} />);

			expect(screen.getByText("Strategy")).toBeInTheDocument();
			expect(screen.getByText("Family")).toBeInTheDocument();
		});

		it("should navigate to the games list filtered by category on badge click", async () => {
			const user = userEvent.setup();
			const game = createGame({ categories: [{ id: 1, name: "Strategy" }] });
			renderWithTheme(<GameStaticSection {...defaultProps} game={game} />);

			await user.click(screen.getByText("Strategy"));

			expect(mockNavigate).toHaveBeenCalledTimes(1);
			const call = mockNavigate.mock.calls[0][0];
			expect(call.to).toBe("/games");
			expect(call.search()).toEqual({ category: "Strategy" });
		});
	});
});
