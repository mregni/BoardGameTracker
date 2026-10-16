import { describe, expect, it, vi } from "vitest";
import { render, renderWithTheme, screen } from "@/test/test-utils";
import { BgtPieChart } from "./BgtPieChart";

// i18next is mocked globally in setup.ts

vi.mock("@nivo/pie", () => ({
	ResponsivePie: ({
		data,
		tooltip,
	}: {
		data: Array<{ id: string; label: string; value: number }>;
		tooltip: (props: { datum: { id: string; label: string; value: number } }) => React.ReactNode;
	}) => (
		<div data-testid="responsive-pie">
			{data.map((item, i) => (
				<div key={i} data-testid={`pie-item-${i}`}>
					{item.label}: {item.value}
				</div>
			))}
			<div data-testid="tooltip">{tooltip({ datum: { id: "1", label: "Test", value: 10 } })}</div>
		</div>
	),
}));

vi.mock("@/models/Charts/PieChartDatum", () => ({
	pieColors: ["#ff0000", "#00ff00", "#0000ff"],
}));

describe("BgtPieChart", () => {
	const defaultProps = {
		data: [
			{ id: "1", label: "Category A", value: 30 },
			{ id: "2", label: "Category B", value: 50 },
			{ id: "3", label: "Category C", value: 20 },
		],
		tooltipPrefix: "chart.sessions",
	};

	describe("Rendering", () => {
		it("should render pie chart when data is provided", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByTestId("responsive-pie")).toBeInTheDocument();
		});

		it("should render all data items", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByTestId("pie-item-0")).toBeInTheDocument();
			expect(screen.getByTestId("pie-item-1")).toBeInTheDocument();
			expect(screen.getByTestId("pie-item-2")).toBeInTheDocument();
		});

		it("should display total in center", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByText("100")).toBeInTheDocument(); // 30 + 50 + 20 = 100
		});

		it('should display "Total" label', () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByText("total")).toBeInTheDocument();
		});
	});

	describe("Null/Empty Data", () => {
		it("should return null when data is null", () => {
			const { container } = render(<BgtPieChart {...defaultProps} data={null as unknown as []} />);
			expect(container.firstChild).toBeNull();
		});

		it("should return null when data is empty array", () => {
			const { container } = render(<BgtPieChart {...defaultProps} data={[]} />);
			expect(container.firstChild).toBeNull();
		});
	});

	describe("Legend", () => {
		it("should not show legend by default", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			// Legend items have specific styling with w-3 h-3 divs
			const legendItems = screen.queryAllByText("Category A");
			// Only the pie item should show, not a separate legend item
			expect(legendItems.length).toBeLessThanOrEqual(2);
		});

		it("should show legend when showLegend is true", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} showLegend={true} />);
			// All category labels should appear in legend
			expect(screen.getAllByText("Category A").length).toBeGreaterThanOrEqual(1);
			expect(screen.getAllByText("Category B").length).toBeGreaterThanOrEqual(1);
			expect(screen.getAllByText("Category C").length).toBeGreaterThanOrEqual(1);
		});

		it("should display values in legend", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} showLegend={true} />);
			expect(screen.getByText("30")).toBeInTheDocument();
			expect(screen.getByText("50")).toBeInTheDocument();
			expect(screen.getByText("20")).toBeInTheDocument();
		});
	});

	describe("Tooltip", () => {
		it("should render tooltip content", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByTestId("tooltip")).toBeInTheDocument();
		});

		it("should display label in tooltip", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);
			expect(screen.getByText("Test")).toBeInTheDocument();
		});
	});

	describe("Colours and labels", () => {
		it("colours the legend from the palette by default", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} showLegend />);

			expect(screen.getByTestId("legend-swatch-1")).toHaveStyle({ backgroundColor: "#ff0000" });
			expect(screen.getByTestId("legend-swatch-3")).toHaveStyle({ backgroundColor: "#0000ff" });
		});

		it("uses the colour given with a slice", () => {
			renderWithTheme(
				<BgtPieChart
					data={[
						{ id: "win", label: "Wins", value: 4, color: "#123456" },
						{ id: "loss", label: "Losses", value: 1 },
					]}
					showLegend
					tooltipPrefix=""
				/>,
			);

			expect(screen.getByTestId("legend-swatch-win")).toHaveStyle({ backgroundColor: "#123456" });
			expect(screen.getByTestId("legend-swatch-loss")).toHaveStyle({ backgroundColor: "#00ff00" });
		});

		it("describes every slice and the total for screen readers", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} />);

			expect(
				screen.getByRole("img", { name: "Category A: 30, Category B: 50, Category C: 20, total: 100" }),
			).toBeInTheDocument();
		});

		it("uses a given accessible label", () => {
			renderWithTheme(<BgtPieChart {...defaultProps} ariaLabel="Wins 3, losses 1" />);

			expect(screen.getByRole("img", { name: "Wins 3, losses 1" })).toBeInTheDocument();
		});
	});

	describe("Combined Props", () => {
		it("should handle all props together", () => {
			renderWithTheme(
				<BgtPieChart
					data={[
						{ id: "1", label: "Owned", value: 75 },
						{ id: "2", label: "Wanted", value: 25 },
					]}
					showLegend={true}
					tooltipPrefix="games:count"
				/>,
			);

			expect(screen.getByText("100")).toBeInTheDocument();
			expect(screen.getByText("total")).toBeInTheDocument();
			expect(screen.getAllByText("Owned").length).toBeGreaterThanOrEqual(1);
			expect(screen.getAllByText("Wanted").length).toBeGreaterThanOrEqual(1);
		});
	});
});
