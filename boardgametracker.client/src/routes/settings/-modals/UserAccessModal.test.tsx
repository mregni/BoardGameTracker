import { beforeEach, describe, expect, it, vi } from "vitest";
import type { UserDto } from "@/models";
import type { ExternalLogin } from "@/models/Auth/Oidc";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	state: {} as {
		logins: ExternalLogin[];
		isLoading: boolean;
		disable: ReturnType<typeof vi.fn>;
		enable: ReturnType<typeof vi.fn>;
		unlink: ReturnType<typeof vi.fn>;
		manageRoleByIdentityProvider: ReturnType<typeof vi.fn>;
		isBusy: boolean;
	},
}));

vi.mock("../-hooks/useUserAccess", () => ({
	useUserAccess: () => mocks.state,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-US" }),
	}),
}));

import { UserAccessModal } from "./UserAccessModal";

const user: UserDto = {
	id: "user-2",
	username: "jane",
	email: "jane@example.com",
	roles: ["User"],
	createdAt: new Date(2026, 0, 1),
	lastLoginAt: null,
	playerId: null,
	isDisabled: false,
	roleManagedByIdentityProvider: false,
	externalLoginCount: 1,
};

const login: ExternalLogin = {
	id: 9,
	provider: "keycloak",
	providerKey: "sub-1",
	providerDisplayName: "Jane Doe",
	linkedAt: new Date("2026-09-01T10:00:00Z"),
	lastUsedAt: null,
};

const renderModal = (overrides: Partial<Parameters<typeof UserAccessModal>[0]> = {}) =>
	renderWithProviders(<UserAccessModal open close={vi.fn()} user={user} isCurrentUser={false} {...overrides} />);

describe("UserAccessModal", () => {
	beforeEach(() => {
		mocks.state = {
			logins: [login],
			isLoading: false,
			disable: vi.fn(),
			enable: vi.fn(),
			unlink: vi.fn(),
			manageRoleByIdentityProvider: vi.fn(),
			isBusy: false,
		};
	});

	it("lists the linked identities and unlinks one after confirmation", async () => {
		const pointer = userEvent.setup();
		renderModal();

		expect(screen.getByText("keycloak · Jane Doe")).toBeInTheDocument();
		await pointer.click(screen.getByRole("button", { name: "account.users.access.unlink" }));
		expect(screen.getByText("account.users.access.unlink-confirm")).toBeInTheDocument();
		const confirm = screen.getByRole("dialog", { name: "account.external-logins.unlink-title" });
		expect(within(confirm).queryByRole("button", { name: "delete.button" })).not.toBeInTheDocument();
		await pointer.click(within(confirm).getByRole("button", { name: "account.users.access.unlink" }));

		expect(mocks.state.unlink).toHaveBeenCalledWith(9);
	});

	it("says so when nothing is linked and hides the role controls", () => {
		mocks.state.logins = [];
		renderModal();

		expect(screen.getByText("account.users.access.sso-none")).toBeInTheDocument();
		expect(screen.queryByText("account.users.access.role-local")).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "account.users.access.manage-role" })).not.toBeInTheDocument();
	});

	it("has no subtitle repeating the title", () => {
		renderModal();

		expect(screen.queryByText("account.users.access.description")).not.toBeInTheDocument();
	});

	it("asks before disabling an enabled user and explains what happens", async () => {
		const pointer = userEvent.setup();
		renderModal();

		expect(screen.getByText("account.users.access.enabled")).toBeInTheDocument();
		expect(screen.getByText("account.users.access.disable-hint")).toBeInTheDocument();
		await pointer.click(screen.getByRole("button", { name: "account.users.access.disable" }));

		expect(mocks.state.disable).not.toHaveBeenCalled();
		expect(screen.getByText("account.users.access.disable-confirm.title")).toBeInTheDocument();
		expect(screen.getByText("account.users.access.disable-confirm.description")).toBeInTheDocument();
		const confirm = screen.getAllByRole("dialog").at(-1) as HTMLElement;
		await pointer.click(within(confirm).getByRole("button", { name: "account.users.access.disable" }));

		expect(mocks.state.disable).toHaveBeenCalledTimes(1);
	});

	it("keeps the user enabled when the confirmation is cancelled", async () => {
		const pointer = userEvent.setup();
		renderModal();

		await pointer.click(screen.getByRole("button", { name: "account.users.access.disable" }));
		const confirm = screen.getAllByRole("dialog").at(-1) as HTMLElement;
		await pointer.click(within(confirm).getByRole("button", { name: "common:cancel" }));

		expect(mocks.state.disable).not.toHaveBeenCalled();
		expect(screen.queryByText("account.users.access.disable-confirm.title")).not.toBeInTheDocument();
	});

	it("enables a disabled user straight away and drops the disable hint", async () => {
		const pointer = userEvent.setup();
		renderModal({ user: { ...user, isDisabled: true } });

		expect(screen.getByText("account.users.access.disabled")).toBeInTheDocument();
		expect(screen.queryByText("account.users.access.disable-hint")).not.toBeInTheDocument();
		await pointer.click(screen.getByRole("button", { name: "account.users.access.enable" }));

		expect(mocks.state.enable).toHaveBeenCalledTimes(1);
	});

	it("never offers to disable the signed-in admin, nor explains it", () => {
		renderModal({ isCurrentUser: true });

		expect(screen.queryByRole("button", { name: "account.users.access.disable" })).not.toBeInTheDocument();
		expect(screen.queryByText("account.users.access.disable-hint")).not.toBeInTheDocument();
	});

	it("hands a locally set role back to the identity provider", async () => {
		const pointer = userEvent.setup();
		renderModal();

		expect(screen.getByText("account.users.access.role-local")).toBeInTheDocument();
		await pointer.click(screen.getByRole("button", { name: "account.users.access.manage-role" }));

		expect(mocks.state.manageRoleByIdentityProvider).toHaveBeenCalledTimes(1);
	});

	it("shows that the identity provider manages the role", () => {
		renderModal({ user: { ...user, roleManagedByIdentityProvider: true } });

		expect(screen.getByText("account.users.access.role-managed")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "account.users.access.manage-role" })).not.toBeInTheDocument();
	});

	it("disables every action while a change is running", () => {
		mocks.state.isBusy = true;
		renderModal();

		const dialog = screen.getByRole("dialog");
		expect(within(dialog).getByRole("button", { name: "account.users.access.disable" })).toBeDisabled();
		expect(within(dialog).getByRole("button", { name: "account.users.access.unlink" })).toBeDisabled();
	});

	it("shows nothing about the identities while they load", () => {
		mocks.state.isLoading = true;
		mocks.state.logins = [];
		renderModal();

		expect(screen.queryByText("account.users.access.sso-none")).not.toBeInTheDocument();
		expect(screen.queryByRole("list")).not.toBeInTheDocument();
	});
});
