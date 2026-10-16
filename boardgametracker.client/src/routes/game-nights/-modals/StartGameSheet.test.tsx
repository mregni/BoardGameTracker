import { describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { StartGameSheet } from "./StartGameSheet";

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

const games = [
	{ id: 1, title: "Azul", image: null },
	{ id: 2, title: "Brass", image: null },
	{ id: 3, title: "Carcassonne", image: null },
] as unknown as Game[];
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Kathleen", image: null },
	{ id: 3, name: "Reggi", image: null },
] as unknown as Player[];

const renderSheet = (initialGameId: number | null = null) => {
	const onStart = vi.fn().mockResolvedValue(undefined);
	const close = vi.fn();
	renderWithProviders(
		<StartGameSheet
			close={close}
			games={games}
			players={players}
			nightGameIds={[3]}
			attendeeIds={[2]}
			initialGameId={initialGameId}
			initialPlayerIds={[1, 2]}
			isPending={false}
			onStart={onStart}
		/>,
	);
	return { onStart, close };
};

describe("StartGameSheet", () => {
	it("needs a game before it can start", () => {
		renderSheet();

		expect(screen.getByRole("button", { name: "play.start.save" })).toBeDisabled();
	});

	it("puts tonight's games first", async () => {
		const user = userEvent.setup();
		renderSheet();

		await user.click(screen.getByRole("combobox", { name: "play.start.game" }));

		const tonight = await screen.findByRole("group", { name: "play.games.tonight" });
		expect(
			within(tonight)
				.getAllByRole("option")
				.map((x) => x.textContent),
		).toEqual(["Carcassonne"]);
	});

	it("starts the picked game with the players who are left in", async () => {
		const user = userEvent.setup();
		const { onStart, close } = renderSheet();

		await user.click(screen.getByRole("combobox", { name: "play.start.game" }));
		await user.click(await screen.findByRole("option", { name: "Azul" }));
		await user.click(screen.getAllByRole("button", { name: "play.leave-out" })[0]);
		await user.click(screen.getByRole("button", { name: "play.start.save" }));

		await waitFor(() => expect(onStart).toHaveBeenCalledWith({ gameId: 1, playerIds: [2] }));
		expect(close).toHaveBeenCalled();
	});

	it("adds a player and starts a played game again", async () => {
		const user = userEvent.setup();
		const { onStart } = renderSheet(3);

		await user.click(screen.getByRole("combobox", { name: "play.start.players" }));
		await user.click(await screen.findByRole("option", { name: /Reggi/ }));
		await user.click(screen.getByRole("button", { name: "play.start.save" }));

		await waitFor(() => expect(onStart).toHaveBeenCalledWith({ gameId: 3, playerIds: [1, 2, 3] }));
	});
});
