import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { TempPasswordModal } from "./TempPasswordModal";

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("TempPasswordModal", () => {
	it("shows the temporary password", () => {
		renderWithTheme(<TempPasswordModal open close={vi.fn()} username="kathleen" tempPassword="tmp-1234" />);

		expect(screen.getByText("tmp-1234")).toBeInTheDocument();
	});

	it("puts close on the left as cancel and copy on the right", () => {
		renderWithTheme(<TempPasswordModal open close={vi.fn()} username="kathleen" tempPassword="tmp-1234" />);

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:close", "account.users.temp-password.copy"]);
		expect(buttons[0]).toHaveClass("border-cancel/60");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from the close button", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithTheme(<TempPasswordModal open close={close} username="kathleen" tempPassword="tmp-1234" />);

		await user.click(within(footer()).getByRole("button", { name: "common:close" }));

		expect(close).toHaveBeenCalledTimes(1);
	});

	it("copies the temporary password to the clipboard", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
		renderWithTheme(<TempPasswordModal open close={vi.fn()} username="kathleen" tempPassword="tmp-1234" />);

		await user.click(within(footer()).getByRole("button", { name: "account.users.temp-password.copy" }));

		expect(writeText).toHaveBeenCalledWith("tmp-1234");
		expect(
			await within(footer()).findByRole("button", { name: "account.users.temp-password.copied" }),
		).toBeInTheDocument();
	});
});
