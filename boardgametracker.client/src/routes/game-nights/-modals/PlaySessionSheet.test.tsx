import { addMinutes } from "date-fns";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, GameNight, Location, Player, RunningGame, Settings } from "@/models";
import { fireEvent, renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { PlaySessionSheet } from "./PlaySessionSheet";

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
}));

const games = [
	{ id: 1, title: "Azul", image: null, hasScoring: true, maxPlayTime: 45, expansions: [] },
	{ id: 2, title: "Brass", image: null, hasScoring: true, maxPlayTime: 120, expansions: [] },
] as unknown as Game[];
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Kathleen", image: null },
] as unknown as Player[];
const locations = [{ id: 5, name: "Thuis" }] as unknown as Location[];
const gameNight = { id: 4, locationId: 5, location: { id: 5, name: "Thuis" } } as unknown as GameNight;
const settings = { timeFormat: "HH:mm", dateFormat: "dd-MM-yyyy", uiLanguage: "en-us" } as Settings;

const startTime = () => document.querySelector<HTMLInputElement>('input[type="time"]')?.value;

const renderSheet = (runningGame: RunningGame | null) => {
	const onSave = vi.fn().mockResolvedValue(undefined);
	const close = vi.fn();
	renderWithProviders(
		<PlaySessionSheet
			close={close}
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
	return { onSave, close };
};

const runningGame = {
	id: 12,
	gameNightId: 4,
	gameId: 1,
	start: addMinutes(new Date(), -37),
	playerIds: [1, 2],
} as RunningGame;

describe("PlaySessionSheet", () => {
	it("finishes a running game with the scores, the winner from the highest score and the times since the start", async () => {
		const user = userEvent.setup();
		const { onSave, close } = renderSheet(runningGame);

		expect(screen.getByText("play.finish.title")).toBeInTheDocument();
		const [first, second] = screen.getAllByRole("spinbutton", { name: "form.player.score" });
		await user.type(first, "79");
		await user.type(second, "88");
		await user.click(screen.getByRole("button", { name: "play.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		const [session] = onSave.mock.calls[0];
		expect(session.start).toEqual(runningGame.start);
		expect(session).toMatchObject({
			gameId: 1,
			locationId: 5,
			gameNightId: 4,
			playerSessions: [
				{ playerId: 1, score: 79, won: false },
				{ playerId: 2, score: 88, won: true },
			],
		});
		expect(session.minutes).toBeGreaterThanOrEqual(36);
		expect(close).toHaveBeenCalled();
	});

	it("always shows the times and sends a changed duration", async () => {
		const user = userEvent.setup();
		const { onSave } = renderSheet(runningGame);

		expect(screen.queryByRole("button", { name: "play.finish.change" })).not.toBeInTheDocument();
		const duration = screen.getByRole("spinbutton", { name: "player-session:new.duration.label" });
		await user.clear(duration);
		await user.type(duration, "50");
		await user.click(screen.getByRole("button", { name: "play.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		expect(onSave.mock.calls[0][0].minutes).toBe(50);
	});

	describe("logging a finished game", () => {
		beforeEach(() => {
			vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ["Date"] });
			vi.setSystemTime(new Date(2026, 9, 16, 22, 30, 45));
		});

		afterEach(() => {
			vi.useRealTimers();
		});

		it("logs a finished game that ends now with the play time of the picked game", async () => {
			const user = userEvent.setup();
			const { onSave } = renderSheet(null);

			expect(screen.getByText("play.log.title")).toBeInTheDocument();
			expect(startTime()).toBe("22:00");
			await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
			await user.click(await screen.findByRole("option", { name: "Brass" }));
			expect(screen.getByRole("spinbutton", { name: "new.duration.label" })).toHaveValue(120);
			expect(startTime()).toBe("20:30");
			await user.click(screen.getAllByRole("button", { name: "form.player.won" })[0]);
			await user.click(screen.getByRole("button", { name: "play.save" }));

			await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
			expect(onSave.mock.calls[0][0]).toMatchObject({ gameId: 2, minutes: 120, gameNightId: 4, locationId: 5 });
			expect(onSave.mock.calls[0][0].start).toEqual(new Date(2026, 9, 16, 20, 30));
		});

		it("moves the start when the duration is typed", async () => {
			const user = userEvent.setup();
			renderSheet(null);

			const duration = screen.getByRole("spinbutton", { name: "new.duration.label" });
			await user.clear(duration);
			await user.type(duration, "50");

			expect(startTime()).toBe("21:40");
		});

		it("keeps a start that was set by hand", async () => {
			const user = userEvent.setup();
			renderSheet(null);

			fireEvent.change(document.querySelector('input[type="time"]') as HTMLInputElement, {
				target: { value: "07:00" },
			});
			await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
			await user.click(await screen.findByRole("option", { name: "Brass" }));

			expect(startTime()).toBe("07:00");
		});
	});

	it("closes without saving", async () => {
		const user = userEvent.setup();
		const { onSave, close } = renderSheet(null);

		await user.click(screen.getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalled();
		expect(onSave).not.toHaveBeenCalled();
	});
});
