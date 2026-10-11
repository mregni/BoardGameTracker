import { addMinutes } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";
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
		games: [game],
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

const addPlayer = async (user: ReturnType<typeof userEvent.setup>, name: string) => {
	await user.click(screen.getByRole("combobox", { name: "form.players.label" }));
	await user.click(screen.getByRole("option", { name }));
};

describe("SessionForm", () => {
	beforeEach(() => {
		vi.clearAllMocks();
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
