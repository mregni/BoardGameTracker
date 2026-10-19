import { describe, expect, it, vi } from "vitest";
import type { UserDto } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { EditUserModal } from "./EditUserModal";

vi.mock("@/utils/errorUtils", () => ({
	translateApiError: (_message: string, fallbackKey: string) => fallbackKey,
}));

const editedUser: UserDto = {
	id: "user-1",
	username: "kathleen",
	email: "kathleen@example.com",
	roles: ["Admin"],
	createdAt: new Date(2026, 0, 1),
	lastLoginAt: null,
	playerId: null,
	isDisabled: false,
	roleManagedByIdentityProvider: false,
	externalLoginCount: 0,
};

const renderModal = (overrides: Partial<Parameters<typeof EditUserModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		user: editedUser,
		players: [],
		users: [editedUser],
		onSubmit: vi.fn(() => Promise.resolve()),
		isLoading: false,
		...overrides,
	};
	renderWithTheme(<EditUserModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("EditUserModal", () => {
	it("puts cancel on the left and the save submit on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "common:save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("has no subtitle that repeats the title", () => {
		renderModal();

		expect(screen.queryByText("account.users.edit.description")).not.toBeInTheDocument();
		expect(screen.getByRole("textbox", { name: "auth:username" })).toBeInTheDocument();
	});

	it("closes from cancel without submitting", async () => {
		const user = userEvent.setup();
		const { close, onSubmit } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onSubmit).not.toHaveBeenCalled();
	});

	it("saves the changed user from the footer submit and closes", async () => {
		const user = userEvent.setup();
		const { close, onSubmit } = renderModal();

		const username = screen.getByLabelText("auth:username");
		await user.clear(username);
		await user.type(username, "kat");
		await user.click(within(footer()).getByRole("button", { name: "common:save" }));

		await waitFor(() =>
			expect(onSubmit).toHaveBeenCalledWith({
				userId: "user-1",
				username: "kat",
				email: "kathleen@example.com",
				role: "Admin",
				playerId: null,
			}),
		);
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("disables both footer buttons while saving", () => {
		renderModal({ isLoading: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "common:save" })).toBeDisabled();
	});
});
