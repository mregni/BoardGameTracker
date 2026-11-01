import { describe, expect, it, vi } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";
import { DEFAULT_SHAME_FILTERS } from "../-utils/shameFilters";
import { ShameFilters } from "./ShameFilters";

vi.mock("@/components/BgtForm/BgtSelect", () => ({
	BgtSelect: ({
		label,
		value,
		items,
		onValueChange,
	}: {
		label: string;
		value: string | number;
		items: { value: string | number; label: string }[];
		onValueChange: (value: string | number) => void;
	}) => (
		<label>
			{label}
			<select
				value={String(value)}
				onChange={(event) => {
					const item = items.find((option) => String(option.value) === event.target.value);
					if (item) onValueChange(item.value);
				}}
			>
				{items.map((item) => (
					<option key={item.value} value={String(item.value)}>
						{item.label}
					</option>
				))}
			</select>
		</label>
	),
}));

const renderFilters = () => {
	const onFiltersChange = vi.fn();
	render(
		<ShameFilters
			filters={DEFAULT_SHAME_FILTERS}
			counts={{ all: 9, never: 5, played: 4 }}
			onFiltersChange={onFiltersChange}
		/>,
	);
	return { onFiltersChange };
};

describe("ShameFilters", () => {
	it("shows the counts on the segments and marks the active one", () => {
		renderFilters();

		expect(screen.getByRole("button", { name: "filters.all 9" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "filters.never 5" })).toHaveAttribute("aria-pressed", "false");
		expect(screen.getByRole("button", { name: "filters.played 4" })).toHaveAttribute("aria-pressed", "false");
	});

	it("switches to never played", async () => {
		const user = userEvent.setup();
		const { onFiltersChange } = renderFilters();

		await user.click(screen.getByRole("button", { name: "filters.never 5" }));

		expect(onFiltersChange).toHaveBeenCalledWith({ ...DEFAULT_SHAME_FILTERS, show: "never" });
	});

	it("changes the owned period and the sort", async () => {
		const user = userEvent.setup();
		const { onFiltersChange } = renderFilters();

		await user.selectOptions(screen.getByRole("combobox", { name: "filters.owned" }), "2y");
		await user.selectOptions(screen.getByRole("combobox", { name: "filters.sort" }), "price");

		expect(onFiltersChange).toHaveBeenCalledWith({ ...DEFAULT_SHAME_FILTERS, owned: "2y" });
		expect(onFiltersChange).toHaveBeenCalledWith({ ...DEFAULT_SHAME_FILTERS, sort: "price" });
	});
});
