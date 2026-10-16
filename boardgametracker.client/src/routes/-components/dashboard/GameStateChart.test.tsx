import { describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { chartColors } from "@/models/Charts/PieChartDatum";
import { renderWithTheme, screen } from "@/test/test-utils";
import { GameStateChartCard } from "./GameStateChart";

const pie = vi.hoisted(() => ({ data: [] as Array<{ id: string; color: string }> }));

vi.mock("@nivo/pie", () => ({
	ResponsivePie: ({ data }: { data: Array<{ id: string; color: string }> }) => {
		pie.data = data;
		return <div data-testid="responsive-pie" />;
	},
}));

const colorOf = (id: string) => pie.data.find((slice) => slice.id === id)?.color;

describe("GameStateChartCard", () => {
	it("gives every game state the colour of its badge", () => {
		renderWithTheme(
			<GameStateChartCard
				data={[
					{ type: GameState.Wanted, gameCount: 2 },
					{ type: GameState.Owned, gameCount: 10 },
					{ type: GameState.PreviouslyOwned, gameCount: 1 },
					{ type: GameState.ForTrade, gameCount: 3 },
				]}
			/>,
		);

		expect(colorOf(GameState.Owned)).toBe(chartColors.win);
		expect(colorOf(GameState.Wanted)).toBe(chartColors.amber);
		expect(colorOf(GameState.PreviouslyOwned)).toBe(chartColors.loss);
		expect(colorOf(GameState.ForTrade)).toBe(chartColors.blue);
	});

	it("describes the collection donut for screen readers", () => {
		renderWithTheme(
			<GameStateChartCard
				data={[
					{ type: GameState.Owned, gameCount: 10 },
					{ type: GameState.Wanted, gameCount: 2 },
				]}
			/>,
		);

		expect(
			screen.getByRole("img", { name: "game:state.wanted: 2, game:state.owned: 10, total: 12" }),
		).toBeInTheDocument();
	});

	it("shows no chart without games", () => {
		renderWithTheme(<GameStateChartCard data={[{ type: GameState.Owned, gameCount: 0 }]} />);

		expect(screen.queryByTestId("responsive-pie")).not.toBeInTheDocument();
	});
});
