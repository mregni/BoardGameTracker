import { describe, expect, it, vi } from "vitest";
import type { Player } from "@/models";
import { act, fireEvent, renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { CreateSessionPlayerModal } from "./CreateSessionPlayerModal";

const createdPlayer = { id: 7, name: "Nova", image: null } as unknown as Player;

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: ({ open, onPlayerCreated }: { open: boolean; onPlayerCreated?: (player: Player) => void }) =>
		open ? (
			<button type="button" onClick={() => onPlayerCreated?.(createdPlayer)}>
				save-new-player
			</button>
		) : null,
}));

const existingPlayers = [{ id: 1, name: "Kathleen", image: null }] as unknown as Player[];

describe("CreateSessionPlayerModal", () => {
	it("selects a just-created player even when the player list refreshed first", async () => {
		const user = userEvent.setup();
		const props = {
			open: true,
			hasScoring: false,
			onClose: vi.fn(),
			onCancel: vi.fn(),
			selectedPlayerIds: [],
			players: existingPlayers,
		};
		const { rerender } = renderWithTheme(<CreateSessionPlayerModal {...props} />);

		await user.click(screen.getByRole("button", { name: /new\.create-player/ }));
		rerender(<CreateSessionPlayerModal {...props} players={[...existingPlayers, createdPlayer]} />);
		act(() => fireEvent.click(screen.getByText("save-new-player")));

		expect(await screen.findByText("Nova")).toBeInTheDocument();
	});
});
