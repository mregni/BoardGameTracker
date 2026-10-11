import { describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, type Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { ManageRSVPsModal } from "./ManageRSVPsModal";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const gameNight = {
	id: 4,
	hostId: 1,
	invitedPlayers: [
		{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: player(1, "Host") },
		{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending, player: player(7, "Guest") },
	],
} as unknown as GameNight;

describe("ManageRSVPsModal", () => {
	it("sends the invited player's id when the RSVP changes", async () => {
		const user = userEvent.setup();
		const onUpdateRsvp = vi.fn().mockResolvedValue(undefined);
		renderWithProviders(
			<ManageRSVPsModal open close={vi.fn()} gameNight={gameNight} onUpdateRsvp={onUpdateRsvp} isLoading={false} />,
		);

		await user.click(screen.getByRole("button", { name: "rsvp.accept" }));

		await waitFor(() =>
			expect(onUpdateRsvp).toHaveBeenCalledWith({
				id: 12,
				gameNightId: 4,
				playerId: 7,
				state: GameNightRsvpState.Accepted,
			}),
		);
	});

	it("does not throw when the update fails", async () => {
		const user = userEvent.setup();
		const onUpdateRsvp = vi.fn().mockRejectedValue(new Error("400"));
		renderWithProviders(
			<ManageRSVPsModal open close={vi.fn()} gameNight={gameNight} onUpdateRsvp={onUpdateRsvp} isLoading={false} />,
		);

		await user.click(screen.getByRole("button", { name: "rsvp.decline" }));

		await waitFor(() => expect(onUpdateRsvp).toHaveBeenCalledTimes(1));
	});

	it("labels the middle state the same as the counters", () => {
		renderWithProviders(
			<ManageRSVPsModal open close={vi.fn()} gameNight={gameNight} onUpdateRsvp={vi.fn()} isLoading={false} />,
		);

		expect(screen.getByRole("button", { name: "common:pending" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "rsvp.maybe" })).not.toBeInTheDocument();
	});
});
