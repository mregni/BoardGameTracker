import { describe, expect, it } from "vitest";
import type { Shame, ShameStatistics } from "@/models";
import { render, screen } from "@/test/test-utils";
import { ShameSummary } from "./ShameSummary";

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

describe("ShameSummary", () => {
	it("sums up the shelf in one line", () => {
		render(<ShameSummary statistics={statistics} shames={[root]} />);

		expect(screen.getByText("summary.on-shelf · summary.never-played · summary.oldest-since")).toBeInTheDocument();
	});

	it("names the oldest game without a date when the owned date is unknown", () => {
		render(<ShameSummary statistics={statistics} shames={[{ ...root, additionDate: null }]} />);

		expect(screen.getByText(/summary\.oldest$/)).toBeInTheDocument();
	});

	it("leaves out the never played parts when every game was played before", () => {
		render(<ShameSummary statistics={{ ...statistics, neverPlayedCount: 0 }} shames={[]} />);

		expect(screen.getByText("summary.on-shelf")).toBeInTheDocument();
	});
});
