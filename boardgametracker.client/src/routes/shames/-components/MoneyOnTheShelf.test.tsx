import { describe, expect, it, vi } from "vitest";
import type { Shame, ShameStatistics } from "@/models";
import { render, screen, userEvent } from "@/test/test-utils";
import { MoneyOnTheShelf } from "./MoneyOnTheShelf";

const statistics: ShameStatistics = {
	count: 9,
	totalValue: 365,
	averageValue: 45.63,
	pricedGameCount: 8,
	months: 6,
	ownedCount: 41,
	collectionValue: 1240,
	neverPlayedCount: 5,
	neverPlayedValue: 200,
	plannedCount: 2,
	nextGameNight: null,
};

const shame = (id: number, title: string, price: number | null): Shame => ({
	id,
	title,
	image: null,
	additionDate: null,
	price,
	lastSessionDate: null,
	playCount: 0,
	minPlayers: null,
	maxPlayers: null,
	minPlayTime: null,
	maxPlayTime: null,
	plannedGameNightId: null,
	plannedFor: null,
});

describe("MoneyOnTheShelf", () => {
	it("splits the collection value into never played, not played lately and the rest", () => {
		render(
			<MoneyOnTheShelf
				statistics={statistics}
				shames={[shame(1, "Root", null), shame(2, "Everdell", 52)]}
				currency="€"
				canAddPrices
				onSortByPrice={vi.fn()}
			/>,
		);

		const bar = screen.getByRole("img", { name: "money.label" });
		expect(bar).toBeInTheDocument();
		const segments = screen.getAllByTestId("progress-segment");
		expect(segments.map((segment) => segment.dataset.tone)).toEqual(["bad", "warn", "subtle"]);
		expect(screen.getByText("€200")).toBeInTheDocument();
		expect(screen.getByText("€165")).toBeInTheDocument();
		expect(screen.getByText("€875")).toBeInTheDocument();
		expect(screen.getByText(/money\.unpriced-one/)).toBeInTheDocument();
		expect(screen.getByText("€365")).toBeInTheDocument();
		expect(screen.getByText("money.share")).toBeInTheDocument();
	});

	it("counts the unpriced games when there are several", () => {
		render(
			<MoneyOnTheShelf
				statistics={statistics}
				shames={[shame(1, "Root", null), shame(2, "Azul", null)]}
				currency="€"
				canAddPrices
				onSortByPrice={vi.fn()}
			/>,
		);

		expect(screen.getByText(/money\.unpriced$|money\.unpriced /)).toBeInTheDocument();
	});

	it("sorts the list by price paid", async () => {
		const user = userEvent.setup();
		const onSortByPrice = vi.fn();
		render(
			<MoneyOnTheShelf statistics={statistics} shames={[]} currency="€" canAddPrices onSortByPrice={onSortByPrice} />,
		);

		await user.click(screen.getByRole("button", { name: "money.sort-by-price" }));

		expect(onSortByPrice).toHaveBeenCalled();
	});

	it("stays hidden for readers when no owned game has a price", () => {
		const { container } = render(
			<MoneyOnTheShelf
				statistics={{ ...statistics, collectionValue: 0, totalValue: null }}
				shames={[]}
				currency="€"
				canAddPrices={false}
				onSortByPrice={vi.fn()}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("asks for prices when no game on the shelf has a price", () => {
		render(
			<MoneyOnTheShelf
				statistics={{ ...statistics, totalValue: null, pricedGameCount: 0 }}
				shames={[shame(1, "Root", null)]}
				currency="€"
				canAddPrices
				onSortByPrice={vi.fn()}
			/>,
		);

		expect(screen.getByText("money.none")).toBeInTheDocument();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});
});
