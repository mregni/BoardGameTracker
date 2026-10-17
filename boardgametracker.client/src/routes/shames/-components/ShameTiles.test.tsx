import { describe, expect, it } from "vitest";
import type { Shame, ShameStatistics } from "@/models";
import { render, screen } from "@/test/test-utils";
import { ShameTiles } from "./ShameTiles";

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
	nextGameNight: { id: 4, title: "Friday", startDate: new Date(2026, 9, 23, 19) },
};

const root: Shame = {
	id: 1,
	title: "Root",
	image: null,
	additionDate: new Date(2023, 7, 1),
	price: null,
	lastSessionDate: null,
	playCount: 0,
	minPlayers: null,
	maxPlayers: null,
	minPlayTime: null,
	maxPlayTime: null,
	plannedGameNightId: null,
	plannedFor: null,
};

describe("ShameTiles", () => {
	it("shows the four tiles with their captions", () => {
		render(<ShameTiles statistics={statistics} shames={[root]} currency="€" dateFormat="dd-MM-yyyy" />);

		expect(screen.getByText("tiles.on-shelf")).toBeInTheDocument();
		expect(screen.getByText("9")).toBeInTheDocument();
		expect(screen.getByText("tiles.of-owned")).toBeInTheDocument();
		expect(screen.getByText("5")).toBeInTheDocument();
		expect(screen.getByText("tiles.oldest-since")).toBeInTheDocument();
		expect(screen.getByText("€365")).toBeInTheDocument();
		expect(screen.getByText("tiles.priced · tiles.share")).toBeInTheDocument();
		expect(screen.getByText("2")).toBeInTheDocument();
		expect(screen.getByText("tiles.planned-for")).toBeInTheDocument();
	});

	it("explains missing prices, an unknown owned date and no upcoming night", () => {
		render(
			<ShameTiles
				statistics={{ ...statistics, totalValue: null, pricedGameCount: 0, nextGameNight: null }}
				shames={[{ ...root, additionDate: null }]}
				currency="€"
				dateFormat="dd-MM-yyyy"
			/>,
		);

		expect(screen.getByText("–")).toBeInTheDocument();
		expect(screen.getByText("tiles.no-prices")).toBeInTheDocument();
		expect(screen.getByText("tiles.oldest")).toBeInTheDocument();
		expect(screen.getByText("tiles.no-night")).toBeInTheDocument();
	});

	it("leaves out the share when the collection has no value", () => {
		render(
			<ShameTiles
				statistics={{ ...statistics, collectionValue: 0 }}
				shames={[]}
				currency="€"
				dateFormat="dd-MM-yyyy"
			/>,
		);

		expect(screen.getByText("tiles.priced")).toBeInTheDocument();
		expect(screen.queryByText(/tiles\.oldest/)).not.toBeInTheDocument();
	});

	it("names the night when every planned game is planned for the same one", () => {
		const planned = { ...root, plannedGameNightId: 4, plannedFor: new Date(2026, 9, 16, 19) };
		render(
			<ShameTiles
				statistics={statistics}
				shames={[planned, { ...planned, id: 2 }]}
				currency="€"
				dateFormat="dd-MM-yyyy"
			/>,
		);

		expect(screen.getByText("tiles.planned-next")).toBeInTheDocument();
		expect(screen.queryByText("tiles.planned-for")).not.toBeInTheDocument();
	});

	it("counts the nights when the planned games are spread over several", () => {
		const planned = { ...root, plannedGameNightId: 4, plannedFor: new Date(2026, 9, 16, 19) };
		render(
			<ShameTiles
				statistics={{ ...statistics, plannedCount: 3 }}
				shames={[
					planned,
					{ ...planned, id: 2, plannedGameNightId: 5, plannedFor: new Date(2026, 9, 23, 19) },
					{ ...planned, id: 3, plannedGameNightId: 6, plannedFor: new Date(2026, 9, 31, 19) },
				]}
				currency="€"
				dateFormat="dd-MM-yyyy"
			/>,
		);

		expect(screen.getByText("tiles.planned-across")).toBeInTheDocument();
	});
});
