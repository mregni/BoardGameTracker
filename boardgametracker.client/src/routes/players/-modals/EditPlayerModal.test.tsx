import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import type { Player, UserDto } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { EditPlayerModal } from "./EditPlayerModal";

const mocks = vi.hoisted(() => ({
	updatePlayer: vi.fn(),
	uploadImage: vi.fn(),
	linkAccount: vi.fn(),
	isLoading: false,
	options: {
		authEnabled: false,
		canManage: false,
		users: [] as UserDto[],
		players: [] as Player[],
	},
}));

vi.mock("../-hooks/usePlayerModal", () => ({
	usePlayerModal: () => ({
		updatePlayer: mocks.updatePlayer,
		uploadImage: mocks.uploadImage,
		linkAccount: mocks.linkAccount,
		isLoading: mocks.isLoading,
	}),
}));

vi.mock("../-hooks/useLinkedAccountOptions", () => ({
	useLinkedAccountOptions: () => mocks.options,
}));

const account = (
	id: string,
	username: string,
	playerId: number | null,
	displayName: string | null = null,
): UserDto => ({
	id,
	username,
	email: null,
	displayName,
	roles: ["User"],
	createdAt: new Date("2026-01-01T00:00:00Z"),
	lastLoginAt: null,
	playerId,
	isDisabled: false,
	roleManagedByIdentityProvider: false,
	externalLoginCount: 0,
	profileImage: null,
});

const asAdmin = () => {
	mocks.options = {
		authEnabled: true,
		canManage: true,
		users: [account("u-jane", "jane", null, "Jane Doe"), account("u-bob", "bob", 9)],
		players: [{ id: 9, name: "Bobby", image: null, email: null, badges: [] }],
	};
};

const asMember = () => {
	mocks.options = { authEnabled: true, canManage: false, users: [], players: [] };
};

const player = { id: 5, name: "Kathleen", image: null, email: "kathleen@example.com", badges: [] } as Player;

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("EditPlayerModal", () => {
	beforeAll(() => {
		Element.prototype.hasPointerCapture = vi.fn(() => false);
		Element.prototype.releasePointerCapture = vi.fn();
		Element.prototype.scrollIntoView = vi.fn();
	});

	beforeEach(() => {
		vi.clearAllMocks();
		mocks.isLoading = false;
		mocks.options = { authEnabled: false, canManage: false, users: [], players: [] };
	});

	it("hides the linked account when sign-in is off", () => {
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		expect(screen.queryByText("account.label")).not.toBeInTheDocument();
	});

	it("shows the linked account read-only to members", () => {
		asMember();
		const linked = {
			...player,
			linkedAccount: { id: "u-jane", username: "jane", displayName: "Jane Doe", profileImage: null },
		};
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={linked} />);

		expect(screen.getByTestId("linked-account")).toHaveTextContent("jane (Jane Doe)");
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("shows nothing to members when the player is not linked", () => {
		asMember();
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		expect(screen.queryByText("account.label")).not.toBeInTheDocument();
	});

	it("lets admins pick an account with not linked first", async () => {
		asAdmin();
		const user = userEvent.setup();
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		await user.click(screen.getByRole("combobox", { name: "account.label" }));

		const options = await screen.findAllByRole("option");
		expect(options).toHaveLength(3);
		expect(options[0]).toHaveTextContent("account.none");
		expect(options[1]).toHaveTextContent("jane (Jane Doe)");
		expect(options[2]).toHaveTextContent("bob");
		expect(options[0].textContent?.trim()).toBe("account.none");
	});

	it("warns that choosing an account linked elsewhere moves the link and saves it", async () => {
		asAdmin();
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.updatePlayer.mockResolvedValue(undefined);
		mocks.linkAccount.mockResolvedValue(undefined);
		renderWithTheme(<EditPlayerModal open close={close} player={player} />);

		expect(screen.queryByTestId("linked-account-move")).not.toBeInTheDocument();
		await user.click(screen.getByRole("combobox", { name: "account.label" }));
		await user.click(await screen.findByRole("option", { name: /bob/ }));

		expect(screen.getByTestId("linked-account-move")).toHaveTextContent("account.moves");
		await user.click(within(footer()).getByRole("button", { name: "update.save" }));

		await waitFor(() => expect(mocks.linkAccount).toHaveBeenCalledWith({ playerId: 5, userId: "u-bob" }));
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("unlinks the account when admins choose not linked", async () => {
		asAdmin();
		const user = userEvent.setup();
		mocks.updatePlayer.mockResolvedValue(undefined);
		mocks.linkAccount.mockResolvedValue(undefined);
		const linked = {
			...player,
			linkedAccount: { id: "u-jane", username: "jane", displayName: null, profileImage: null },
		};
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={linked} />);

		await user.click(screen.getByRole("combobox", { name: "account.label" }));
		await user.click(await screen.findByRole("option", { name: /account\.none/ }));
		await user.click(within(footer()).getByRole("button", { name: "update.save" }));

		await waitFor(() => expect(mocks.linkAccount).toHaveBeenCalledWith({ playerId: 5, userId: null }));
	});

	it("does not touch the link when admins keep the same account", async () => {
		asAdmin();
		const user = userEvent.setup();
		mocks.updatePlayer.mockResolvedValue(undefined);
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		await user.click(within(footer()).getByRole("button", { name: "update.save" }));

		await waitFor(() => expect(mocks.updatePlayer).toHaveBeenCalled());
		expect(mocks.linkAccount).not.toHaveBeenCalled();
	});

	it("explains that the picture comes from the linked account", () => {
		const linked: Player = {
			...player,
			image: "/images/profile/player_copy.webp",
			imageSource: "account",
			linkedAccount: { id: "u-jane", username: "jane", displayName: null, profileImage: "/images/profile/a.webp" },
		};
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={linked} />);

		expect(screen.getByTestId("player-picture-hint")).toHaveTextContent("account.uses-picture");
	});

	it("explains how to get the account picture back when the player has its own", () => {
		const linked: Player = {
			...player,
			image: "/images/profile/own.webp",
			imageSource: "upload",
			linkedAccount: { id: "u-jane", username: "jane", displayName: null, profileImage: "/images/profile/a.webp" },
		};
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={linked} />);

		expect(screen.getByTestId("player-picture-hint")).toHaveTextContent("account.own-picture");
	});

	it("shows no picture hint without a linked account", () => {
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={{ ...player, imageSource: "upload" }} />);

		expect(screen.queryByTestId("player-picture-hint")).not.toBeInTheDocument();
	});

	it("puts cancel on the left and the save submit on the right", () => {
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "update.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("has a title without a filler subtitle", () => {
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		expect(screen.getByText("update.title")).toBeInTheDocument();
		expect(screen.queryByText("update.description")).not.toBeInTheDocument();
	});

	it("closes from cancel without saving", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		renderWithTheme(<EditPlayerModal open close={close} player={player} />);

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(mocks.updatePlayer).not.toHaveBeenCalled();
	});

	it("saves the changed player from the footer submit and closes", async () => {
		const user = userEvent.setup();
		const close = vi.fn();
		mocks.updatePlayer.mockResolvedValue(undefined);
		renderWithTheme(<EditPlayerModal open close={close} player={player} />);

		const name = screen.getByLabelText("common:name");
		expect(name).toHaveValue("Kathleen");
		await user.clear(name);
		await user.type(name, "Kat");
		await user.click(within(footer()).getByRole("button", { name: "update.save" }));

		await waitFor(() =>
			expect(mocks.updatePlayer).toHaveBeenCalledWith(
				expect.objectContaining({ id: 5, name: "Kat", email: "kathleen@example.com" }),
			),
		);
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("disables both footer buttons while saving", () => {
		mocks.isLoading = true;
		renderWithTheme(<EditPlayerModal open close={vi.fn()} player={player} />);

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "update.save" })).toBeDisabled();
	});
});
