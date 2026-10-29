import type { AnyFieldApi } from "@tanstack/react-form";
import { useState } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";
import { BgtDateTimePicker } from "./BgtDateTimePicker";

const mockSettings = vi.hoisted(() => ({
	current: { dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-US" },
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve(mockSettings.current),
	}),
}));

const createMockField = (value: Date | string = "", errors: string[] = []) =>
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
		name: "testDateTime",
	}) as unknown as AnyFieldApi;

const timeGroup = () => screen.getByRole("group", { name: "common:time" });
const timeSegment = (name: RegExp) => within(timeGroup()).getByRole("spinbutton", { name });

describe("BgtDateTimePicker", () => {
	const defaultProps = {
		field: createMockField(),
		label: "Select Date and Time",
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockSettings.current = { dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-US" };
	});

	describe("Rendering", () => {
		it("should render the label, the date picker and a labelled segmented time field", () => {
			renderWithProviders(<BgtDateTimePicker {...defaultProps} />);

			expect(screen.getByText("Select Date and Time")).toBeInTheDocument();
			expect(screen.getByRole("button", { name: /open-calendar/ })).toBeInTheDocument();
			expect(timeSegment(/hour/i)).toBeInTheDocument();
			expect(timeSegment(/minute/i)).toBeInTheDocument();
		});

		it("should show placeholders when no date is set", () => {
			renderWithProviders(<BgtDateTimePicker {...defaultProps} />);

			expect(timeSegment(/hour/i)).not.toHaveAttribute("aria-valuenow");
			expect(timeSegment(/minute/i)).not.toHaveAttribute("aria-valuenow");
		});

		it("should show the time of the field value", () => {
			renderWithProviders(
				<BgtDateTimePicker {...defaultProps} field={createMockField(new Date("2024-06-15T14:05:00"))} />,
			);

			expect(timeSegment(/hour/i)).toHaveTextContent("14");
			expect(timeSegment(/minute/i)).toHaveTextContent("05");
		});

		it("should handle an ISO string value", () => {
			renderWithProviders(<BgtDateTimePicker {...defaultProps} field={createMockField("2024-06-15T10:30:00.000Z")} />);

			expect(timeSegment(/minute/i)).toHaveTextContent("30");
		});
	});

	describe("Hour cycle", () => {
		it("should use a 24-hour clock for HH:mm", async () => {
			renderWithProviders(
				<BgtDateTimePicker {...defaultProps} field={createMockField(new Date("2024-06-15T14:30:00"))} />,
			);

			expect(await screen.findByRole("spinbutton", { name: /hour/i })).toHaveTextContent("14");
			expect(within(timeGroup()).queryByRole("spinbutton", { name: /AM\/PM/i })).not.toBeInTheDocument();
		});

		it("should use a 12-hour clock for hh:mm a", async () => {
			mockSettings.current = { dateFormat: "yyyy-MM-dd", timeFormat: "hh:mm a", uiLanguage: "en-US" };

			renderWithProviders(
				<BgtDateTimePicker {...defaultProps} field={createMockField(new Date("2024-06-15T14:30:00"))} />,
			);

			expect(await screen.findByRole("spinbutton", { name: /AM\/PM/i })).toHaveTextContent("PM");
			expect(timeSegment(/hour/i)).toHaveTextContent("02");
		});
	});

	describe("Disabled State", () => {
		it("should disable both the date and the time field", () => {
			renderWithProviders(<BgtDateTimePicker {...defaultProps} disabled={true} />);

			expect(screen.getByRole("button", { name: /open-calendar/ })).toBeDisabled();
			expect(timeSegment(/hour/i)).toHaveAttribute("aria-disabled", "true");
		});
	});

	describe("Date changes", () => {
		it("should keep the last valid date when every date segment is cleared", async () => {
			const user = userEvent.setup();
			const changes: unknown[] = [];
			const StatefulPicker = () => {
				const [value, setValue] = useState<Date | string>(new Date(2024, 5, 15, 10, 30));
				const field = {
					...createMockField(value),
					handleChange: (next: Date) => {
						changes.push(next);
						setValue(next);
					},
				} as unknown as AnyFieldApi;
				return <BgtDateTimePicker {...defaultProps} field={field} />;
			};

			renderWithProviders(<StatefulPicker />);

			const timeSegments = within(timeGroup()).getAllByRole("spinbutton");
			const dateSegments = screen.getAllByRole("spinbutton").filter((segment) => !timeSegments.includes(segment));
			for (const segment of dateSegments) {
				act(() => segment.focus());
				await user.keyboard("{Backspace}{Backspace}{Backspace}{Backspace}");
			}

			expect(changes.every((change) => change instanceof Date && !Number.isNaN(change.getTime()))).toBe(true);
			expect(timeSegment(/hour/i)).toHaveTextContent("10");
		});

		it("should render an invalid stored value as empty instead of crashing", () => {
			renderWithProviders(<BgtDateTimePicker {...defaultProps} field={createMockField(new Date(""))} />);

			expect(timeSegment(/hour/i)).not.toHaveAttribute("aria-valuenow");
		});
	});

	describe("Time changes", () => {
		it("should keep the date and update the time when the hour is changed", async () => {
			const user = userEvent.setup();
			const field = createMockField(new Date(2024, 5, 15, 10, 30));

			renderWithProviders(<BgtDateTimePicker {...defaultProps} field={field} />);

			act(() => timeSegment(/hour/i).focus());
			await user.keyboard("{ArrowUp}");

			expect(field.handleChange).toHaveBeenCalledWith(new Date(2024, 5, 15, 11, 30));
		});

		it("should update the minutes when they are typed", async () => {
			const user = userEvent.setup();
			const field = createMockField(new Date(2024, 5, 15, 10, 30));

			renderWithProviders(<BgtDateTimePicker {...defaultProps} field={field} />);

			act(() => timeSegment(/minute/i).focus());
			await user.keyboard("7");

			expect(field.handleChange).toHaveBeenLastCalledWith(new Date(2024, 5, 15, 10, 7));
		});
	});
});
