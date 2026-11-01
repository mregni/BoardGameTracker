import { describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen } from "@/test/test-utils";

vi.mock("@/components/BgtCharts/BgtBarChart", () => ({
	BgtBarChart: ({ data, tickFormat }: { data: { day: string }[]; tickFormat: (value: string) => string }) => (
		<div data-testid="bars">{[...data.map((item) => tickFormat(item.day)), tickFormat("other")].join(",")}</div>
	),
}));

import { SessionCountChartCard } from "./SessionCountChartCard";

describe("SessionCountChartCard", () => {
	it("hides the chart until there are enough sessions", () => {
		const { container } = renderWithProviders(
			<SessionCountChartCard playByDayChart={[{ dayOfWeek: "monday", playCount: 4 }]} />,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("labels the bars with short weekdays", () => {
		renderWithProviders(
			<SessionCountChartCard
				playByDayChart={[
					{ dayOfWeek: "monday", playCount: 3 },
					{ dayOfWeek: "someday", playCount: 2 },
				]}
			/>,
		);

		expect(screen.getByTestId("bars")).toHaveTextContent("Mon,common:days.someday,other");
	});
});
