import { afterAll, beforeAll, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { EditableSelectCell } from "./EditableSelectCell";

beforeAll(() => {
	HTMLElement.prototype.hasPointerCapture = vi.fn(() => false);
	HTMLElement.prototype.setPointerCapture = vi.fn();
	HTMLElement.prototype.releasePointerCapture = vi.fn();
	HTMLElement.prototype.scrollIntoView = vi.fn();
});

afterAll(() => {
	vi.restoreAllMocks();
});

const items = [
	{ value: "owned", label: "Owned" },
	{ value: "wanted", label: "Wanted" },
];

const renderCell = (overrides: Partial<Parameters<typeof EditableSelectCell>[0]> = {}) => {
	const props = {
		value: "owned",
		items,
		editing: false,
		onStartEdit: vi.fn(),
		onStopEdit: vi.fn(),
		onChange: vi.fn(),
		...overrides,
	};
	renderWithTheme(<EditableSelectCell {...props} />);
	return props;
};

describe("EditableSelectCell", () => {
	it("shows plain text without an editor for read-only users", () => {
		renderCell({ readOnly: true, editing: true });

		expect(screen.getByText("Owned")).toBeInTheDocument();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("shows the selected label as a button when not editing", async () => {
		const user = userEvent.setup();
		const props = renderCell();

		await user.click(screen.getByRole("button", { name: "Owned" }));

		expect(props.onStartEdit).toHaveBeenCalledTimes(1);
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("shows a dash when the value is not in the list", () => {
		renderCell({ value: "unknown" });

		expect(screen.getByRole("button", { name: "-" })).toBeInTheDocument();
	});

	it("opens the select immediately when editing", () => {
		renderCell({ editing: true });

		expect(screen.getByRole("listbox")).toBeInTheDocument();
		expect(screen.getByRole("option", { name: "Wanted" })).toBeInTheDocument();
	});

	it("commits the chosen value and stops editing", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		await user.click(screen.getByRole("option", { name: "Wanted" }));

		expect(props.onChange).toHaveBeenCalledWith("wanted");
		expect(props.onStopEdit).toHaveBeenCalled();
	});

	it("stops editing when the select closes without a choice", async () => {
		const user = userEvent.setup();
		const props = renderCell({ editing: true });

		await user.keyboard("{Escape}");

		expect(props.onChange).not.toHaveBeenCalled();
		expect(props.onStopEdit).toHaveBeenCalled();
	});
});
