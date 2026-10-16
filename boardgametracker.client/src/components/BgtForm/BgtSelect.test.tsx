import type { AnyFieldApi } from "@tanstack/react-form";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, waitFor } from "@/test/test-utils";
import { BgtSelect } from "./BgtSelect";

// i18next is mocked globally in setup.ts

vi.mock("@/assets/icons/magnifying-glass.svg?react", () => ({
	default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="search-icon" {...props} />,
}));

vi.mock("@/assets/icons/check.svg?react", () => ({
	default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

vi.mock("@/assets/icons/caret-up.svg?react", () => ({
	default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="caret-up-icon" {...props} />,
}));

vi.mock("@/assets/icons/caret-down.svg?react", () => ({
	default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="caret-down-icon" {...props} />,
}));

const createMockField = (value: string = "") =>
	({
		state: {
			value,
			meta: {
				errors: [] as string[],
			},
		},
		handleChange: vi.fn(),
		handleBlur: vi.fn(),
	}) as unknown as AnyFieldApi;

const mockItems = [
	{ value: "1", label: "Option 1" },
	{ value: "2", label: "Option 2" },
	{ value: "3", label: "Option 3" },
];

describe("BgtSelect", () => {
	let mockField: ReturnType<typeof createMockField>;

	beforeEach(() => {
		mockField = createMockField();
		vi.clearAllMocks();
	});

	describe("Rendering", () => {
		it("should render select with label", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByText("Category")).toBeInTheDocument();
		});

		it("should render trigger button", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByRole("combobox")).toBeInTheDocument();
		});

		it("should show the placeholder when the value is not one of the items", () => {
			renderWithTheme(
				<BgtSelect field={createMockField("0")} label="Game" items={mockItems} placeholder="Choose game" />,
			);
			expect(screen.getByText("Choose game")).toBeInTheDocument();
		});

		it("should name the trigger after its label", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByRole("combobox", { name: "Category" })).toBeInTheDocument();
		});

		it("should show placeholder when no value", () => {
			renderWithTheme(
				<BgtSelect field={mockField} label="Category" items={mockItems} placeholder="Select an option" />,
			);
			expect(screen.getByText("Select an option")).toBeInTheDocument();
		});

		it("should show caret down icon when closed", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByTestId("caret-down-icon")).toBeInTheDocument();
		});

		it("should display selected value", () => {
			mockField = createMockField("2");
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByText("Option 2")).toBeInTheDocument();
		});

		it("should show the initial of a player without a picture when avatars are on", () => {
			mockField = createMockField("1");
			renderWithTheme(
				<BgtSelect
					field={mockField}
					label="Player"
					showAvatars
					items={[{ value: "1", label: "Kathleen", image: null }]}
				/>,
			);
			expect(screen.getByText("K")).toBeInTheDocument();
		});

		it("should leave items without an image bare when avatars are off", () => {
			mockField = createMockField("1");
			renderWithTheme(
				<BgtSelect field={mockField} label="Game" items={[{ value: "1", label: "Kingdomino", image: null }]} />,
			);
			expect(screen.queryByText("K")).not.toBeInTheDocument();
		});
	});

	describe("Disabled State", () => {
		it("should be disabled when disabled prop is true", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} disabled />);
			expect(screen.getByRole("combobox")).toBeDisabled();
		});

		it("should not be disabled by default", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByRole("combobox")).not.toBeDisabled();
		});
	});

	describe("Error State", () => {
		it("should display error message", () => {
			mockField.state.meta.errors = ["Please select an option"];
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} />);
			expect(screen.getByText("Please select an option")).toBeInTheDocument();
		});
	});

	describe("Props", () => {
		it("should accept empty items array", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={[]} />);
			expect(screen.getByRole("combobox")).toBeInTheDocument();
		});

		it("should handle hasSearch prop without error", () => {
			renderWithTheme(<BgtSelect field={mockField} label="Category" items={mockItems} hasSearch />);
			expect(screen.getByRole("combobox")).toBeInTheDocument();
		});
	});

	describe("Items arriving after the value", () => {
		it("keeps the field value and shows it once the items load inside a form", () => {
			const field = createMockField("2");
			const { rerender } = renderWithTheme(
				<form>
					<BgtSelect field={field} label="Game" items={[]} placeholder="Choose game" />
				</form>,
			);
			expect(screen.getByText("Choose game")).toBeInTheDocument();

			rerender(
				<form>
					<BgtSelect field={field} label="Game" items={mockItems} placeholder="Choose game" />
				</form>,
			);

			expect(field.handleChange).not.toHaveBeenCalled();
			expect(screen.getByRole("combobox", { name: "Game" })).toHaveTextContent("Option 2");
		});

		it("keeps the controlled value when the items load inside a form", () => {
			const onValueChange = vi.fn();
			const { rerender } = renderWithTheme(
				<form>
					<BgtSelect label="Location" items={[]} value={3} onValueChange={onValueChange} />
				</form>,
			);

			rerender(
				<form>
					<BgtSelect label="Location" items={mockItems} value={3} onValueChange={onValueChange} />
				</form>,
			);

			expect(onValueChange).not.toHaveBeenCalled();
		});
	});

	describe("Controlled", () => {
		beforeAll(() => {
			Element.prototype.hasPointerCapture = vi.fn(() => false);
			Element.prototype.releasePointerCapture = vi.fn();
			Element.prototype.scrollIntoView = vi.fn();
		});

		it("reports the picked value and keeps showing the placeholder when its value stays empty", async () => {
			const user = userEvent.setup();
			const onValueChange = vi.fn();
			renderWithTheme(
				<BgtSelect
					label="Players"
					items={mockItems}
					value={null}
					onValueChange={onValueChange}
					placeholder="Add a player"
				/>,
			);

			await user.click(screen.getByRole("combobox", { name: "Players" }));
			await user.click(await screen.findByRole("option", { name: "Option 2" }));

			expect(onValueChange).toHaveBeenCalledWith("2");
			expect(screen.getByText("Add a player")).toBeInTheDocument();
		});

		it("shows the errors it is given", () => {
			renderWithTheme(
				<BgtSelect label="Players" items={mockItems} value={null} onValueChange={vi.fn()} errors={["Add a player"]} />,
			);
			expect(screen.getByText("Add a player")).toBeInTheDocument();
		});

		it("shows group headings and drops them while searching", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<BgtSelect
					label="Game"
					hasSearch
					value={null}
					onValueChange={vi.fn()}
					items={[
						{ value: "1", label: "Azul", group: "Recently played" },
						{ value: "2", label: "Catan", group: "All games" },
						{ value: "3", label: "Carcassonne", group: "All games" },
					]}
				/>,
			);

			await user.click(screen.getByRole("combobox", { name: "Game" }));
			expect(await screen.findByText("Recently played")).toBeInTheDocument();
			expect(screen.getByText("All games")).toBeInTheDocument();

			await user.type(screen.getByPlaceholderText("search"), "ca");
			expect(screen.queryByText("Recently played")).not.toBeInTheDocument();
			expect(screen.queryByText("All games")).not.toBeInTheDocument();
			expect(screen.getByRole("option", { name: "Catan" })).toBeInTheDocument();
			expect(screen.getByRole("option", { name: "Carcassonne" })).toBeInTheDocument();
			expect(screen.queryByRole("option", { name: "Azul" })).not.toBeInTheDocument();
		});
	});

	describe("Keyboard", () => {
		const games = [
			{ value: 1, label: "Agricola" },
			{ value: 12, label: "Carcassonne" },
			{ value: 14, label: "Catan" },
		];

		beforeAll(() => {
			Element.prototype.hasPointerCapture = vi.fn(() => false);
			Element.prototype.setPointerCapture = vi.fn();
			Element.prototype.releasePointerCapture = vi.fn();
			Element.prototype.scrollIntoView = vi.fn();
		});

		it("should pick the searched game with Enter and return focus to the field", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtSelect field={mockField} label="Game" items={games} hasSearch />);

			await user.click(screen.getByRole("combobox"));
			await user.type(await screen.findByPlaceholderText("search"), "carc{Enter}");

			expect(mockField.handleChange).toHaveBeenCalledWith(12);
			await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
			await waitFor(() => expect(screen.getByRole("combobox")).toHaveFocus());
		});

		it("should keep the list open on Enter when nothing matches", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtSelect field={mockField} label="Game" items={games} hasSearch />);

			await user.click(screen.getByRole("combobox"));
			await user.type(await screen.findByPlaceholderText("search"), "brass{Enter}");

			expect(mockField.handleChange).not.toHaveBeenCalled();
			expect(screen.getByRole("listbox")).toBeInTheDocument();
		});

		it("should move focus from the search box to the first option with ArrowDown", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtSelect field={mockField} label="Game" items={games} hasSearch />);

			await user.click(screen.getByRole("combobox"));
			await user.type(await screen.findByPlaceholderText("search"), "cat{ArrowDown}");

			expect(document.activeElement).toHaveAttribute("role", "option");
			expect(document.activeElement).toHaveTextContent("Catan");
		});

		it("should close the list with Escape from the search box", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtSelect field={mockField} label="Game" items={games} hasSearch />);

			await user.click(screen.getByRole("combobox"));
			await user.type(await screen.findByPlaceholderText("search"), "ca{Escape}");

			await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
			expect(mockField.handleChange).not.toHaveBeenCalled();
		});

		it("should report the first match in controlled mode", async () => {
			const user = userEvent.setup();
			const onValueChange = vi.fn();
			renderWithTheme(<BgtSelect label="Game" items={games} value={null} onValueChange={onValueChange} hasSearch />);

			await user.click(screen.getByRole("combobox"));
			await user.type(await screen.findByPlaceholderText("search"), "ca{Enter}");

			expect(onValueChange).toHaveBeenCalledWith(12);
			await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
		});

		it("should pick the first option of the flat search results when items are grouped", async () => {
			const user = userEvent.setup();
			const onValueChange = vi.fn();
			renderWithTheme(
				<BgtSelect
					label="Game"
					hasSearch
					value={null}
					onValueChange={onValueChange}
					items={[
						{ value: "2", label: "Catan", group: "All games" },
						{ value: "1", label: "Azul", group: "Recently played" },
						{ value: "3", label: "Carcassonne", group: "All games" },
					]}
				/>,
			);

			await user.click(screen.getByRole("combobox"));
			const search = await screen.findByPlaceholderText("search");
			await user.type(search, "a");
			expect(screen.getAllByRole("option").map((option) => option.textContent)).toEqual([
				"Catan",
				"Azul",
				"Carcassonne",
			]);

			await user.type(search, "{Enter}");

			expect(onValueChange).toHaveBeenCalledWith("2");
		});
	});
});
