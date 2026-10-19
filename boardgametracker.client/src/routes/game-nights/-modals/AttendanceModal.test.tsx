import { describe, expect, it, vi } from "vitest";
import { type GameNightDetail, GameNightRsvpState, type Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { AttendanceModal } from "./AttendanceModal";

const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Kathleen", image: null },
	{ id: 3, name: "Bram", image: null },
	{ id: 4, name: "Zoe", image: null },
	{ id: 5, name: "Anna", image: null },
] as unknown as Player[];

const detail = {
	gameNight: {
		id: 4,
		hostId: 1,
		invitedPlayers: [
			{ id: 11, playerId: 1, state: GameNightRsvpState.Accepted, player: players[0] },
			{ id: 12, playerId: 2, state: GameNightRsvpState.Accepted, player: players[1] },
			{ id: 13, playerId: 4, state: GameNightRsvpState.Declined, player: players[3] },
		],
		summary: { attendeeIds: [1, 3], attendanceRecorded: false },
	},
	sessions: [{ playerSessions: [{ playerId: 3 }] }],
	runningGames: [{ gameId: 8, playerIds: [5, 3] }],
} as unknown as GameNightDetail;

const renderModal = () => {
	const onSave = vi.fn().mockResolvedValue(undefined);
	const close = vi.fn();
	renderWithProviders(
		<AttendanceModal close={close} detail={detail} players={players} isPending={false} onSave={onSave} />,
	);
	return { onSave, close };
};

describe("AttendanceModal", () => {
	it("starts from who is at the table, with the ones who played locked in", () => {
		renderModal();

		const bram = screen.getByRole("button", { name: /Bram/ });
		expect(bram).toHaveAttribute("aria-pressed", "true");
		expect(bram).toBeDisabled();
		expect(bram).toHaveTextContent("attendance.played");
		expect(screen.getByRole("button", { name: /Mikhael/ })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: /Kathleen/ })).toHaveAttribute("aria-pressed", "false");
	});

	it("says playing now for someone who is only in a running game", () => {
		renderModal();

		const anna = screen.getByRole("button", { name: /Anna/ });
		expect(anna).toHaveTextContent("attendance.playing");
		expect(anna).toBeDisabled();
		expect(screen.getByRole("button", { name: /Bram/ })).toHaveTextContent("attendance.played");
	});

	it("lists who is here first, then the invited and then everyone else", () => {
		renderModal();

		const names = screen.getAllByRole("button", { pressed: undefined }).map((x) => x.textContent ?? "");
		const order = ["Anna", "Bram", "Mikhael", "Kathleen", "Zoe"].map((name) =>
			names.findIndex((x) => x.includes(name)),
		);
		expect(order).toEqual([...order].sort((a, b) => a - b));
	});

	it("saves the players marked present and closes", async () => {
		const user = userEvent.setup();
		const { onSave, close } = renderModal();

		await user.click(screen.getByRole("button", { name: /Kathleen/ }));
		await user.click(screen.getByRole("button", { name: /Mikhael/ }));
		await user.click(screen.getByRole("button", { name: "attendance.save" }));

		expect(onSave).toHaveBeenCalledWith(expect.arrayContaining([2, 3]));
		expect(onSave.mock.calls[0][0]).not.toContain(1);
		await waitFor(() => expect(close).toHaveBeenCalled());
	});
});
