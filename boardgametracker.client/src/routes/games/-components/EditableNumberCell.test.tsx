import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { EditableNumberCell } from "./EditableNumberCell";

const renderCell = (overrides: Partial<Parameters<typeof EditableNumberCell>[0]> = {}) => {
	const props = {
		value: 12.5,
		editing: false,
		onStartEdit: vi.fn(),
		onStopEdit: vi.fn(),
		onChange: vi.fn(),
		prefix: "€",
		...overrides,
	};
	renderWithTheme(<EditableNumberCell {...props} />);
	return props;
};

describe("EditableNumberCell", () => {
	it("shows the formatted value as a button when not editing", async () => {
		const user = userEvent.setup();
		const props = renderCell();

		const button = screen.getByRole("button", { name: "€ 12.5" });
		await user.click(button);

		expect(props.onStartEdit).toHaveBeenCalledTimes(1);
		expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
	});

	it("shows a dash for an empty value", () => {
		renderCell({ value: null });

		expect(screen.getByRole("button", { name: "–" })).toBeInTheDocument();
	});

	it("focuses the input and commits the new value on Enter", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		const input = screen.getByRole("textbox");
		expect(input).toHaveFocus();
		await user.clear(input);
		await user.type(input, "20{Enter}");

		expect(props.onChange).toHaveBeenCalledWith(20);
		expect(props.onStopEdit).toHaveBeenCalled();
	});

	it("commits null when the input is cleared", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		await user.clear(screen.getByRole("textbox"));
		await user.tab();

		expect(props.onChange).toHaveBeenCalledWith(null);
	});

	it("does not commit an unchanged value", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		await user.tab();

		expect(props.onChange).not.toHaveBeenCalled();
		expect(props.onStopEdit).toHaveBeenCalled();
	});

	it("discards the draft on Escape", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		const input = screen.getByRole("textbox");
		await user.clear(input);
		await user.type(input, "99{Escape}");

		expect(props.onChange).not.toHaveBeenCalled();
		expect(props.onStopEdit).toHaveBeenCalled();
	});

	it.each([
		["12,75", 12.75],
		["12.75", 12.75],
	])("accepts %s as a decimal", async (typed, expected) => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		const input = screen.getByRole("textbox");
		expect(input).toHaveAttribute("inputmode", "decimal");
		await user.clear(input);
		await user.type(input, `${typed}{Enter}`);

		expect(props.onChange).toHaveBeenCalledWith(expected);
	});

	it("does not commit text that is not a number or is out of range", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true, min: 0 });

		const input = screen.getByRole("textbox");
		await user.clear(input);
		await user.type(input, "-5{Enter}");

		expect(props.onChange).not.toHaveBeenCalled();
		expect(props.onStopEdit).toHaveBeenCalled();
	});

	it("formats the value without a custom formatter", () => {
		renderCell({ value: 12.345, prefix: undefined, suffix: "kg" });

		expect(screen.getByRole("button", { name: "12.35 kg" })).toBeInTheDocument();
	});
});
