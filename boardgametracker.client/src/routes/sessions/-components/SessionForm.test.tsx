import { addMinutes } from "date-fns";
import { StrictMode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import { renderWithTheme, screen, userEvent, waitFor } from "@/test/test-utils";
import { SessionForm } from "./SessionForm";

const game = {
	id: 1,
	title: "Carcassonne",
	image: null,
	hasScoring: true,
	maxPlayTime: 45,
	expansions: [],
} as unknown as Game;
const longGame = {
	id: 2,
	title: "Twilight Imperium",
	image: null,
	hasScoring: true,
	maxPlayTime: 240,
	expansions: [],
} as unknown as Game;
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Reggi", image: null },
] as unknown as Player[];

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: vi.fn() } }),
}));

vi.mock("@/routes/players/-hooks/usePlayerModal", () => ({
	usePlayerModal: () => ({ savePlayer: vi.fn() }),
}));

vi.mock("../-hooks/useSessionForm", () => ({
	useSessionForm: () => ({
		isLoading: false,
		settings: undefined,
		locations: [],
		games: [game, longGame],
		players,
		suggestions: { recentGameIds: [1], frequentPlayerIds: [1, 2], lastLocationId: null },
	}),
}));

const renderForm = () => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithTheme(
		<SessionForm
			game={game}
			minutes={45}
			start={addMinutes(new Date(), -45)}
			buttonText="save"
			title="Log a session"
			onClick={onClick}
			disabled={false}
		/>,
	);
	return onClick;
};

const renderNewForm = () => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithTheme(
		<StrictMode>
			<SessionForm buttonText="save" title="Log a session" onClick={onClick} disabled={false} />
		</StrictMode>,
	);
	return onClick;
};

const startInput = () => screen.getByLabelText("new.start.label");
const durationInput = () => screen.getByLabelText("new.duration.label");

const addPlayer = async (user: ReturnType<typeof userEvent.setup>, name: string) => {
	await user.click(screen.getByRole("combobox", { name: "form.players.label" }));
	await user.click(screen.getByRole("option", { name }));
};

describe("SessionForm", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("hides the player error until the first submit in StrictMode", async () => {
		const user = userEvent.setup();
		const onClick = renderNewForm();

		expect(screen.queryByText("player-session:new.players.minimum")).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "save" }));

		expect(await screen.findByText("player-session:new.players.minimum")).toBeInTheDocument();
		expect(onClick).not.toHaveBeenCalled();
	});

	it("shows the player error again once the last player is removed", async () => {
		const user = userEvent.setup();
		renderNewForm();

		await addPlayer(user, "Mikhael");
		await user.click(screen.getByRole("button", { name: "form.player.remove" }));

		expect(await screen.findByText("player-session:new.players.minimum")).toBeInTheDocument();
	});

	it("starts a new session the default duration before now", () => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 9, 21));
		renderNewForm();

		expect(durationInput()).toHaveValue(30);
		expect(startInput()).toHaveValue("2026-10-11T08:51");
	});

	it("moves the start with the game's play time and the duration until the start is edited", async () => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 9, 21));
		const user = userEvent.setup();
		renderNewForm();

		await user.click(screen.getByRole("button", { name: "new.game.label" }));
		await user.click(screen.getByRole("option", { name: "Twilight Imperium" }));

		expect(durationInput()).toHaveValue(240);
		expect(startInput()).toHaveValue("2026-10-11T05:21");

		await user.clear(durationInput());
		await user.type(durationInput(), "60");

		expect(startInput()).toHaveValue("2026-10-11T08:21");

		await user.clear(startInput());
		await user.type(startInput(), "2026-10-11T07:00");
		await user.clear(durationInput());
		await user.type(durationInput(), "90");

		expect(startInput()).toHaveValue("2026-10-11T07:00");
	});

	it("asks for at least one player on the first submit", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await user.click(screen.getByRole("button", { name: "save" }));

		expect(await screen.findByText("player-session:new.players.minimum")).toBeInTheDocument();
		expect(onClick).not.toHaveBeenCalled();
	});

	it("adds and removes player rows", async () => {
		const user = userEvent.setup();
		renderForm();

		await addPlayer(user, "Mikhael");
		await addPlayer(user, "Reggi");

		expect(screen.getAllByRole("button", { name: "form.player.remove" })).toHaveLength(2);

		await user.click(screen.getAllByRole("button", { name: "form.player.remove" })[0]);

		expect(screen.getAllByRole("button", { name: "form.player.remove" })).toHaveLength(1);
		expect(screen.getByText("Reggi")).toBeInTheDocument();
		expect(screen.queryByText("Mikhael")).not.toBeInTheDocument();
	});

	it("pre-selects the winner from the scores until Won is toggled", async () => {
		const user = userEvent.setup();
		renderForm();

		await addPlayer(user, "Mikhael");
		await addPlayer(user, "Reggi");
		const [firstScore, secondScore] = screen.getAllByRole("spinbutton", { name: "form.player.score" });
		await user.type(firstScore, "120");
		await user.type(secondScore, "70");

		const wonButtons = () => screen.getAllByRole("button", { name: "form.player.won" });
		expect(wonButtons().map((x) => x.getAttribute("aria-pressed"))).toEqual(["true", "false"]);

		await user.click(wonButtons()[1]);
		await user.clear(secondScore);
		await user.type(secondScore, "10");

		expect(wonButtons().map((x) => x.getAttribute("aria-pressed"))).toEqual(["true", "true"]);
	});

	it("submits the players with their scores and toggles", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await addPlayer(user, "Mikhael");
		await user.type(screen.getByRole("spinbutton", { name: "form.player.score" }), "42");
		await user.click(screen.getByRole("button", { name: "form.player.first-play" }));
		await user.click(screen.getByRole("button", { name: "save" }));

		await waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		expect(onClick.mock.calls[0][0]).toMatchObject({
			gameId: 1,
			minutes: 45,
			locationId: null,
			playerSessions: [{ playerId: 1, score: 42, won: true, firstPlay: true }],
		});
	});
});
