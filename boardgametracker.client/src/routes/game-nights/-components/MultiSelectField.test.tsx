import { useState } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { BgtDialog, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog/BgtDialog";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
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
				<form data-testid="form">
					<MultiSelectField
						label="Players"
						placeholder="Pick players"
						options={options}
						selected={selected}
						disabled={false}
						onChange={setSelected}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};

beforeAll(() => {
	Element.prototype.hasPointerCapture = vi.fn(() => false);
	Element.prototype.releasePointerCapture = vi.fn();
	Element.prototype.scrollIntoView = vi.fn();
});

const openList = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByRole("combobox", { name: "Players" }));
	return screen.findByRole("listbox");
};

describe("MultiSelectField", () => {
	it("opens the list in a floating layer outside the form flow", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		const listbox = await openList(user);

		expect(screen.getByTestId("form")).not.toContainElement(listbox);
		expect(screen.getByTestId("form")).not.toContainElement(screen.getByPlaceholderText("search"));
	});

	it("focuses the search input and filters the options", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		await openList(user);
		const search = screen.getByPlaceholderText("search");
		await waitFor(() => expect(search).toHaveFocus());

		await user.type(search, "bo");
		expect(search).toHaveValue("bo");
		expect(screen.queryByRole("option", { name: /Alice/ })).not.toBeInTheDocument();
		expect(screen.getByRole("option", { name: /Bob/ })).toBeInTheDocument();
	});

	it("closes only the list on Escape and keeps the dialog open", async () => {
		const user = userEvent.setup();
		const onDialogClose = vi.fn();
		renderWithTheme(<Harness onDialogClose={onDialogClose} />);

		await openList(user);
		await user.keyboard("{Escape}");

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		expect(onDialogClose).not.toHaveBeenCalled();
		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	it("adds the picked option as a chip and removes it again", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		await openList(user);
		await user.click(screen.getByRole("option", { name: /Alice/ }));

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		const form = screen.getByTestId("form");
		const chip = within(form).getByText("Alice", { selector: "span" }).parentElement as HTMLElement;
		expect(chip).toBeInTheDocument();

		await user.click(within(chip).getByRole("button"));
		expect(within(form).queryByText("Alice", { selector: "span" })).not.toBeInTheDocument();
	});

	it("names each remove button after its chip", async () => {
		const user = userEvent.setup();
		renderWithTheme(<Harness onDialogClose={vi.fn()} />);

		await openList(user);
		await user.click(screen.getByRole("option", { name: /Bob/ }));

		await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		await user.click(screen.getByRole("button", { name: "common:remove-item" }));
		expect(within(screen.getByTestId("form")).queryByText("Bob", { selector: "span" })).not.toBeInTheDocument();
	});
});
