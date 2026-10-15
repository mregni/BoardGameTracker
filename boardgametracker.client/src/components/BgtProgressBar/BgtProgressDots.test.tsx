import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { type BgtProgressDot, BgtProgressDots } from "./BgtProgressDots";

const attendance: BgtProgressDot[] = [
	{ key: "1", state: "on", label: "came" },
	{ key: "2", state: "on", label: "came" },
	{ key: "3", state: "off", label: "not there" },
	{ key: "4", state: "bad", label: "cancelled" },
];

const dots = () => screen.getAllByTestId("progress-dot");

describe("BgtProgressDots", () => {
	it("renders one dot per item in order with its state", () => {
		render(<BgtProgressDots items={attendance} />);

		expect(dots().map((dot) => dot.dataset.state)).toEqual(["on", "on", "off", "bad"]);
		expect(dots().map((dot) => dot.getAttribute("title"))).toEqual(["came", "came", "not there", "cancelled"]);
	});

	it("summarises every item in the accessible label", () => {
		render(<BgtProgressDots items={attendance} label="Anna" />);

		expect(screen.getByRole("img", { name: "Anna: came, came, not there, cancelled" })).toBeInTheDocument();
	});

	it("leaves out the prefix without a label", () => {
		render(<BgtProgressDots items={attendance.slice(0, 2)} />);

		expect(screen.getByRole("img", { name: "came, came" })).toBeInTheDocument();
	});

	it("uses a custom label formatter", () => {
		const formatLabel = vi.fn(() => "2 van 4 avonden");
		render(<BgtProgressDots items={attendance} label="Anna" formatLabel={formatLabel} />);

		expect(screen.getByRole("img", { name: "2 van 4 avonden" })).toBeInTheDocument();
		expect(formatLabel).toHaveBeenCalledWith(attendance);
	});

	it("colours on dots with the ok tone by default", () => {
		render(<BgtProgressDots items={attendance} />);

		const [on, , off, bad] = dots();
		expect(on).toHaveClass("bg-ok", "border-transparent");
		expect(off).toHaveClass("bg-card", "border-card-border");
		expect(off).not.toHaveClass("bg-ok");
		expect(bad).toHaveClass("bg-bad/35", "border-bad/60");
	});

	it("colours on dots with the given tone", () => {
		render(<BgtProgressDots items={attendance} tone="value" />);

		expect(dots()[0]).toHaveClass("bg-card-value");
		expect(dots()[2]).not.toHaveClass("bg-card-value");
	});

	it("applies the size variant and className", () => {
		const { rerender } = render(<BgtProgressDots items={attendance} className="mt-1" />);
		expect(dots()[0]).toHaveClass("size-2.5");
		expect(screen.getByRole("img")).toHaveClass("mt-1", "flex-wrap");

		rerender(<BgtProgressDots items={attendance} size="sm" />);
		expect(dots()[0]).toHaveClass("size-2");
	});

	it("renders items without keys", () => {
		render(
			<BgtProgressDots
				items={[
					{ state: "on", label: "won" },
					{ state: "off", label: "lost" },
				]}
			/>,
		);

		expect(dots()).toHaveLength(2);
		expect(screen.getByRole("img", { name: "won, lost" })).toBeInTheDocument();
	});

	it("renders an empty strip without items", () => {
		render(<BgtProgressDots items={[]} label="Anna" />);

		expect(screen.getByRole("img", { name: "Anna:" })).toBeInTheDocument();
		expect(screen.queryAllByTestId("progress-dot")).toHaveLength(0);
	});
});
