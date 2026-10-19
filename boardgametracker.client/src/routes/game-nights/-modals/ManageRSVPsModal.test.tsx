import { describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: { publicUrl: "https://games.example.com", emailEnabled: false } }),
}));

import { ManageRSVPsModal } from "./ManageRSVPsModal";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const settings = { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-us" } as Settings;

const gameNight = {
	id: 4,
	title: "Strategy night",
	notes: "Bring snacks",
	startDate: new Date(2026, 9, 11, 19, 30),
	hostId: 1,
	locationId: 3,
	linkId: "abc-123",
	suggestedGames: [{ id: 9 }],
	invitedPlayers: [
		{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: player(1, "Host") },
		{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending, player: player(7, "Guest") },
		{ id: 13, playerId: 8, gameNightId: 4, state: GameNightRsvpState.Declined, player: player(8, "Other") },
	],
} as unknown as GameNight;

const players = [player(1, "Host"), player(7, "Guest"), player(8, "Other"), player(9, "Newbie"), player(10, "Zed")];

const renderModal = (overrides: Partial<Parameters<typeof ManageRSVPsModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		gameNight,
		players,
		settings,
		onUpdateRsvp: vi.fn(),
		onUpdateGameNight: vi.fn(),
		isLoading: false,
		...overrides,
	};
	renderWithProviders(<ManageRSVPsModal {...props} />);
	return props;
};

describe("ManageRSVPsModal", () => {
	it("sends the invited player's id when the RSVP changes", async () => {
		const user = userEvent.setup();
		const { onUpdateRsvp } = renderModal();

		const guestRow = screen.getByTestId("rsvp-row-7");
		await user.click(within(guestRow).getByRole("button", { name: "rsvp.coming" }));

		await waitFor(() =>
			expect(onUpdateRsvp).toHaveBeenCalledWith({
				id: 12,
				gameNightId: 4,
				playerId: 7,
				state: GameNightRsvpState.Accepted,
			}),
		);
	});

	it("does not resend the state that is already selected", async () => {
		const user = userEvent.setup();
		const { onUpdateRsvp } = renderModal();

		const guestRow = screen.getByTestId("rsvp-row-7");
		await user.click(within(guestRow).getByRole("button", { name: "rsvp.no-answer" }));

		expect(onUpdateRsvp).not.toHaveBeenCalled();
	});

	it("marks the current state of every guest with aria-pressed", () => {
		renderModal();

		const guestGroup = within(screen.getByTestId("rsvp-row-7")).getByRole("group", { name: "rsvp.player-status" });
		expect(within(guestGroup).getByRole("button", { name: "rsvp.no-answer" })).toHaveAttribute("aria-pressed", "true");
		expect(within(guestGroup).getByRole("button", { name: "rsvp.coming" })).toHaveAttribute("aria-pressed", "false");
		expect(within(guestGroup).getByRole("button", { name: "rsvp.not-coming" })).toHaveAttribute(
			"aria-pressed",
			"false",
		);

		const otherGroup = within(screen.getByTestId("rsvp-row-8")).getByRole("group");
		expect(within(otherGroup).getByRole("button", { name: "rsvp.not-coming" })).toHaveAttribute("aria-pressed", "true");
	});

	it("offers maybe and no answer as separate states, in the same order as the counters", () => {
		renderModal();

		const guestGroup = within(screen.getByTestId("rsvp-row-7")).getByRole("group", { name: "rsvp.player-status" });
		expect(
			within(guestGroup)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual(["rsvp.coming", "rsvp.maybe", "rsvp.no-answer", "rsvp.not-coming"]);
		expect(screen.getAllByRole("button", { name: "rsvp.maybe" })).toHaveLength(2);
	});

	it("saves a maybe answer as maybe", async () => {
		const user = userEvent.setup();
		const { onUpdateRsvp } = renderModal();

		await user.click(within(screen.getByTestId("rsvp-row-7")).getByRole("button", { name: "rsvp.maybe" }));

		await waitFor(() =>
			expect(onUpdateRsvp).toHaveBeenCalledWith({
				id: 12,
				gameNightId: 4,
				playerId: 7,
				state: GameNightRsvpState.Maybe,
			}),
		);
	});

	it("shows the host first without a switch or a remove button", () => {
		renderModal();

		const rows = screen.getAllByRole("listitem");
		expect(rows[0]).toBe(screen.getByTestId("rsvp-row-1"));

		const hostRow = screen.getByTestId("rsvp-row-1");
		expect(within(hostRow).queryByRole("group")).not.toBeInTheDocument();
		expect(within(hostRow).queryByRole("button")).not.toBeInTheDocument();
		expect(within(hostRow).getByText("rsvp.coming")).toBeInTheDocument();
		expect(within(hostRow).getByText("rsvp.host")).toBeInTheDocument();
	});

	it("shows the totals as a summary", () => {
		renderModal();

		expect(screen.getByTestId("rsvp-summary")).toHaveTextContent(
			"rsvp.summary.coming · rsvp.summary.pending · rsvp.summary.declined",
		);
		expect(screen.getByText("rsvp.invited")).toBeInTheDocument();
	});

	it("removes a guest by saving the game night without them", async () => {
		const user = userEvent.setup();
		const { onUpdateGameNight } = renderModal();

		await user.click(within(screen.getByTestId("rsvp-row-7")).getByRole("button", { name: "rsvp.remove" }));

		await waitFor(() =>
			expect(onUpdateGameNight).toHaveBeenCalledWith({
				id: 4,
				title: "Strategy night",
				notes: "Bring snacks",
				startDate: new Date(2026, 9, 11, 19, 30),
				hostId: 1,
				locationId: 3,
				suggestedGameIds: [9],
				invitedPlayerIds: [8],
			}),
		);
	});

	it("invites a player found through the search", async () => {
		const user = userEvent.setup();
		const { onUpdateGameNight } = renderModal();

		expect(screen.getAllByRole("button", { name: "rsvp.invite" })).toHaveLength(2);
		await user.type(screen.getByRole("textbox", { name: "rsvp.invite-more" }), "new");

		const suggestions = screen.getAllByRole("button", { name: "rsvp.invite" });
		expect(suggestions).toHaveLength(1);
		await user.click(suggestions[0]);

		await waitFor(() =>
			expect(onUpdateGameNight).toHaveBeenCalledWith(expect.objectContaining({ invitedPlayerIds: [7, 8, 9] })),
		);
	});

	it("shows the invite link and copies it", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
		renderModal();

		expect(screen.getByText("https://games.example.com/rsvp?linkId=abc-123")).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "rsvp.copy" }));

		expect(writeText).toHaveBeenCalledWith("https://games.example.com/rsvp?linkId=abc-123");
		expect(await screen.findByRole("button", { name: "card.copied" })).toBeInTheDocument();
	});

	it("has done as the single footer button and closes with it", async () => {
		const user = userEvent.setup();
		const { close } = renderModal();

		const footer = document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
		const buttons = within(footer).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:done"]);
		expect(buttons[0]).toHaveClass("bg-primary/60");

		await user.click(buttons[0]);

		expect(close).toHaveBeenCalledTimes(1);
	});
});
