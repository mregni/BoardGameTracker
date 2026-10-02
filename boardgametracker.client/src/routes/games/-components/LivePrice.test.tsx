import { describe, expect, it } from "vitest";
import { type Game, GameState } from "@/models";
import type { GamePrice } from "@/models/Games/GamePrice";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LivePrice, withStateChange } from "./LivePrice";

const price = (overrides: Partial<GamePrice> = {}): GamePrice => ({
	gameId: 1,
	watchId: "watch",
	available: true,
	status: "ok",
	inStock: true,
	price: 34.5,
	currency: "EUR",
	checkedAt: null,
	shopUrl: null,
	recheckQueued: false,
	fetchedAt: null,
	...overrides,
});

const game = { id: 1, title: "Brass", state: GameState.Wanted, buyingPrice: null } as unknown as Game;

describe("LivePrice", () => {
	it("shows the formatted live price", () => {
		renderWithTheme(<LivePrice livePrice={price()} uiLanguage="en-US" />);

		expect(screen.getByText(/34\.50/)).toBeInTheDocument();
	});

	it("flags an unreachable watch and shows a dash when there is no price", () => {
		const { unmount } = renderWithTheme(<LivePrice livePrice={price({ available: false, status: "unreachable" })} />);
		expect(screen.getByText("!")).toBeInTheDocument();
		unmount();

		renderWithTheme(<LivePrice />);
		expect(screen.getByText("-")).toBeInTheDocument();
	});
});

describe("withStateChange", () => {
	it("fills in the buying price from the live price when a wanted game becomes owned", () => {
		expect(withStateChange(game, GameState.Owned, price())).toMatchObject({
			state: GameState.Owned,
			buyingPrice: 34.5,
		});
	});

	it("keeps an existing buying price and only prefills for owned", () => {
		expect(withStateChange({ ...game, buyingPrice: 20 } as Game, GameState.Owned, price()).buyingPrice).toBe(20);
		expect(withStateChange(game, GameState.Wanted, price()).buyingPrice).toBeNull();
	});
});
