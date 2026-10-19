import { beforeEach, describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, type Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";

const { toastError, toastSuccess, sendInvitesCall, settingsMock, permissions } = vi.hoisted(() => ({
	toastError: vi.fn(),
	toastSuccess: vi.fn(),
	sendInvitesCall: vi.fn(),
	settingsMock: { current: { publicUrl: "https://games.example.com", emailEnabled: false } },
	permissions: { isAdmin: true },
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: permissions.isAdmin, canWrite: true }),
}));

vi.mock("sonner", () => ({
	toast: { success: toastSuccess, error: toastError },
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: settingsMock.current }),
}));

vi.mock("@/services/gameNightService", () => ({
	sendInvitesCall,
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

import { GameNightActions } from "./GameNightActions";

const gameNight = { id: 4, linkId: "abc-123" } as GameNight;

const invitees = [
	{ id: 1, name: "Host", email: "host@example.com" },
	{ id: 2, name: "Anna", email: "anna@example.com" },
	{ id: 3, name: "Bob", email: "" },
	{ id: 4, name: "Cleo", email: "cleo@example.com" },
] as unknown as Player[];

const invitingNight = {
	id: 4,
	linkId: "abc-123",
	title: "Strategy night",
	hostId: 1,
	invitedPlayers: [
		{ id: 11, playerId: 1, state: GameNightRsvpState.Accepted, player: invitees[0] },
		{ id: 12, playerId: 2, state: GameNightRsvpState.Pending, player: invitees[1] },
		{ id: 13, playerId: 3, state: GameNightRsvpState.Pending, player: invitees[2] },
		{ id: 14, playerId: 4, state: GameNightRsvpState.Maybe, player: invitees[3] },
	],
} as unknown as GameNight;

const setClipboard = (clipboard: Partial<Clipboard> | undefined) => {
	Object.defineProperty(navigator, "clipboard", { value: clipboard, configurable: true });
};

describe("GameNightActions", () => {
	beforeEach(() => {
		toastError.mockClear();
		toastSuccess.mockClear();
		sendInvitesCall.mockReset();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: false };
		permissions.isAdmin = true;
	});

	it("leaves the email invites button out for a non-admin while mailing is not set up", () => {
		permissions.isAdmin = false;
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		expect(screen.queryByRole("button", { name: "card.email-invites" })).not.toBeInTheDocument();
		expect(screen.queryByText("invites.email-disabled")).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "card.copy-link" })).toBeInTheDocument();
	});

	it("copies the invite link and says so", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		setClipboard({ writeText });
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		await user.click(screen.getByRole("button", { name: "card.copy-link" }));

		expect(writeText).toHaveBeenCalledWith("https://games.example.com/rsvp?linkId=abc-123");
		expect(await screen.findByRole("button", { name: "card.copied" })).toBeInTheDocument();
		expect(toastError).not.toHaveBeenCalled();
	});

	it("reports a failed copy instead of claiming success when the clipboard is unavailable", async () => {
		const user = userEvent.setup();
		setClipboard(undefined);
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		await user.click(screen.getByRole("button", { name: "card.copy-link" }));

		await waitFor(() => expect(toastError).toHaveBeenCalledWith("card.copy-failed"));
		expect(screen.getByRole("button", { name: "card.copy-link" })).toBeInTheDocument();
	});

	it("leaves the email invites button out for an admin too while email is not set up, as the list explains it once", () => {
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		expect(screen.queryByRole("button", { name: "card.email-invites" })).not.toBeInTheDocument();
		expect(screen.queryByText("invites.email-disabled")).not.toBeInTheDocument();
	});

	it("leaves the email invites button out while no public URL is set", () => {
		settingsMock.current = { publicUrl: "", emailEnabled: true };
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		expect(screen.queryByRole("button", { name: "card.email-invites" })).not.toBeInTheDocument();
	});

	it("offers no invite actions once the night has started", () => {
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		renderWithProviders(
			<GameNightActions invitable={false} gameNight={gameNight} players={[]} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		expect(screen.queryByRole("button", { name: "card.copy-link" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "card.email-invites" })).not.toBeInTheDocument();
	});

	it("asks before mailing, with who gets it and who has no email, and only sends on confirm", async () => {
		const user = userEvent.setup();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		sendInvitesCall.mockResolvedValue({ sent: 1 });
		renderWithProviders(
			<GameNightActions invitable gameNight={invitingNight} players={invitees} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		await user.click(screen.getByRole("button", { name: "card.email-invites" }));

		const dialog = await screen.findByRole("dialog");
		expect(within(dialog).getByText("invites.title")).toBeInTheDocument();
		expect(within(dialog).getByText("invites.description")).toBeInTheDocument();
		expect(within(dialog).getByTestId("invite-recipients")).toHaveTextContent(/^Anna$/);
		expect(within(dialog).getByTestId("invite-without-email")).toHaveTextContent("invites.without-email");
		expect(sendInvitesCall).not.toHaveBeenCalled();

		await user.click(within(dialog).getByRole("button", { name: "invites.send" }));

		await waitFor(() => expect(sendInvitesCall).toHaveBeenCalledWith(4));
		await waitFor(() => expect(toastSuccess).toHaveBeenCalledWith("card.invites-sent"));
		await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
	});

	it("sends nothing when the confirmation is cancelled", async () => {
		const user = userEvent.setup();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		renderWithProviders(
			<GameNightActions invitable gameNight={invitingNight} players={invitees} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		await user.click(screen.getByRole("button", { name: "card.email-invites" }));
		await user.click(within(await screen.findByRole("dialog")).getByRole("button", { name: "common:cancel" }));

		await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
		expect(sendInvitesCall).not.toHaveBeenCalled();
	});

	it("can't confirm when nobody without an answer has an email address", async () => {
		const user = userEvent.setup();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		renderWithProviders(
			<GameNightActions
				invitable
				gameNight={invitingNight}
				players={invitees.map((x) => ({ ...x, email: null }))}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "card.email-invites" }));

		const dialog = await screen.findByRole("dialog");
		expect(within(dialog).getByText("invites.nobody")).toBeInTheDocument();
		expect(within(dialog).getByRole("button", { name: "invites.send" })).toBeDisabled();
	});

	it("reports a failed send", async () => {
		const user = userEvent.setup();
		settingsMock.current = { publicUrl: "https://games.example.com", emailEnabled: true };
		sendInvitesCall.mockRejectedValue(new Error("boom"));
		renderWithProviders(
			<GameNightActions invitable gameNight={invitingNight} players={invitees} onEdit={vi.fn()} onDelete={vi.fn()} />,
		);

		await user.click(screen.getByRole("button", { name: "card.email-invites" }));
		await user.click(within(await screen.findByRole("dialog")).getByRole("button", { name: "invites.send" }));

		await waitFor(() => expect(toastError).toHaveBeenCalledWith("game-nights:card.invites-failed"));
	});

	it("edits and deletes the game night", async () => {
		const user = userEvent.setup();
		const onEdit = vi.fn();
		const onDelete = vi.fn();
		renderWithProviders(
			<GameNightActions invitable gameNight={gameNight} players={[]} onEdit={onEdit} onDelete={onDelete} />,
		);

		await user.click(screen.getByRole("button", { name: "edit" }));
		await user.click(screen.getByRole("button", { name: "delete.button" }));

		expect(onEdit).toHaveBeenCalledWith(gameNight);
		expect(onDelete).toHaveBeenCalledWith(gameNight);
	});
});
