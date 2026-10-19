import { beforeEach, describe, expect, it, vi } from "vitest";
import type { UserDto } from "@/models";
import { fireEvent, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	users: [] as UserDto[],
	accessModal: vi.fn(),
	resetPassword: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ errorToast: mocks.errorToast, successToast: vi.fn() }),
}));

vi.mock("../-modals/TempPasswordModal", () => ({
	TempPasswordModal: (props: { username: string; tempPassword: string }) => (
		<div role="dialog" aria-label="temp-password">
			{props.username}:{props.tempPassword}
		</div>
	),
}));

vi.mock("../-hooks/useAccountData", () => ({
	useAccountData: () => ({
		profile: {
			id: "admin-1",
			username: "admin",
			email: null,
			displayName: "Administrator",
			roles: ["Admin"],
			createdAt: new Date(2026, 0, 1),
			lastLoginAt: null,
			playerId: null,
		},
		isProfileLoading: false,
		users: mocks.users,
		linkablePlayers: [],
		players: [],
		isAdmin: true,
		updateProfile: vi.fn(),
		isUpdatingProfile: false,
		changePassword: vi.fn(),
		isChangingPassword: false,
		registerUser: vi.fn(),
		isRegistering: false,
		deleteUser: vi.fn(),
		updateUser: vi.fn(),
		isUpdatingUser: false,
		resetPassword: mocks.resetPassword,
	}),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { user: { id: string } }) => unknown) => selector({ user: { id: "admin-1" } }),
}));

vi.mock("./ExternalLoginsSection", () => ({
	ExternalLoginsSection: () => null,
}));

vi.mock("../-modals/UserAccessModal", () => ({
	UserAccessModal: (props: { user: UserDto; isCurrentUser: boolean; close: () => void }) => {
		mocks.accessModal(props);
		return (
			<div role="dialog" aria-label="access">
				{props.user.username}
				<button type="button" onClick={props.close}>
					close-access
				</button>
			</div>
		);
	},
}));

vi.mock("@/utils/errorUtils", () => ({
	translateApiError: (_message: string, fallbackKey: string) => fallbackKey,
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-US" }),
	}),
}));

import { AccountSettings } from "./AccountSettings";

const user = (overrides: Partial<UserDto>): UserDto => ({
	id: "user-2",
	username: "jane",
	email: null,
	roles: ["User"],
	createdAt: new Date(2026, 0, 1),
	lastLoginAt: null,
	playerId: null,
	isDisabled: false,
	roleManagedByIdentityProvider: false,
	externalLoginCount: 0,
	...overrides,
});

describe("AccountSettings user management", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.users = [
			user({ id: "admin-1", username: "admin", roles: ["Admin"] }),
			user({ id: "user-2", username: "jane", isDisabled: true, externalLoginCount: 1 }),
		];
	});

	it("marks disabled users and users with single sign-on", () => {
		renderWithProviders(<AccountSettings />);

		const jane = within(screen.getByRole("table")).getByText("jane").closest("tr") as HTMLElement;
		expect(within(jane).getByText("account.users.access.disabled-badge")).toBeInTheDocument();
		expect(within(jane).getByText("account.users.access.sso-badge")).toBeInTheDocument();
		const admin = within(screen.getByRole("table"))
			.getByText("admin", { selector: "span" })
			.closest("tr") as HTMLElement;
		expect(within(admin).queryByText("account.users.access.disabled-badge")).not.toBeInTheDocument();
		expect(within(admin).queryByText("account.users.access.sso-badge")).not.toBeInTheDocument();
	});

	it("opens the access dialog for the chosen user and closes it again", async () => {
		const pointer = userEvent.setup();
		renderWithProviders(<AccountSettings />);

		const jane = within(screen.getByRole("table")).getByText("jane").closest("tr") as HTMLElement;
		fireEvent.click(within(jane).getByRole("button", { name: "account.users.access.button" }));

		expect(screen.getByRole("dialog", { name: "access" })).toHaveTextContent("jane");
		expect(mocks.accessModal).toHaveBeenLastCalledWith(
			expect.objectContaining({ user: expect.objectContaining({ id: "user-2" }), isCurrentUser: false }),
		);
		await pointer.click(screen.getByRole("button", { name: "close-access" }));
		expect(screen.queryByRole("dialog", { name: "access" })).not.toBeInTheDocument();
	});

	it("asks before resetting a password and only then calls the server", async () => {
		const pointer = userEvent.setup();
		mocks.resetPassword.mockResolvedValue({ tempPassword: "temporary-value" });
		renderWithProviders(<AccountSettings />);

		const jane = within(screen.getByRole("table")).getByText("jane").closest("tr") as HTMLElement;
		fireEvent.click(within(jane).getByRole("button", { name: "account.users.reset-password.button" }));

		expect(mocks.resetPassword).not.toHaveBeenCalled();
		const confirm = await screen.findByRole("dialog");
		expect(confirm).toHaveTextContent("account.users.reset-password.description");
		const confirmButton = within(confirm).getByRole("button", { name: "account.users.reset-password.button" });
		expect(confirmButton.className).toContain("bg-error-strong");
		await pointer.click(confirmButton);

		expect(mocks.resetPassword).toHaveBeenCalledWith("user-2");
		expect(await screen.findByRole("dialog", { name: "temp-password" })).toHaveTextContent("jane:temporary-value");
	});

	it("does nothing when the password reset is cancelled", async () => {
		const pointer = userEvent.setup();
		renderWithProviders(<AccountSettings />);

		const jane = within(screen.getByRole("table")).getByText("jane").closest("tr") as HTMLElement;
		fireEvent.click(within(jane).getByRole("button", { name: "account.users.reset-password.button" }));
		await pointer.click(screen.getByRole("button", { name: "common:cancel" }));

		expect(mocks.resetPassword).not.toHaveBeenCalled();
		expect(screen.queryByText("account.users.reset-password.description")).not.toBeInTheDocument();
	});

	it("reports a failed reset", async () => {
		const pointer = userEvent.setup();
		mocks.resetPassword.mockRejectedValue(new Error("nope"));
		renderWithProviders(<AccountSettings />);

		const jane = within(screen.getByRole("table")).getByText("jane").closest("tr") as HTMLElement;
		fireEvent.click(within(jane).getByRole("button", { name: "account.users.reset-password.button" }));
		const dialogs = screen.getAllByRole("button", { name: "account.users.reset-password.button" });
		await pointer.click(dialogs.at(-1) as HTMLElement);

		expect(mocks.errorToast).toHaveBeenCalledWith("settings:account.notifications.password-reset-failed");
		expect(screen.queryByRole("dialog", { name: "temp-password" })).not.toBeInTheDocument();
	});

	it("shows every user as a card with role, last login and a menu on phones", async () => {
		const pointer = userEvent.setup();
		renderWithProviders(<AccountSettings />);

		const cards = screen.getAllByTestId("user-card");
		expect(cards).toHaveLength(2);
		expect(cards[1]).toHaveTextContent("jane");
		expect(cards[1]).toHaveTextContent("User");
		expect(cards[1]).toHaveTextContent("account.users.never-logged-in");

		await pointer.click(within(cards[1]).getByRole("button", { name: "account.users.actions" }));
		const menu = screen.getByRole("menu");
		expect(
			within(menu)
				.getAllByRole("menuitem")
				.map((x) => x.textContent),
		).toEqual([
			"common:edit",
			"account.users.access.button",
			"account.users.reset-password.button",
			"common:delete.button",
		]);
		await pointer.click(within(menu).getByRole("menuitem", { name: "account.users.access.button" }));

		expect(screen.getByRole("dialog", { name: "access" })).toHaveTextContent("jane");
		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});

	it("leaves delete out of the menu of the signed-in admin", async () => {
		const pointer = userEvent.setup();
		renderWithProviders(<AccountSettings />);

		const admin = screen.getAllByTestId("user-card")[0];
		await pointer.click(within(admin).getByRole("button", { name: "account.users.actions" }));

		expect(
			within(screen.getByRole("menu")).queryByRole("menuitem", { name: "common:delete.button" }),
		).not.toBeInTheDocument();
	});

	it("tells the dialog when the admin opens their own access", () => {
		renderWithProviders(<AccountSettings />);

		const admin = within(screen.getByRole("table"))
			.getByText("admin", { selector: "span" })
			.closest("tr") as HTMLElement;
		fireEvent.click(within(admin).getByRole("button", { name: "account.users.access.button" }));

		expect(mocks.accessModal).toHaveBeenLastCalledWith(expect.objectContaining({ isCurrentUser: true }));
	});
});
