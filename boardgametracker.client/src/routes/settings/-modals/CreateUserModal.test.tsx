import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { CreateUserModal } from "./CreateUserModal";

vi.mock("@/utils/errorUtils", () => ({
	translateApiError: (_message: string, fallbackKey: string) => fallbackKey,
}));

const renderModal = (overrides: Partial<Parameters<typeof CreateUserModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		players: [],
		users: [],
		onSubmit: vi.fn(() => Promise.resolve()),
		isLoading: false,
		...overrides,
	};
	renderWithTheme(<CreateUserModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("CreateUserModal", () => {
	it("puts cancel on the left and the create submit on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "account.users.create.save"]);
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

	it("creates the user from the footer submit and closes", async () => {
		const user = userEvent.setup();
		const { close, onSubmit } = renderModal();

		await user.type(screen.getByLabelText("account.users.create.username.label"), "kathleen");
		await user.type(screen.getByLabelText("account.users.create.email.label"), "kathleen@example.com");
		await user.type(screen.getByLabelText("account.users.create.password.label"), "secret");
		await user.click(within(footer()).getByRole("button", { name: "account.users.create.save" }));

		await waitFor(() =>
			expect(onSubmit).toHaveBeenCalledWith({
				username: "kathleen",
				email: "kathleen@example.com",
				password: "secret",
				role: "User",
				createPlayer: true,
				playerId: null,
			}),
		);
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("does not submit an empty form", async () => {
		const user = userEvent.setup();
		const { onSubmit } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "account.users.create.save" }));

		await waitFor(() => expect(onSubmit).not.toHaveBeenCalled());
	});

	it("disables both footer buttons while saving", () => {
		renderModal({ isLoading: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "account.users.create.save" })).toBeDisabled();
	});
});
