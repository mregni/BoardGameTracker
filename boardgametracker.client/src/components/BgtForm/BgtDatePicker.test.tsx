import type { AnyFieldApi } from "@tanstack/react-form";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { BgtDatePicker } from "./BgtDatePicker";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () =>
			Promise.resolve({
				dateFormat: "yyyy-MM-dd",
				timeFormat: "HH:mm",
				uiLanguage: "en-us",
			}),
	}),
}));

const createMockField = (value: string = "", errors: string[] = []) =>
	({
		state: {
			value,
			meta: {
				errors,
				isTouched: false,
				isValidating: false,
			},
		},
		handleChange: vi.fn(),
		handleBlur: vi.fn(),
		name: "testDate",
	}) as unknown as AnyFieldApi;

describe("BgtDatePicker", () => {
	const defaultProps = {
		field: createMockField(),
		label: "Select Date",
		placeholder: "Pick a date",
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Rendering", () => {
		it("should render the label", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			expect(screen.getByText("Select Date")).toBeInTheDocument();
		});

		it("should render the calendar trigger button", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			expect(screen.getByLabelText("Open calendar")).toBeInTheDocument();
		});

		it("should render segmented date input", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			// react-aria DateSegment renders each segment as role="spinbutton" (day/month/year)
			const segments = screen.getAllByRole("spinbutton");
			expect(segments.length).toBeGreaterThanOrEqual(3);
		});

		it("should apply custom className to the input group", () => {
			const { container } = renderWithProviders(<BgtDatePicker {...defaultProps} className="custom-class" />);
			expect(container.querySelector(".custom-class")).toBeInTheDocument();
		});
	});

	describe("Disabled State", () => {
		it("should disable the calendar button when disabled is true", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} disabled />);
			expect(screen.getByLabelText("Open calendar")).toBeDisabled();
		});

		it("should not disable the calendar button by default", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			expect(screen.getByLabelText("Open calendar")).not.toBeDisabled();
		});
	});

	describe("Error State", () => {
		it("should display error messages when field has errors", () => {
			const fieldWithErrors = createMockField("", ["Date is required"]);
			renderWithProviders(<BgtDatePicker {...defaultProps} field={fieldWithErrors} />);

			expect(screen.getByText("Date is required")).toBeInTheDocument();
		});

		it("should not display error messages when field has no errors", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			expect(screen.queryByText("Date is required")).not.toBeInTheDocument();
		});
	});

	describe("Popover Interaction", () => {
		it("should open the calendar popover when the calendar button is clicked", async () => {
			const user = userEvent.setup();
			renderWithProviders(<BgtDatePicker {...defaultProps} />);

			await user.click(screen.getByLabelText("Open calendar"));

			await waitFor(() => {
				expect(screen.getByRole("dialog")).toBeInTheDocument();
			});
			expect(screen.getByRole("grid")).toBeInTheDocument();
		});

		it("should not open the popover when disabled", async () => {
			const user = userEvent.setup();
			renderWithProviders(<BgtDatePicker {...defaultProps} disabled />);

			await user.click(screen.getByLabelText("Open calendar"));

			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});
	});

	describe("Date Selection", () => {
		it("should call handleChange when a calendar date is selected", async () => {
			const user = userEvent.setup();
			const mockHandleChange = vi.fn();
			const field = {
				...createMockField("2024-06-15"),
				handleChange: mockHandleChange,
			} as unknown as AnyFieldApi;

			renderWithProviders(<BgtDatePicker {...defaultProps} field={field} />);

			await user.click(screen.getByLabelText("Open calendar"));

			await waitFor(() => {
				expect(screen.getByRole("grid")).toBeInTheDocument();
			});

			// Pick a different day in the visible month (June 2024). The cells are buttons.
			const cells = screen.getAllByRole("button").filter((el) => el.textContent?.trim() === "20");
			expect(cells.length).toBeGreaterThan(0);
			await user.click(cells[0]);

			expect(mockHandleChange).toHaveBeenCalled();
			expect(mockHandleChange).toHaveBeenCalledWith("2024-06-20");
		});
	});

	describe("Pre-selected Date", () => {
		it("should populate the segments with the field value", async () => {
			const fieldWithValue = createMockField("2024-06-15");
			renderWithProviders(<BgtDatePicker {...defaultProps} field={fieldWithValue} />);

			// Wait for settings (and thus locale) to load before the segments hydrate the value.
			await waitFor(() => {
				const text = screen.getByRole("group", { name: "Select Date" }).textContent ?? "";
				expect(text).toContain("2024");
				expect(text).toContain("15");
			});
		});
	});

	describe("Accessibility", () => {
		it("should expose the input group with the label as an accessible name", () => {
			renderWithProviders(<BgtDatePicker {...defaultProps} />);
			expect(screen.getByRole("group", { name: "Select Date" })).toBeInTheDocument();
		});
	});
});
