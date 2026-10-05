import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameFactsCard } from "./GameFactsCard";

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

describe("GameFactsCard", () => {
	const defaultProps = {
		game: createGame(),
		currency: "EUR",
		uiLanguage: "en-US",
		dateFormat: "yyyy-MM-dd",
		manualCount: 2,
		onOpenManuals: vi.fn(),
		onOpenExpansions: vi.fn(),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Players", () => {
		it("should render a range when min and max players are set", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("players")).toBeInTheDocument();
			expect(screen.getByText("2 - 4")).toBeInTheDocument();
		});

		it("should render a single value when only min players is set", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayers: 3, maxPlayers: null })} />);

			expect(screen.getByText("3")).toBeInTheDocument();
		});

		it("should hide the row when min and max players are null", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayers: null, maxPlayers: null })} />);

			expect(screen.queryByText("players")).not.toBeInTheDocument();
		});
	});

	describe("Duration", () => {
		it("should render a range with the minutes unit", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("duration")).toBeInTheDocument();
			expect(screen.getByText(/30 - 60/)).toHaveTextContent("30 - 60 minutes-abbreviation");
		});

		it("should hide the row when min and max play time are null", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayTime: null, maxPlayTime: null })} />);

			expect(screen.queryByText("duration")).not.toBeInTheDocument();
		});
	});

	describe("Prices", () => {
		it("should format the price paid like the current price", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("statistics:buy-price")).toBeInTheDocument();
			expect(screen.getByText(/45\.00/)).toBeInTheDocument();
		});

		it("should hide the price paid when it is unknown", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ buyingPrice: null })} />);

			expect(screen.queryByText("statistics:buy-price")).not.toBeInTheDocument();
		});

		it("should show the current price and refresh it for a tracked game", async () => {
			const user = userEvent.setup();
			const onRefreshPrice = vi.fn();
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ changeDetectionWatchId: "e0808154-28da-4b85-9a71-24a409e694f1" })}
					price={{ gameId: 3, available: true, price: 39.5, currency: "EUR" } as never}
					onRefreshPrice={onRefreshPrice}
				/>,
			);

			expect(screen.getByText("game:current-price.title")).toBeInTheDocument();
			expect(screen.getByText(/39\.50/)).toBeInTheDocument();
			await user.click(screen.getByRole("button", { name: "price.refresh" }));
			expect(onRefreshPrice).toHaveBeenCalledTimes(1);
		});

		it("should not show a current price for a game without a price watch", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.queryByText("game:current-price.title")).not.toBeInTheDocument();
		});
	});

	describe("In collection", () => {
		it("should render with the date it was added", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ additionDate: new Date(2026, 0, 15) })} />);

			expect(screen.getByText("statistics:in-collection")).toBeInTheDocument();
			expect(screen.getByText("since")).toBeInTheDocument();
		});

		it("should not render when the addition date is null", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.queryByText("statistics:in-collection")).not.toBeInTheDocument();
		});
	});

	describe("Manuals and expansions", () => {
		it("should call onOpenManuals when the manuals row is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			await user.click(screen.getByRole("button", { name: /game:manuals\.title/ }));

			expect(defaultProps.onOpenManuals).toHaveBeenCalledTimes(1);
			expect(defaultProps.onOpenExpansions).not.toHaveBeenCalled();
		});

		it("should call onOpenExpansions when the expansions row is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			await user.click(screen.getByRole("button", { name: /game:expansions\.title/ }));

			expect(defaultProps.onOpenExpansions).toHaveBeenCalledTimes(1);
			expect(defaultProps.onOpenManuals).not.toHaveBeenCalled();
		});
	});
});
