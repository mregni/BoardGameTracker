import { addMinutes } from "date-fns";
import { describe, expect, it, vi } from "vitest";
import type { Game, GameNight, Location, Player, RunningGame, Settings } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { PlaySessionSheet } from "./PlaySessionSheet";

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
}));

const games = [
	{
		id: 1,
		title: "Spirit Island",
		image: null,
		hasScoring: true,
		hasCoopMode: true,
		coopByDefault: true,
		maxPlayTime: 120,
		expansions: [],
	},
	{
		id: 2,
		title: "Azul",
		image: null,
		hasScoring: true,
		hasCoopMode: false,
		coopByDefault: false,
		maxPlayTime: 45,
		expansions: [],
	},
] as unknown as Game[];
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Kathleen", image: null },
] as unknown as Player[];
const locations = [{ id: 5, name: "Thuis" }] as unknown as Location[];
const gameNight = { id: 4, locationId: 5, location: { id: 5, name: "Thuis" } } as unknown as GameNight;
const settings = { timeFormat: "HH:mm", dateFormat: "dd-MM-yyyy", uiLanguage: "en-us" } as Settings;

const renderSheet = (runningGame: RunningGame | null) => {
	const onSave = vi.fn().mockResolvedValue(undefined);
	renderWithProviders(
		<PlaySessionSheet
			close={vi.fn()}
			gameNight={gameNight}
			runningGame={runningGame}
			games={games}
			players={players}
			locations={locations}
			nightGameIds={[1]}
			attendeeIds={[1, 2]}
			settings={settings}
			isPending={false}
			onSave={onSave}
		/>,
	);
	return onSave;
};

const coopSwitch = () => screen.queryByRole("switch", { name: "coop.played" });

describe("PlaySessionSheet co-op", () => {
	it("finishes a running co-op game with one team result for the whole table", async () => {
		const user = userEvent.setup();
		const onSave = renderSheet({
			id: 12,
			gameNightId: 4,
			gameId: 1,
			start: addMinutes(new Date(), -50),
			playerIds: [1, 2],
		} as RunningGame);

		expect(coopSwitch()).toBeChecked();
		expect(screen.queryAllByRole("button", { name: "form.player.won" })).toHaveLength(0);
		await user.click(screen.getByRole("button", { name: "coop.lost" }));
		await user.click(screen.getByRole("button", { name: "play.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		expect(onSave.mock.calls[0][0]).toMatchObject({
			isCoop: true,
			teamWon: false,
			teamScore: null,
			playerSessions: [
				{ playerId: 1, won: false, score: null },
				{ playerId: 2, won: false, score: null },
			],
		});
	});

	it("logs a finished game as co-op only when the picked game has co-op mode", async () => {
		const user = userEvent.setup();
		renderSheet(null);

		expect(coopSwitch()).not.toBeInTheDocument();

		await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
		await user.click(await screen.findByRole("option", { name: "Spirit Island" }));
		expect(coopSwitch()).toBeChecked();

		await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
		await user.click(await screen.findByRole("option", { name: "Azul" }));
		expect(coopSwitch()).not.toBeInTheDocument();
		expect(screen.getAllByRole("button", { name: "form.player.won" })).toHaveLength(2);
	});
});
