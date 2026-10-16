import type { AnyFieldApi } from "@tanstack/react-form";
import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { SettingsToggle } from "./SettingsToggle";

const createMockField = (value: boolean) =>
	({
		state: { value, meta: { errors: [] } },
		handleChange: vi.fn(),
		handleBlur: vi.fn(),
	}) as unknown as AnyFieldApi;

describe("SettingsToggle", () => {
	it("names the switch after its label", () => {
		renderWithTheme(
			<SettingsToggle
				field={createMockField(true)}
				label="Create accounts on first sign-in"
				description="New people"
			/>,
		);

		const toggle = screen.getByRole("switch", { name: "Create accounts on first sign-in" });
		expect(toggle).toHaveAttribute("aria-checked", "true");
	});

	it("reports the new value when the named switch is flipped", async () => {
		const field = createMockField(true);
		renderWithTheme(<SettingsToggle field={field} label="Enabled" description="Show the button" />);

		await userEvent.click(screen.getByRole("switch", { name: "Enabled" }));

		expect(field.handleChange).toHaveBeenCalledWith(false);
	});
});
