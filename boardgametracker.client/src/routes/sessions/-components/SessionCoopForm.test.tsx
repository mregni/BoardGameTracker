import { addMinutes } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { SessionForm } from "./SessionForm";

const coopOnly = {
	id: 1,
	title: "Pandemic",
	image: null,
	hasScoring: true,
	hasCoopMode: true,
	coopByDefault: true,
	maxPlayTime: 45,
	expansions: [],
} as unknown as Game;
const coopOptional = {
	id: 2,
	title: "Gloomhaven",
	image: null,
	hasScoring: false,
	hasCoopMode: true,
	coopByDefault: false,
	maxPlayTime: 90,
	expansions: [],
} as unknown as Game;
const competitive = {
	id: 3,
	title: "Azul",
	image: null,
	hasScoring: true,
	hasCoopMode: false,
	coopByDefault: false,
	maxPlayTime: 30,
	expansions: [],
} as unknown as Game;
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Reggi", image: null },
] as unknown as Player[];

vi.mock("@tanstack/react-router", () => ({
	useRouter: () => ({ history: { back: vi.fn(), canGoBack: () => false } }),
	useNavigate: () => vi.fn(),
}));

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
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
		games: [coopOnly, coopOptional, competitive],
		players,
		suggestions: { recentGameIds: [], frequentPlayerIds: [], lastLocationId: null },
	}),
}));

const twoPlayers = [
	{ playerId: 1, won: false, firstPlay: true, score: 12 },
	{ playerId: 2, won: true, firstPlay: false, score: 20 },
];

const renderForm = (props: Partial<Parameters<typeof SessionForm>[0]> = {}) => {
	const onClick = vi.fn().mockResolvedValue(undefined);
	renderWithProviders(
		<SessionForm
			game={coopOnly}
			minutes={45}
			start={addMinutes(new Date(), -45)}
			playerSessions={twoPlayers}
			buttonText="save"
			title="Log a session"
			backTo="/games/1"
			onClick={onClick}
			disabled={false}
			{...props}
		/>,
	);
	return onClick;
};

const coopSwitch = () => screen.queryByRole("switch", { name: "coop.played" });
const wonToggles = () => screen.queryAllByRole("button", { name: "form.player.won" });

const pickGame = async (user: ReturnType<typeof userEvent.setup>, title: string) => {
	await user.click(screen.getByRole("combobox", { name: "new.game.label" }));
	await user.click(await screen.findByRole("option", { name: title }));
};

describe("SessionForm co-op", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("starts as co-op for a co-op game and replaces the won toggles and scores with the team result", () => {
		renderForm();

		expect(coopSwitch()).toBeChecked();
		expect(wonToggles()).toHaveLength(0);
		expect(screen.queryByRole("spinbutton", { name: /form\.player\.score/ })).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "coop.won" })).toHaveAttribute("aria-pressed", "false");
		expect(screen.getByRole("button", { name: "coop.lost" })).toHaveAttribute("aria-pressed", "false");
		expect(screen.getByLabelText("coop.team-score")).toBeInTheDocument();
		expect(screen.getAllByRole("button", { name: "form.player.first-play" })).toHaveLength(2);
	});

	it("brings the won toggles back when co-op is switched off", async () => {
		const user = userEvent.setup();
		renderForm();

		await user.click(coopSwitch() as HTMLElement);

		expect(coopSwitch()).not.toBeChecked();
		expect(wonToggles()).toHaveLength(2);
		expect(screen.queryByRole("button", { name: "coop.won" })).not.toBeInTheDocument();
	});

	it("asks for the team result before saving", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await user.click(screen.getByRole("button", { name: "save" }));

		expect(await screen.findByText("coop.result-required")).toBeInTheDocument();
		expect(onClick).not.toHaveBeenCalled();
	});

	it("saves the team result on every player and drops their scores", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await user.click(screen.getByRole("button", { name: "coop.won" }));
		await user.type(screen.getByLabelText("coop.team-score"), "37");
		await user.click(screen.getByRole("button", { name: "save" }));

		await vi.waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		const saved = onClick.mock.calls[0][0];
		expect(saved.isCoop).toBe(true);
		expect(saved.teamWon).toBe(true);
		expect(saved.teamScore).toBe(37);
		expect(saved.playerSessions).toEqual([
			{ playerId: 1, won: true, firstPlay: true, score: null },
			{ playerId: 2, won: true, firstPlay: false, score: null },
		]);
	});

	it("saves the player results without a team result when co-op is off", async () => {
		const user = userEvent.setup();
		const onClick = renderForm();

		await user.click(screen.getByRole("button", { name: "coop.lost" }));
		await user.click(coopSwitch() as HTMLElement);
		await user.click(screen.getByRole("button", { name: "save" }));

		await vi.waitFor(() => expect(onClick).toHaveBeenCalledTimes(1));
		const saved = onClick.mock.calls[0][0];
		expect(saved.isCoop).toBe(false);
		expect(saved.teamWon).toBeNull();
		expect(saved.playerSessions).toEqual(twoPlayers);
	});

	it("only offers co-op for co-op games, prefilled from the game", async () => {
		const user = userEvent.setup();
		renderForm();

		await pickGame(user, "Azul");
		expect(coopSwitch()).not.toBeInTheDocument();
		expect(wonToggles()).toHaveLength(2);

		await pickGame(user, "Gloomhaven");
		expect(coopSwitch()).not.toBeChecked();

		await user.click(coopSwitch() as HTMLElement);
		expect(screen.queryByLabelText("coop.team-score")).not.toBeInTheDocument();

		await pickGame(user, "Pandemic");
		expect(coopSwitch()).toBeChecked();
	});

	it("keeps how an existing co-op session was logged", () => {
		renderForm({ game: competitive, coop: { isCoop: true, teamWon: false, teamScore: null } });

		expect(coopSwitch()).toBeChecked();
		expect(screen.getByRole("button", { name: "coop.lost" })).toHaveAttribute("aria-pressed", "true");
	});
});
