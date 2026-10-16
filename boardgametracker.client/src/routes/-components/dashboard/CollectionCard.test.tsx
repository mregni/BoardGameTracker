import { describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen } from "@/test/test-utils";
import { CollectionCard } from "./CollectionCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const collection = [
	{ type: GameState.Owned, gameCount: 41 },
	{ type: GameState.Wanted, gameCount: 6 },
	{ type: GameState.ForTrade, gameCount: 2 },
];

const renderCard = (overrides: Partial<Parameters<typeof CollectionCard>[0]> = {}) =>
	renderWithTheme(
		<CollectionCard
			collection={collection}
			ownedGames={41}
			totalCollectionValue={1240}
			pricedGameCount={31}
			recentAddedGames={[
				{ id: 5, title: "Age of Inventors", image: null, additionDate: "2026-10-04T10:00:00", price: 40 },
				{ id: 6, title: "Cascadia", image: null, additionDate: null, price: null },
			]}
			currency="EUR"
			uiLanguage="en-US"
			{...overrides}
		/>,
	);

describe("CollectionCard", () => {
	it("links to the games page", () => {
		renderCard();

		expect(screen.getByRole("link", { name: "collection.games" })).toHaveAttribute("href", "/games");
	});

	it("draws the state bar with a legend of the states in use", () => {
		renderCard();

		expect(screen.getByRole("img", { name: "collection.summary" })).toBeInTheDocument();
		expect(screen.getByText("game:state.owned")).toBeInTheDocument();
		expect(screen.getByText("game:state.wanted")).toBeInTheDocument();
		expect(screen.getByText("game:state.for-trade")).toBeInTheDocument();
		expect(screen.queryByText("game:state.previously-owned")).not.toBeInTheDocument();
	});

	it("shows what was paid for the owned games and how many are priced", () => {
		renderCard();

		expect(screen.getByText("collection.paid")).toBeInTheDocument();
		expect(screen.getByText(/1,240/)).toBeInTheDocument();
		expect(screen.getByText(/collection.priced/)).toBeInTheDocument();
	});

	it("says there are no prices yet without a collection value", () => {
		renderCard({ totalCollectionValue: null });

		expect(screen.getByText("collection.no-prices")).toBeInTheDocument();
	});

	it("lists the recently added games with their dates", () => {
		renderCard();

		const first = screen.getByRole("link", { name: /Age of Inventors/ });
		expect(first).toHaveAttribute("href", "/games/5");
		expect(first).toHaveTextContent("4 Oct");
		expect(screen.getByRole("link", { name: /Cascadia/ })).toHaveAttribute("href", "/games/6");
	});

	it("leaves out the bar and the recent games when the collection is empty", () => {
		renderCard({ collection: [], recentAddedGames: [] });

		expect(screen.queryByRole("img", { name: "collection.summary" })).not.toBeInTheDocument();
		expect(screen.queryByText("collection.recently-added")).not.toBeInTheDocument();
	});
});
