import { beforeEach, describe, expect, it, vi } from "vitest";
import type { UserDto } from "@/models";
import { fireEvent, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	users: [] as UserDto[],
	accessModal: vi.fn(),
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
		resetPassword: vi.fn(),
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

		const jane = screen.getByText("jane").closest("tr") as HTMLElement;
		expect(within(jane).getByText("account.users.access.disabled-badge")).toBeInTheDocument();
		expect(within(jane).getByText("account.users.access.sso-badge")).toBeInTheDocument();
		const admin = screen.getByText("admin", { selector: "span" }).closest("tr") as HTMLElement;
		expect(within(admin).queryByText("account.users.access.disabled-badge")).not.toBeInTheDocument();
		expect(within(admin).queryByText("account.users.access.sso-badge")).not.toBeInTheDocument();
	});

	it("opens the access dialog for the chosen user and closes it again", async () => {
		const pointer = userEvent.setup();
		renderWithProviders(<AccountSettings />);

		const jane = screen.getByText("jane").closest("tr") as HTMLElement;
		fireEvent.click(within(jane).getByRole("button", { name: "account.users.access.button" }));

		expect(screen.getByRole("dialog", { name: "access" })).toHaveTextContent("jane");
		expect(mocks.accessModal).toHaveBeenLastCalledWith(
			expect.objectContaining({ user: expect.objectContaining({ id: "user-2" }), isCurrentUser: false }),
		);
		await pointer.click(screen.getByRole("button", { name: "close-access" }));
		expect(screen.queryByRole("dialog", { name: "access" })).not.toBeInTheDocument();
	});

	it("tells the dialog when the admin opens their own access", () => {
		renderWithProviders(<AccountSettings />);

		const admin = screen.getByText("admin", { selector: "span" }).closest("tr") as HTMLElement;
		fireEvent.click(within(admin).getByRole("button", { name: "account.users.access.button" }));

		expect(mocks.accessModal).toHaveBeenLastCalledWith(expect.objectContaining({ isCurrentUser: true }));
	});
});
