import { describe, expect, it, vi } from "vitest";
import { type Game, type GameNight, GameNightRsvpState, type Location, type Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { EditGameNightModal } from "./EditGameNightModal";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

const players = [
	{ id: 1, name: "Host", image: null },
	{ id: 7, name: "Guest", image: null },
] as unknown as Player[];
const games = [{ id: 9, title: "Everdell", image: null }] as unknown as Game[];
const locations = [{ id: 3, name: "Living room", playCount: 0 }] as Location[];

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
		{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: players[0] },
		{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending, player: players[1] },
	],
} as unknown as GameNight;

const renderModal = (overrides: Partial<Parameters<typeof EditGameNightModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		gameNight,
		players,
		games,
		locations,
		isLoading: false,
		onSave: vi.fn(() => Promise.resolve()),
		...overrides,
	};
	renderWithProviders(<EditGameNightModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("EditGameNightModal", () => {
	it("puts cancel on the left and the submit for the game night form on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "edit.save"]);
		expect(buttons[1]).toHaveClass("bg-primary/60");
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveAttribute("form", "game-night-form");
	});

	it("closes when cancel is clicked", async () => {
		const user = userEvent.setup();
		const { close, onSave } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onSave).not.toHaveBeenCalled();
	});

	it("saves the game night when the footer submit is clicked", async () => {
		const user = userEvent.setup();
		const { onSave, close } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "edit.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		expect(onSave).toHaveBeenCalledWith(
			expect.objectContaining({
				id: 4,
				title: "Strategy night",
				hostId: 1,
				locationId: 3,
				suggestedGameIds: [9],
				invitedPlayerIds: [7],
			}),
		);
		await waitFor(() => expect(close).toHaveBeenCalled());
	});

	it("disables both footer buttons while saving", () => {
		renderModal({ isLoading: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "edit.save" })).toBeDisabled();
	});

	it("renders nothing without a game night", () => {
		renderModal({ gameNight: null });

		expect(screen.queryByText("edit.title")).not.toBeInTheDocument();
		expect(document.querySelector("[data-slot='dialog-footer']")).not.toBeInTheDocument();
	});
});
