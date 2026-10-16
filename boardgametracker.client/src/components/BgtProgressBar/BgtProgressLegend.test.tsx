import { describe, expect, it } from "vitest";
import { render, screen, within } from "@/test/test-utils";
import { BgtProgressLegend } from "./BgtProgressLegend";

describe("BgtProgressLegend", () => {
	it("renders nothing without items", () => {
		const { container } = render(<BgtProgressLegend items={[]} />);

		expect(container).toBeEmptyDOMElement();
	});

	it("renders a labelled list of swatches, labels and values", () => {
		render(
			<BgtProgressLegend
				aria-label="Attendance"
				className="mt-2"
				items={[
					{ key: "came", label: "came", tone: "ok" },
					{ key: "missed", label: "not there", tone: "empty" },
					{ key: "owned", label: "Owned", value: 41, tone: "value" },
				]}
			/>,
		);

		const list = screen.getByRole("list", { name: "Attendance" });
		expect(list).toHaveClass("mt-2", "flex-wrap");
		const items = within(list).getAllByRole("listitem");
		expect(items.map((item) => item.textContent)).toEqual(["came", "not there", "Owned41"]);
		const swatches = screen.getAllByTestId("progress-legend-swatch");
		expect(swatches[0]).toHaveClass("bg-ok");
		expect(swatches[1]).toHaveClass("bg-card", "border-card-border");
		expect(swatches[2]).toHaveClass("bg-card-value");
	});

	it("defaults swatches to the primary tone and solid pattern", () => {
		render(<BgtProgressLegend items={[{ label: "Wanted" }]} />);

		const swatch = screen.getByTestId("progress-legend-swatch");
		expect(swatch).toHaveClass("bg-primary");
		expect(swatch).toHaveAttribute("data-pattern", "solid");
	});

	it("renders hatched and custom coloured swatches", () => {
		render(
			<BgtProgressLegend
				items={[
					{ label: "Same co-op team", value: "3 (won 2, lost 1)", pattern: "hatch" },
					{ label: "Azul", color: "rgb(165, 13, 34)", tone: "bad" },
				]}
			/>,
		);

		const swatches = screen.getAllByTestId("progress-legend-swatch");
		expect(swatches[0]).toHaveClass("bg-hatch");
		expect(swatches[0]).toHaveAttribute("data-pattern", "hatch");
		expect(swatches[1]).toHaveStyle({ backgroundColor: "rgb(165, 13, 34)" });
		expect(swatches[1]).not.toHaveClass("bg-bad");
		expect(screen.getByText("3 (won 2, lost 1)")).toHaveClass("tabular-nums");
	});

	it("shows a zero value", () => {
		render(<BgtProgressLegend items={[{ label: "For trade", value: 0 }]} />);

		expect(screen.getByRole("listitem")).toHaveTextContent("For trade0");
	});
});
