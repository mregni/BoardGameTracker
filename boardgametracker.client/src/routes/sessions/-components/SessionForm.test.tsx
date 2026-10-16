import { addMinutes } from "date-fns";
import { StrictMode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import { act, renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
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
	{ id: 3, name: "Nova", image: null },
	{ id: 4, name: "Anna", image: null },
] as unknown as Player[];

const router = vi.hoisted(() => ({ back: vi.fn(), navigate: vi.fn() }));

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: router.back, canGoBack: () => false } }),
	useNavigate: () => router.navigate,
}));

const createdPlayer = { id: 3, name: "Nova", image: null } as unknown as Player;

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: ({ open, onPlayerCreated }: { open: boolean; onPlayerCreated?: (player: Player) => void }) =>
		open ? (
			<button type="button" onClick={() => onPlayerCreated?.(createdPlayer)}>
				save-new-player
			</button>
		) : null,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: async () => ({}) }),
}));

Element.prototype.hasPointerCapture = () => false;
Element.prototype.releasePointerCapture = () => {};
Element.prototype.scrollIntoView = () => {};

vi.mock("../-hooks/useSessionForm", () => ({
	useSessionForm: () => ({
		isLoading: false,
		settings: undefined,
		locations: [],
		games: [game, longGame],
		players,
		suggestions: { recentGameIds: [1], frequentPlayerIds: [2, 1], lastLocationId: null },
	}),
}));

const renderForm = () => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithProviders(
		<SessionForm
			game={game}
			minutes={45}
			start={addMinutes(new Date(), -45)}
			buttonText="save"
			title="Log a session"
			backTo="/games/1"
			onClick={onClick}
			disabled={false}
		/>,
	);
	return onClick;
};

const renderNewForm = () => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithProviders(
		<StrictMode>
			<SessionForm buttonText="save" title="Log a session" backTo="/" onClick={onClick} disabled={false} />
		</StrictMode>,
	);
	return onClick;
};

const startTimeSegment = (name: RegExp) =>
	within(screen.getByRole("group", { name: "common:time" })).getByRole("spinbutton", { name });
const startTime = () => `${startTimeSegment(/hour/i).textContent}:${startTimeSegment(/minute/i).textContent}`;
const durationInput = () => screen.getByLabelText("new.duration.label");

const playerSelect = () => screen.getByRole("combobox", { name: "form.players.label" });

const openPlayerSelect = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(playerSelect());
	return screen.findByRole("listbox");
};

const optionNames = () => screen.getAllByRole("option").map((x) => x.textContent?.slice(1));

const addPlayer = async (user: ReturnType<typeof userEvent.setup>, name: string) => {
	await openPlayerSelect(user);
	await user.click(screen.getByRole("option", { name: new RegExp(name) }));
};

describe("SessionForm", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("shows the page title and a back link above the form", async () => {
		const user = userEvent.setup();
		renderForm();

		expect(screen.getByRole("heading", { name: "Log a session" })).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: /back/ }));

		expect(router.navigate).toHaveBeenCalledWith({ to: "/games/1", replace: true });
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
		expect(screen.getByText("new.start.label")).toBeInTheDocument();
		expect(startTime()).toBe("08:51");
	});

	it("moves the start with the game's play time and the duration until the start is edited", async () => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 9, 21));
		const user = userEvent.setup();
		renderNewForm();

		await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
		await user.click(await screen.findByRole("option", { name: "Twilight Imperium" }));

		expect(durationInput()).toHaveValue(240);
		expect(startTime()).toBe("05:21");

		await user.clear(durationInput());
		await user.type(durationInput(), "60");

		expect(startTime()).toBe("08:21");

		act(() => startTimeSegment(/hour/i).focus());
		await user.keyboard("07");
		act(() => startTimeSegment(/minute/i).focus());
		await user.keyboard("00");
		await user.clear(durationInput());
		await user.type(durationInput(), "90");

		expect(startTime()).toBe("07:00");
	});

	it("groups recently played games above the other games until searching", async () => {
		const user = userEvent.setup();
		renderNewForm();

		await user.click(screen.getByRole("combobox", { name: "new.game.label" }));

		const recent = await screen.findByRole("group", { name: "player-session:form.game.recent" });
		const all = screen.getByRole("group", { name: "form.game.all" });
		expect(
			within(recent)
				.getAllByRole("option")
				.map((x) => x.textContent),
		).toEqual(["Carcassonne"]);
		expect(
			within(all)
				.getAllByRole("option")
				.map((x) => x.textContent),
		).toEqual(["Twilight Imperium"]);

		await user.type(screen.getByPlaceholderText("search"), "c");

		expect(screen.queryByRole("group", { name: "player-session:form.game.recent" })).not.toBeInTheDocument();
		expect(screen.getAllByRole("option").map((x) => x.textContent)).toEqual(["Carcassonne"]);
	});

	it("asks for at least one player on the first submit", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await user.click(screen.getByRole("button", { name: "save" }));

		expect(await screen.findByText("player-session:new.players.minimum")).toBeInTheDocument();
		expect(onClick).not.toHaveBeenCalled();
	});

	it("lists frequent players first and closes after adding a player", async () => {
		const user = userEvent.setup();
		renderForm();

		await openPlayerSelect(user);

		expect(optionNames()).toEqual(["Reggi", "Mikhael", "Anna", "Nova"]);

		await user.click(screen.getByRole("option", { name: /Mikhael/ }));

		expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
		expect(screen.getByText("Mikhael")).toBeInTheDocument();
		expect(playerSelect()).toHaveTextContent("form.players.search");
	});

	it("leaves already added players out of the options", async () => {
		const user = userEvent.setup();
		renderForm();

		await addPlayer(user, "Reggi");
		await openPlayerSelect(user);

		expect(optionNames()).toEqual(["Mikhael", "Anna", "Nova"]);
	});

	it("searches players inside the dropdown", async () => {
		const user = userEvent.setup();
		renderForm();

		await openPlayerSelect(user);
		await user.type(screen.getByPlaceholderText("search"), "nn");

		expect(optionNames()).toEqual(["Anna"]);
	});

	it("adds a newly created player as a row", async () => {
		const user = userEvent.setup();
		renderForm();

		await user.click(screen.getByRole("button", { name: /new\.create-player/ }));
		await user.click(screen.getByText("save-new-player"));

		expect(screen.getAllByRole("button", { name: "form.player.remove" })).toHaveLength(1);
		expect(screen.getByText("Nova")).toBeInTheDocument();
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

	it("lays out the shared fields in two columns on wide screens with compact player rows", async () => {
		const user = userEvent.setup();
		renderForm();

		const fields = screen.getByTestId("session-form-fields");
		expect(fields).toHaveClass("lg:grid", "lg:grid-cols-2");
		expect(screen.getByText("expansions.title")).toBeInTheDocument();
		expect(screen.getByLabelText("new.comment.label")).toHaveAttribute("rows", "2");

		await addPlayer(user, "Mikhael");

		const row = screen.getByTestId("session-player-row");
		expect(row).toHaveClass("@md:grid-cols-[1.75rem_minmax(0,1fr)_auto_auto]");
		expect(row.parentElement).toHaveClass("@container");
	});

	it("sends the comment with the session", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await addPlayer(user, "Mikhael");
		await user.type(screen.getByLabelText("new.comment.label"), "Close one");
		await user.click(screen.getByRole("button", { name: "save" }));

		await waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		expect(onClick.mock.calls[0][0]).toMatchObject({ comment: "Close one", expansionIds: [] });
	});
});
