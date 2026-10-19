import { describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen } from "@/test/test-utils";

vi.mock("@/components/BgtCharts/BgtPieChart", () => ({
	BgtPieChart: ({ data }: { data: unknown[] }) => <div data-testid="pie">{data.length}</div>,
}));

import { PlayerCountChartCard } from "./PlayerCountChartCard";

describe("PlayerCountChartCard", () => {
	it("hides the donut when every session had the same player count", () => {
		const { container } = renderWithProviders(
			<PlayerCountChartCard playerCountChart={[{ players: 2, playCount: 4 }]} />,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("shows the donut for more than one player count", () => {
		renderWithProviders(
			<PlayerCountChartCard
				playerCountChart={[
					{ players: 2, playCount: 4 },
					{ players: 4, playCount: 1 },
				]}
			/>,
		);

		expect(screen.getByTestId("pie")).toHaveTextContent("2");
	});
});
