import { describe, expect, it, vi } from "vitest";
import type { Shame, ShameStatistics } from "@/models";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { render, screen } from "@/test/test-utils";
import { GameNightNudge } from "./GameNightNudge";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const night = { id: 4, title: "Friday", startDate: new Date(2026, 9, 23, 19) };

const statistics: ShameStatistics = {
	count: 3,
	totalValue: null,
	averageValue: null,
	pricedGameCount: 0,
	months: 6,
	ownedCount: 41,
	collectionValue: 0,
	neverPlayedCount: 3,
	neverPlayedValue: 0,
	plannedCount: 0,
	nextGameNight: night,
};

const root: Shame = {
	id: 1,
	title: "Root",
	image: null,
	additionDate: null,
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

describe("GameNightNudge", () => {
	it("names the next night and asks to pick a game when nothing is planned", () => {
		render(<GameNightNudge statistics={statistics} shames={[root]} dateFormat="dd-MM-yyyy" canPlan />);

		expect(screen.getByText("nudge.next")).toBeInTheDocument();
		expect(screen.getByText(/nudge\.pick/)).toBeInTheDocument();
	});

	it("does not ask readers to pick a game", () => {
		render(<GameNightNudge statistics={statistics} shames={[root]} dateFormat="dd-MM-yyyy" canPlan={false} />);

		expect(screen.getByText("nudge.next")).toBeInTheDocument();
		expect(screen.queryByText(/nudge\.pick/)).not.toBeInTheDocument();
	});

	it("counts the games coming off the shelf for one night", () => {
		const planned = { ...root, plannedGameNightId: 4, plannedFor: night.startDate };
		render(
			<GameNightNudge
				statistics={{ ...statistics, plannedCount: 2 }}
				shames={[planned, { ...planned, id: 2 }]}
				dateFormat="dd-MM-yyyy"
				canPlan
			/>,
		);

		expect(screen.getByText(/nudge\.planned$|nudge\.planned /)).toBeInTheDocument();
	});

	it("counts the nights when the planned games are spread over several", () => {
		const planned = { ...root, plannedGameNightId: 4, plannedFor: night.startDate };
		render(
			<GameNightNudge
				statistics={{ ...statistics, plannedCount: 2 }}
				shames={[planned, { ...planned, id: 2, plannedGameNightId: 5 }]}
				dateFormat="dd-MM-yyyy"
				canPlan
			/>,
		);

		expect(screen.getByText(/nudge\.planned-across/)).toBeInTheDocument();
	});

	it("links to the game nights when none is coming up, only for planners", () => {
		const { unmount } = render(
			<GameNightNudge
				statistics={{ ...statistics, nextGameNight: null }}
				shames={[root]}
				dateFormat="dd-MM-yyyy"
				canPlan
			/>,
		);
		expect(screen.getByRole("link", { name: "plan.plan-night" })).toHaveAttribute("href", "/game-nights");
		unmount();

		const { container } = render(
			<GameNightNudge
				statistics={{ ...statistics, nextGameNight: null }}
				shames={[root]}
				dateFormat="dd-MM-yyyy"
				canPlan={false}
			/>,
		);
		expect(container).toBeEmptyDOMElement();
	});
});
