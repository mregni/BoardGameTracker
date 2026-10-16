import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { ChangePasswordModal } from "./ChangePasswordModal";

vi.mock("@/utils/errorUtils", () => ({
	translateApiError: (_message: string, fallbackKey: string) => fallbackKey,
}));

const renderModal = (overrides: Partial<Parameters<typeof ChangePasswordModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		onSubmit: vi.fn(() => Promise.resolve()),
		isLoading: false,
		...overrides,
	};
	renderWithTheme(<ChangePasswordModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("ChangePasswordModal", () => {
	it("puts cancel on the left and the save submit on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "account.password.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without submitting", async () => {
		const user = userEvent.setup();
		const { close, onSubmit } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onSubmit).not.toHaveBeenCalled();
	});

	it("changes the password from the footer submit and closes", async () => {
		const user = userEvent.setup();
		const { close, onSubmit } = renderModal();

		await user.type(screen.getByLabelText("account.password.current.label"), "old-secret");
		await user.type(screen.getByLabelText("account.password.new.label"), "new-secret");
		await user.type(screen.getByLabelText("account.password.confirm.label"), "new-secret");
		await user.click(within(footer()).getByRole("button", { name: "account.password.save" }));

		await waitFor(() =>
			expect(onSubmit).toHaveBeenCalledWith({ currentPassword: "old-secret", newPassword: "new-secret" }),
		);
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("does not submit when the confirmation does not match", async () => {
		const user = userEvent.setup();
		const { onSubmit } = renderModal();

		await user.type(screen.getByLabelText("account.password.current.label"), "old-secret");
		await user.type(screen.getByLabelText("account.password.new.label"), "new-secret");
		await user.type(screen.getByLabelText("account.password.confirm.label"), "other-secret");
		await user.click(within(footer()).getByRole("button", { name: "account.password.save" }));

		expect(await screen.findByText("account.password.mismatch")).toBeInTheDocument();
		expect(onSubmit).not.toHaveBeenCalled();
	});

	it("disables both footer buttons while saving", () => {
		renderModal({ isLoading: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "account.password.save" })).toBeDisabled();
	});
});
