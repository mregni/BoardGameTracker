import { beforeEach, describe, expect, it, vi } from "vitest";
import { BoxOrientation, type Expansion, type Game, GameState, StorageLayout } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameFactsCard } from "./GameFactsCard";

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		children,
		to,
		search,
		className,
	}: {
		children: React.ReactNode;
		to: string;
		search: { at: string };
		className?: string;
	}) => (
		<a href={`${to}?at=${search.at}`} className={className}>
			{children}
		</a>
	),
}));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes: [
			{
				id: 5,
				name: "Kallax",
				locationId: 1,
				parentId: null,
				layout: StorageLayout.Free,
				rows: null,
				columns: null,
				row: null,
				column: null,
			},
		],
		locations: [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 1, lastPlayed: null }],
	}),
}));

const createExpansion = (id: number, overrides: Partial<Expansion> = {}): Expansion => ({
	id,
	title: `Expansion ${id}`,
	bggId: null,
	gameId: 3,
	image: null,
	state: GameState.Owned,
	buyingPrice: null,
	soldPrice: null,
	additionDate: null,
	storedAtLocationId: null,
	storageLocationId: null,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
	...overrides,
});

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
	hasCoopMode: false,
	coopByDefault: false,
	buyingPrice: 45,
	soldPrice: null,
	additionDate: null,
	storedAtLocationId: null,
	storageLocationId: null,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
	playCount: 0,
	lastPlayed: null,
	...overrides,
});

describe("GameFactsCard", () => {
	const defaultProps = {
		game: createGame(),
		currency: "EUR",
		uiLanguage: "en-US",
		dateFormat: "yyyy-MM-dd",
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Groups", () => {
		it("should split facts about the game from facts about your copy", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByRole("heading", { name: "game:facts.about" })).toBeInTheDocument();
			expect(screen.getByRole("heading", { name: "game:facts.your-copy" })).toBeInTheDocument();
		});

		it("should hide your copy when there is nothing to show", () => {
			renderWithTheme(
				<GameFactsCard {...defaultProps} game={createGame({ buyingPrice: null, state: GameState.Wanted })} />,
			);

			expect(screen.queryByRole("heading", { name: "game:facts.your-copy" })).not.toBeInTheDocument();
		});
	});

	describe("Players", () => {
		it("should render a range when min and max players are set", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("players")).toBeInTheDocument();
			expect(screen.getByText("2–4")).toBeInTheDocument();
		});

		it("should render a single value when only min players is set", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayers: 3, maxPlayers: null })} />);

			expect(screen.getByText("3")).toBeInTheDocument();
		});

		it("should collapse equal min and max players to one value", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayers: 4, maxPlayers: 4 })} />);

			expect(screen.getByText("4")).toBeInTheDocument();
			expect(screen.queryByText("4–4")).not.toBeInTheDocument();
		});

		it("should hide the row when min and max players are null", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayers: null, maxPlayers: null })} />);

			expect(screen.queryByText("players")).not.toBeInTheDocument();
		});
	});

	describe("Duration", () => {
		it("should render the range in the short duration format", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("duration")).toBeInTheDocument();
			expect(screen.getByText("short-duration.minutes")).toBeInTheDocument();
			expect(screen.queryByText(/minutes-abbreviation/)).not.toBeInTheDocument();
		});

		it("should collapse an equal duration range to one value", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ minPlayTime: 45, maxPlayTime: 45 })} />);

			expect(screen.getByText("short-duration.minutes")).toBeInTheDocument();
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
			expect(screen.getByText(/45(?![.,]\d)/)).toBeInTheDocument();
		});

		it("should show a dash instead of a zero price", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ buyingPrice: 0 })} />);

			expect(screen.getByText("statistics:buy-price").parentElement?.parentElement).toHaveTextContent("–");
			expect(screen.queryByText(/0[.,]00/)).not.toBeInTheDocument();
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

		it("should flag why the current price is unavailable instead of a bare dash", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ changeDetectionWatchId: "e0808154-28da-4b85-9a71-24a409e694f1" })}
					price={{ gameId: 3, available: false, status: "unauthorized", price: null } as never}
				/>,
			);

			expect(screen.getByRole("img", { name: "game:price.unavailable.unauthorized" })).toHaveTextContent("!");
		});

		it("should say the price is pending while the first check has not run", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ changeDetectionWatchId: "e0808154-28da-4b85-9a71-24a409e694f1" })}
					price={{ gameId: 3, available: false, status: "pending", price: null } as never}
				/>,
			);

			expect(screen.getByTitle("games:tracked-pending")).toHaveTextContent("-");
		});

		it("should not show a current price for a game without a price watch", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.queryByText("game:current-price.title")).not.toBeInTheDocument();
		});

		it("should say in words whether the tracked game is in stock", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ changeDetectionWatchId: "e0808154-28da-4b85-9a71-24a409e694f1" })}
					price={{ gameId: 3, available: true, price: 39.5, currency: "EUR", inStock: false } as never}
				/>,
			);

			expect(screen.getByText("games:in-stock.no")).toBeInTheDocument();
		});

		it("should offer price tracking under your copy when allowed", async () => {
			const user = userEvent.setup();
			const onTrackPrice = vi.fn();
			renderWithTheme(<GameFactsCard {...defaultProps} onTrackPrice={onTrackPrice} />);

			await user.click(screen.getByRole("button", { name: /game:track-price\.button/ }));

			expect(onTrackPrice).toHaveBeenCalledTimes(1);
		});
	});

	describe("In collection", () => {
		it("should render with the date it was added", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ additionDate: new Date(2026, 0, 15) })} />);

			expect(screen.getByText("statistics:in-collection")).toBeInTheDocument();
			expect(screen.getByText("2026-01-15")).toBeInTheDocument();
		});

		it("should label the date as added for a game that is not in the collection", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ state: GameState.Wanted, additionDate: new Date(2026, 0, 15) })}
				/>,
			);

			expect(screen.getByText("statistics:added")).toBeInTheDocument();
			expect(screen.queryByText("statistics:in-collection")).not.toBeInTheDocument();
		});

		it("should not render when the addition date is null", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.queryByText("statistics:in-collection")).not.toBeInTheDocument();
		});
	});

	describe("Storage", () => {
		it("should link to the place the game is stored at", () => {
			renderWithTheme(
				<GameFactsCard {...defaultProps} game={createGame({ storedAtLocationId: 1, storageLocationId: 5 })} />,
			);

			expect(screen.getByText("storage:title")).toBeInTheDocument();
			expect(screen.getByRole("link", { name: "Home › Kallax" })).toHaveAttribute("href", "/storage?at=1:5");
		});

		it("should say an owned game without a place has no storage location", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} />);

			expect(screen.getByText("storage:place.none")).toBeInTheDocument();
			expect(screen.queryByRole("link")).not.toBeInTheDocument();
		});

		it("should leave the row out for a game that cannot be stored", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ state: GameState.Wanted })} />);

			expect(screen.queryByText("storage:title")).not.toBeInTheDocument();
		});

		it("should mark a loaned game next to its place", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ storedAtLocationId: 1, isLoaned: true })} />);

			expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
			expect(screen.getByText("game:state.on-loan")).toBeInTheDocument();
		});

		it("should count the expansions stored outside the base game's box", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({
						expansions: [createExpansion(1, { storedAtLocationId: 1 }), createExpansion(2)],
					})}
				/>,
			);

			expect(screen.getByText("· game:storage.expansions-elsewhere")).toBeInTheDocument();
		});

		it("should not count anything when every expansion is in the box", () => {
			renderWithTheme(<GameFactsCard {...defaultProps} game={createGame({ expansions: [createExpansion(1)] })} />);

			expect(screen.queryByText(/expansions-elsewhere/)).not.toBeInTheDocument();
		});
	});

	describe("Game details", () => {
		it("should render year, minimum age, rating and weight when they are set", () => {
			renderWithTheme(
				<GameFactsCard {...defaultProps} game={createGame({ minAge: 10, rating: 7.46, weight: 2.31 })} />,
			);

			expect(screen.getByText("game:facts.year")).toBeInTheDocument();
			expect(screen.getByText("1995")).toBeInTheDocument();
			expect(screen.getByText("10+")).toBeInTheDocument();
			expect(screen.getByText("7.5 / 10")).toBeInTheDocument();
			expect(screen.getByText("2.3 / 5")).toBeInTheDocument();
		});

		it("should leave out the rows that have no value", () => {
			renderWithTheme(
				<GameFactsCard
					{...defaultProps}
					game={createGame({ yearPublished: null, minAge: 0, rating: null, weight: 0 })}
				/>,
			);

			expect(screen.queryByText("game:facts.year")).not.toBeInTheDocument();
			expect(screen.queryByText("game:facts.min-age")).not.toBeInTheDocument();
			expect(screen.queryByText("rating")).not.toBeInTheDocument();
			expect(screen.queryByText("weight")).not.toBeInTheDocument();
		});
	});
});
