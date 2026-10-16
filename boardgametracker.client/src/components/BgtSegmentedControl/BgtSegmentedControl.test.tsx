import { describe, expect, it, vi } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";
import { BgtSegmentedControl } from "./BgtSegmentedControl";

const options = [
	{ value: "nights", label: "Nights" },
	{ value: "stats", label: "Statistics" },
] as const;

describe("BgtSegmentedControl", () => {
	it("marks the selected option as pressed inside a labelled group", () => {
		render(<BgtSegmentedControl options={[...options]} value="stats" onChange={vi.fn()} aria-label="View" />);

		expect(screen.getByRole("group", { name: "View" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Statistics" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "Nights" })).toHaveAttribute("aria-pressed", "false");
	});

	it("reports the option that was picked", async () => {
		const onChange = vi.fn();
		render(<BgtSegmentedControl options={[...options]} value="stats" onChange={onChange} aria-label="View" />);

		await userEvent.click(screen.getByRole("button", { name: "Nights" }));

		expect(onChange).toHaveBeenCalledWith("nights");
	});

	it("applies the small size and the extra class", () => {
		render(
			<BgtSegmentedControl
				options={[...options]}
				value="nights"
				onChange={vi.fn()}
				aria-label="View"
				size="sm"
				className="w-full"
			/>,
		);

		expect(screen.getByRole("group")).toHaveClass("w-full");
		expect(screen.getByRole("button", { name: "Nights" })).toHaveClass("text-xs", "border-primary");
		expect(screen.getByRole("button", { name: "Statistics" })).toHaveClass("border-transparent");
	});
});
