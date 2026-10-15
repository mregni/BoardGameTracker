import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@/test/test-utils";
import { BgtProgressBar, type BgtProgressSegment } from "./BgtProgressBar";

const rsvpSegments: BgtProgressSegment[] = [
	{ key: "coming", label: "Coming", value: 3, tone: "ok" },
	{ key: "maybe", label: "Maybe", value: 1, tone: "warn" },
	{ key: "declined", label: "Declined", value: 1, tone: "bad" },
];

const fill = () => screen.getByTestId("progress-fill");
const segmentsOf = () => screen.queryAllByTestId("progress-segment");

describe("BgtProgressBar single value", () => {
	it("renders a progressbar with its value, range and label", () => {
		render(<BgtProgressBar value={38} aria-label="Win rate" />);

		const bar = screen.getByRole("progressbar", { name: "Win rate" });
		expect(bar).toHaveAttribute("aria-valuenow", "38");
		expect(bar).toHaveAttribute("aria-valuemin", "0");
		expect(bar).toHaveAttribute("aria-valuemax", "100");
		expect(fill()).toHaveStyle({ width: "38%" });
	});

	it("supports aria-labelledby and aria-valuetext", () => {
		render(
			<>
				<span id="label">Achievements</span>
				<BgtProgressBar value={2} max={4} aria-labelledby="label" aria-valuetext="2 of 4" />
			</>,
		);

		const bar = screen.getByRole("progressbar", { name: "Achievements" });
		expect(bar).toHaveAttribute("aria-valuetext", "2 of 4");
		expect(bar).toHaveAttribute("aria-valuemax", "4");
		expect(fill()).toHaveStyle({ width: "50%" });
	});

	it("clamps a value above max", () => {
		render(<BgtProgressBar value={140} max={100} aria-label="bar" />);

		expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "100");
		expect(fill()).toHaveStyle({ width: "100%" });
	});

	it("clamps a negative value to zero without a minimum width", () => {
		render(<BgtProgressBar value={-5} max={10} aria-label="bar" />);

		expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "0");
		expect(fill()).toHaveStyle({ width: "0%" });
		expect(fill()).not.toHaveClass("min-w-1");
	});

	it("handles a max of zero", () => {
		render(<BgtProgressBar value={3} max={0} aria-label="bar" />);

		const bar = screen.getByRole("progressbar");
		expect(bar).toHaveAttribute("aria-valuemax", "0");
		expect(bar).toHaveAttribute("aria-valuenow", "0");
		expect(fill()).toHaveStyle({ width: "0%" });
	});

	it("rounds the width to two decimals and keeps a tiny value visible", () => {
		render(<BgtProgressBar value={1} max={3} aria-label="bar" />);

		expect(fill()).toHaveStyle({ width: "33.33%" });
		expect(fill()).toHaveClass("min-w-1");
	});

	it("uses the value tone by default", () => {
		render(<BgtProgressBar value={1} aria-label="bar" />);

		expect(fill()).toHaveClass("bg-card-value");
	});

	it.each([
		["primary", "bg-primary"],
		["value", "bg-card-value"],
		["ok", "bg-ok"],
		["warn", "bg-warn"],
		["bad", "bg-bad"],
		["muted", "bg-bar-muted"],
		["subtle", "bg-bar-subtle"],
		["playerOne", "bg-player-one"],
		["playerTwo", "bg-player-two"],
		["empty", "bg-card"],
	] as const)("applies the %s tone", (tone, className) => {
		render(<BgtProgressBar value={1} tone={tone} aria-label="bar" />);

		expect(fill()).toHaveClass(className);
	});

	it("uses a custom colour instead of a tone class", () => {
		render(<BgtProgressBar value={1} tone="bad" color="rgb(16, 185, 129)" aria-label="bar" />);

		expect(fill()).toHaveStyle({ backgroundColor: "rgb(16, 185, 129)" });
		expect(fill()).not.toHaveClass("bg-bad");
	});

	it("can be hatched", () => {
		render(<BgtProgressBar value={1} pattern="hatch" aria-label="bar" />);

		expect(fill()).toHaveClass("bg-hatch");
	});

	it.each([
		["xs", "h-1.5"],
		["thin", "h-2"],
		["default", "h-2.5"],
		["lg", "h-3"],
		["xl", "h-3.5"],
	] as const)("applies the %s size", (size, className) => {
		render(<BgtProgressBar value={1} size={size} aria-label="bar" />);

		expect(screen.getByRole("progressbar")).toHaveClass(className);
	});

	it.each([
		["default", "bg-white/10"],
		["primary", "bg-primary/10"],
		["card", "bg-card-light"],
		["none", "bg-transparent"],
	] as const)("applies the %s track", (track, className) => {
		render(<BgtProgressBar value={1} track={track} aria-label="bar" />);

		expect(screen.getByRole("progressbar")).toHaveClass(className);
	});

	it("uses the default size and track", () => {
		render(<BgtProgressBar value={1} aria-label="bar" />);

		expect(screen.getByRole("progressbar")).toHaveClass("h-2.5", "bg-white/10", "overflow-hidden");
	});

	it("only animates the width when motion is allowed", () => {
		render(<BgtProgressBar value={1} aria-label="bar" />);

		expect(fill().className).toContain("motion-safe:transition-[width,flex-grow]");
		expect(fill().className).not.toMatch(/(^|\s)transition/);
	});

	it("passes id, title and className", () => {
		const { container } = render(
			<BgtProgressBar id="bar-id" title="38% won" className="grid-area-bar" value={38} aria-label="bar" />,
		);

		const bar = screen.getByRole("progressbar");
		expect(bar).toHaveAttribute("id", "bar-id");
		expect(bar).toHaveAttribute("title", "38% won");
		expect(container.firstElementChild).toHaveClass("grid-area-bar");
	});

	it("does not render a legend for a single value", () => {
		render(<BgtProgressBar value={1} aria-label="bar" />);

		expect(screen.queryByRole("list")).not.toBeInTheDocument();
	});
});

describe("BgtProgressBar segmented", () => {
	it("renders an image with a generated label for every segment", () => {
		render(<BgtProgressBar segments={rsvpSegments} />);

		expect(screen.getByRole("img", { name: "Coming 3, Maybe 1, Declined 1 of 5" })).toBeInTheDocument();
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("passes every segment to the label formatter so callers can translate it", () => {
		const formatLabel = vi.fn(() => "3 komen, 1 misschien, 1 niet");
		render(
			<BgtProgressBar
				segments={[...rsvpSegments, { key: "none", label: "None", value: 0 }]}
				max={6}
				formatLabel={formatLabel}
			/>,
		);

		expect(screen.getByRole("img", { name: "3 komen, 1 misschien, 1 niet" })).toBeInTheDocument();
		expect(formatLabel).toHaveBeenCalledWith({
			segments: [
				{ label: "Coming", value: 3 },
				{ label: "Maybe", value: 1 },
				{ label: "Declined", value: 1 },
				{ label: "None", value: 0 },
			],
			total: 5,
			max: 6,
			overflow: false,
		});
	});

	it("uses the value formatter in the default label and tooltips", () => {
		render(
			<BgtProgressBar
				max={1240}
				formatValue={(value) => `€${value}`}
				segments={[
					{ key: "never", label: "Never played", value: 200, tone: "bad" },
					{ key: "late", label: "Not lately", value: 165, tone: "warn" },
					{ key: "played", label: "Played", value: 875, tone: "subtle" },
				]}
			/>,
		);

		expect(screen.getByRole("img", { name: "Never played €200, Not lately €165, Played €875 of €1240" })).toBeVisible();
		expect(segmentsOf().map((segment) => segment.getAttribute("title"))).toEqual([
			"Never played · €200",
			"Not lately · €165",
			"Played · €875",
		]);
	});

	it("sizes segments in order by their share of the total", () => {
		render(<BgtProgressBar segments={rsvpSegments} />);

		const segments = segmentsOf();
		expect(segments).toHaveLength(3);
		expect(segments[0]).toHaveStyle({ flexGrow: "60" });
		expect(segments[1]).toHaveStyle({ flexGrow: "20" });
		expect(segments[2]).toHaveStyle({ flexGrow: "20" });
		expect(segments.map((segment) => segment.dataset.tone)).toEqual(["ok", "warn", "bad"]);
		expect(segments[0]).toHaveClass("bg-ok");
		expect(segments[1]).toHaveClass("bg-warn");
		expect(segments[2]).toHaveClass("bg-bad");
	});

	it("rounds segment shares to two decimals", () => {
		render(
			<BgtProgressBar
				segments={[
					{ label: "a", value: 1 },
					{ label: "b", value: 2 },
				]}
			/>,
		);

		const segments = segmentsOf();
		expect(segments[0]).toHaveStyle({ flexGrow: "33.33" });
		expect(segments[1]).toHaveStyle({ flexGrow: "66.67" });
	});

	it("skips segments with a value of zero", () => {
		render(
			<BgtProgressBar
				segments={[
					{ key: "coming", label: "Coming", value: 2, tone: "ok" },
					{ key: "maybe", label: "Maybe", value: 0, tone: "warn" },
					{ key: "declined", label: "Declined", value: -1, tone: "bad" },
				]}
			/>,
		);

		const segments = segmentsOf();
		expect(segments).toHaveLength(1);
		expect(segments[0]).toHaveAttribute("title", "Coming · 2");
		expect(screen.getByRole("img", { name: "Coming 2 of 2" })).toBeInTheDocument();
	});

	it("gives every visible segment a minimum width and a flexible basis", () => {
		render(
			<BgtProgressBar
				segments={[
					{ label: "big", value: 999 },
					{ label: "tiny", value: 1 },
				]}
			/>,
		);

		const tiny = segmentsOf()[1];
		expect(tiny).toHaveStyle({ flexGrow: "0.1" });
		expect(tiny).toHaveClass("min-w-1", "basis-0");
	});

	it("renders an empty labelled bar when every segment is zero", () => {
		render(<BgtProgressBar segments={[{ label: "Coming", value: 0 }]} formatLabel={() => "nobody yet"} />);

		expect(screen.getByRole("img", { name: "nobody yet" })).toBeInTheDocument();
		expect(segmentsOf()).toHaveLength(0);
		expect(screen.queryByTestId("progress-rest")).not.toBeInTheDocument();
	});

	it("fills the remaining space when the total is below max", () => {
		render(
			<BgtProgressBar
				max={240}
				segments={[
					{ label: "Carcassonne", value: 45, color: "rgb(58, 125, 22)" },
					{ label: "Age of Inventors", value: 90, color: "rgb(180, 95, 6)" },
				]}
			/>,
		);

		expect(screen.getByTestId("progress-rest")).toHaveStyle({ flexGrow: "43.75" });
		expect(screen.getByTestId("progress-rest")).toHaveAttribute("aria-hidden", "true");
		expect(screen.queryByTestId("progress-overflow")).not.toBeInTheDocument();
		expect(screen.getByRole("img")).not.toHaveAttribute("data-overflow");
	});

	it("uses custom colours for segments", () => {
		render(<BgtProgressBar segments={[{ label: "Azul", value: 30, color: "rgb(165, 13, 34)", tone: "ok" }]} />);

		const segment = segmentsOf()[0];
		expect(segment).toHaveStyle({ backgroundColor: "rgb(165, 13, 34)" });
		expect(segment).toHaveAttribute("data-tone", "custom");
		expect(segment).not.toHaveClass("bg-ok");
	});

	it("defaults segments to the primary tone", () => {
		render(<BgtProgressBar segments={[{ label: "Owned", value: 41 }]} />);

		expect(segmentsOf()[0]).toHaveClass("bg-primary");
		expect(segmentsOf()[0]).toHaveAttribute("data-tone", "primary");
	});

	it("renders hatched segments", () => {
		render(
			<BgtProgressBar
				segments={[
					{ label: "Mikhaël won", value: 23, tone: "playerOne" },
					{ label: "Same co-op team", value: 3, pattern: "hatch" },
					{ label: "Kathleen won", value: 17, tone: "playerTwo" },
				]}
			/>,
		);

		const segments = segmentsOf();
		expect(segments[1]).toHaveClass("bg-hatch");
		expect(segments[1]).toHaveAttribute("data-pattern", "hatch");
		expect(segments[0]).toHaveAttribute("data-pattern", "solid");
		expect(segments[0]).toHaveClass("bg-player-one");
		expect(segments[2]).toHaveClass("bg-player-two");
	});

	it("uses an explicit segment title as tooltip", () => {
		render(<BgtProgressBar segments={[{ label: "Azul", value: 30, title: "Azul · 30 min" }]} />);

		expect(segmentsOf()[0]).toHaveAttribute("title", "Azul · 30 min");
	});

	it("caps an overflowing total at max and marks the end", () => {
		const formatLabel = vi.fn(() => "Planned 5h of a 4h evening");
		render(
			<BgtProgressBar
				max={240}
				formatLabel={formatLabel}
				segments={[
					{ label: "Brass", value: 180 },
					{ label: "Azul", value: 60 },
					{ label: "Carcassonne", value: 60 },
				]}
			/>,
		);

		const bar = screen.getByRole("img", { name: "Planned 5h of a 4h evening" });
		expect(bar).toHaveAttribute("data-overflow", "true");
		expect(formatLabel).toHaveBeenCalledWith(expect.objectContaining({ total: 300, max: 240, overflow: true }));
		const segments = segmentsOf();
		expect(segments).toHaveLength(2);
		expect(segments[0]).toHaveStyle({ flexGrow: "75" });
		expect(segments[1]).toHaveStyle({ flexGrow: "25" });
		expect(segments[1]).toHaveAttribute("title", "Azul · 60");
		const marker = within(bar).getByTestId("progress-overflow");
		expect(marker).toHaveAttribute("aria-hidden", "true");
		expect(marker).toHaveClass("border-dashed", "border-warn", "bg-overflow-stripes", "right-0");
		expect(screen.queryByTestId("progress-rest")).not.toBeInTheDocument();
	});

	it("adds 2px gaps only when asked", () => {
		const { rerender } = render(<BgtProgressBar segments={rsvpSegments} />);
		expect(screen.getByRole("img")).not.toHaveClass("gap-0.5");

		rerender(<BgtProgressBar segments={rsvpSegments} gapped />);
		expect(screen.getByRole("img")).toHaveClass("gap-0.5");
	});

	it("rounds the outer ends only", () => {
		render(<BgtProgressBar segments={rsvpSegments} size="thin" />);

		expect(screen.getByRole("img")).toHaveClass("rounded", "overflow-hidden");
		for (const segment of segmentsOf()) {
			expect(segment.className).not.toMatch(/rounded/);
		}
	});

	it("applies size and track variants", () => {
		render(<BgtProgressBar segments={rsvpSegments} size="xl" track="none" id="budget" />);

		const bar = screen.getByRole("img");
		expect(bar).toHaveClass("h-3.5", "bg-transparent");
		expect(bar).toHaveAttribute("id", "budget");
	});
});

describe("BgtProgressBar legend", () => {
	it("is hidden by default", () => {
		render(<BgtProgressBar segments={rsvpSegments} />);

		expect(screen.queryByRole("list")).not.toBeInTheDocument();
	});

	it("lists every segment with a swatch, label and formatted value", () => {
		render(
			<BgtProgressBar
				legend
				formatValue={(value) => `€${value}`}
				segments={[
					{ key: "never", label: "Never played", value: 200, tone: "bad" },
					{ key: "late", label: "Not lately", value: 0, tone: "warn" },
					{ key: "team", label: "Same team", value: 3, pattern: "hatch" },
					{ key: "custom", label: "Azul", value: 30, color: "rgb(165, 13, 34)" },
				]}
			/>,
		);

		const items = within(screen.getByRole("list")).getAllByRole("listitem");
		expect(items.map((item) => item.textContent)).toEqual([
			"Never played€200",
			"Not lately€0",
			"Same team€3",
			"Azul€30",
		]);
		const swatches = screen.getAllByTestId("progress-legend-swatch");
		expect(swatches[0]).toHaveClass("bg-bad");
		expect(swatches[1]).toHaveClass("bg-warn");
		expect(swatches[2]).toHaveClass("bg-hatch");
		expect(swatches[2]).toHaveAttribute("data-pattern", "hatch");
		expect(swatches[3]).toHaveStyle({ backgroundColor: "rgb(165, 13, 34)" });
		for (const swatch of swatches) {
			expect(swatch).toHaveAttribute("aria-hidden", "true");
		}
	});

	it("uses the default value formatter", () => {
		render(<BgtProgressBar legend segments={[{ label: "Owned", value: 41.256 }]} />);

		expect(screen.getByRole("listitem")).toHaveTextContent("Owned41.26");
	});
});

describe("BgtProgressBar ticks", () => {
	it("renders scale ticks under the bar in order", () => {
		render(<BgtProgressBar max={240} segments={rsvpSegments} ticks={["19:30", "21:30", "23:30"]} />);

		const ticks = screen.getByTestId("progress-ticks");
		expect(
			within(ticks)
				.getAllByText(/:30$/)
				.map((tick) => tick.textContent),
		).toEqual(["19:30", "21:30", "23:30"]);
		expect(ticks).toHaveClass("justify-between", "tabular-nums");
		expect(screen.getByRole("img").compareDocumentPosition(ticks)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
	});

	it("accepts nodes and numbers as ticks", () => {
		render(<BgtProgressBar value={50} aria-label="bar" ticks={[0, <strong key="mid">mid</strong>, 100]} />);

		const ticks = screen.getByTestId("progress-ticks");
		expect(within(ticks).getByText("mid").tagName).toBe("STRONG");
		expect(ticks).toHaveTextContent("0mid100");
	});

	it("renders nothing for an empty tick list", () => {
		render(<BgtProgressBar value={50} aria-label="bar" ticks={[]} />);

		expect(screen.queryByTestId("progress-ticks")).not.toBeInTheDocument();
	});
});
