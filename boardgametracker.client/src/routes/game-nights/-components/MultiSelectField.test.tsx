import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { BgtDialog, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog/BgtDialog";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { MultiSelectField } from "./MultiSelectField";

const options = [
	{ value: 1, label: "Alice" },
	{ value: 2, label: "Bob" },
];

const Harness = ({ onDialogClose }: { onDialogClose: () => void }) => {
	const [selected, setSelected] = useState<number[]>([]);
	return (
		<BgtDialog open onClose={onDialogClose}>
			<BgtDialogContent>
				<BgtDialogTitle>Dialog</BgtDialogTitle>
				<MultiSelectField
					label="Players"
					placeholder="Pick players"
					options={options}
					selected={selected}
					disabled={false}
					onChange={setSelected}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};

describe("MultiSelectField", () => {
	it("focuses the search input inside the dialog when opened", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: /pick players/i }));

		const search = screen.getByRole("textbox", { name: "search" });
		expect(search).toHaveFocus();
		expect(screen.getByRole("dialog")).toContainElement(search);

		await user.type(search, "bo");
		expect(search).toHaveValue("bo");
		expect(screen.queryByRole("button", { name: "Alice" })).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Bob" })).toBeInTheDocument();
	});

	it("closes only the dropdown on Escape and keeps the dialog open", async () => {
		const user = userEvent.setup();
		const onDialogClose = vi.fn();
		renderWithTheme(<Harness onDialogClose={onDialogClose} />);

		await user.click(screen.getByRole("button", { name: /pick players/i }));
		expect(screen.getByRole("textbox", { name: "search" })).toBeInTheDocument();

		await user.keyboard("{Escape}");

		expect(screen.queryByRole("textbox", { name: "search" })).not.toBeInTheDocument();
		expect(onDialogClose).not.toHaveBeenCalled();
		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	it("adds the picked option to the selection", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: /pick players/i }));
		await user.click(screen.getByRole("button", { name: "Alice" }));

		expect(screen.getByText("Alice")).toBeInTheDocument();
		expect(screen.queryByRole("textbox", { name: "search" })).not.toBeInTheDocument();
	});
});
