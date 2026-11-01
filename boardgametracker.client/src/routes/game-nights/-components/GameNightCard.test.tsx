import { addDays } from "date-fns";
import { describe, expect, it, vi } from "vitest";
import { type Game, type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const media = vi.hoisted(() => ({ isDesktop: false }));
const navigate = vi.hoisted(() => vi.fn());

vi.mock("@/hooks/useMediaQuery", () => ({
	useMediaQuery: () => media.isDesktop,
}));

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => navigate,
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params: { gameNightId: number } }) => (
		<a href={to.replace("$gameNightId", String(params.gameNightId))}>{children}</a>
	),
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: { publicUrl: "https://games.example.com", emailEnabled: false } }),
}));

vi.mock("@/services/gameNightService", () => ({
	sendInvitesCall: vi.fn(),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

import { GameNightCard } from "./GameNightCard";
import { PastGameNightItem } from "./PastGameNightItem";
import { DESKTOP_MAX, MOBILE_MAX, RsvpAvatarStack } from "./RsvpAvatarStack";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const settings = { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-us" } as Settings;

const gameNight = (startDate: Date) =>
	({
		id: 4,
		title: "Strategy night",
		notes: "",
		startDate,
		hostId: 1,
		host: player(1, "Host"),
		locationId: 3,
		location: { id: 3, name: "Home" },
		linkId: "abc-123",
		suggestedGames: [{ id: 9, title: "Carcassonne", image: null }],
		invitedPlayers: [
			{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: player(1, "Host") },
			{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending, player: player(7, "Guest") },
			{ id: 13, playerId: 8, gameNightId: 4, state: GameNightRsvpState.Declined, player: player(8, "Other") },
		],
		summary: {
			gamesPlayed: 0,
			totalMinutes: 0,
			mostWins: 0,
			winnerIds: [],
			runningGames: 0,
			attendeeIds: [1],
			attendanceRecorded: false,
			playedGameIds: [],
		},
	}) as unknown as GameNight;

describe("GameNightCard", () => {
	it("keeps the time pill, title, location and host next to the date box", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 1))}
				players={[]}
				settings={settings}
				canWrite
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		const heading = screen.getByRole("heading", { name: "Strategy night" });
		const head = heading.parentElement as HTMLElement;
		expect(within(head).getByText("card.when")).toBeInTheDocument();
		expect(within(head).getByText("Home")).toBeInTheDocument();
		expect(within(head).queryByRole("button")).not.toBeInTheDocument();
	});

	it("shows location, host, RSVP summary and suggested games", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 3))}
				players={[]}
				settings={settings}
				canWrite
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.getByText("Home")).toBeInTheDocument();
		expect(screen.getByText("card.host")).toBeInTheDocument();
		expect(screen.getByRole("img", { name: /rsvp\.summary\.coming/ })).toBeInTheDocument();
		expect(screen.getByText("Carcassonne")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "card.actions" })).toBeInTheDocument();
	});

	it("opens the RSVP dialog for this night", async () => {
		const user = userEvent.setup();
		const onManageRsvps = vi.fn();
		const night = gameNight(addDays(new Date(), 3));
		renderWithProviders(
			<GameNightCard
				gameNight={night}
				players={[]}
				settings={settings}
				canWrite
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={onManageRsvps}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "card.manage-rsvps" }));

		expect(onManageRsvps).toHaveBeenCalledWith(night);
	});

	it("hides the host actions for read-only users", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 3))}
				players={[]}
				settings={settings}
				canWrite={false}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.queryByRole("button", { name: "card.actions" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "card.manage-rsvps" })).not.toBeInTheDocument();
	});

	it("tells the host on the highlighted night who hasn't answered yet", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 3))}
				players={[]}
				settings={settings}
				canWrite
				highlight
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		const names = within(screen.getByTestId("rsvp-names")).getAllByRole("listitem");
		expect(names.map((item) => item.textContent)).toEqual(["card.waiting", "card.declined-names"]);
		expect(screen.getByText("rsvp.coming-of")).toBeInTheDocument();
		expect(screen.queryByTestId("rsvp-summary")).not.toBeInTheDocument();
		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(stack.querySelectorAll("[data-host-badge]")).toHaveLength(1);
	});
});

describe("RsvpAvatarStack", () => {
	const rsvps = Array.from({ length: 10 }, (_, index) => ({
		id: 100 + index,
		playerId: 100 + index,
		gameNightId: 4,
		state: GameNightRsvpState.Pending,
		player: player(100 + index, `Player ${index}`),
	}));

	it("shows five avatars and a +N button on phones that reveals everyone", async () => {
		media.isDesktop = false;
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("listitem")).toHaveLength(MOBILE_MAX + 1);
		await userEvent.click(within(stack).getByRole("button", { name: "rsvp.show-all" }));
		expect(within(stack).getAllByRole("listitem")).toHaveLength(rsvps.length);
	});

	it("shows eight avatars and a +N button from md up", () => {
		media.isDesktop = true;
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("listitem")).toHaveLength(DESKTOP_MAX + 1);
		expect(within(stack).getByRole("button", { name: "rsvp.show-all" })).toHaveTextContent("+2");
	});

	it("names each answer for screen readers when the avatars aren't links", () => {
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps.slice(0, 2)} linkPlayers={false} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByText("rsvp.avatar-label")).toHaveLength(2);
		expect(within(stack).queryByRole("button")).not.toBeInTheDocument();
	});

	it("shows no chip when everyone fits", () => {
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps.slice(0, 5)} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("button")).toHaveLength(5);
		expect(within(stack).queryByText(/^\+/)).not.toBeInTheDocument();
	});
});

describe("PastGameNightItem", () => {
	it("opens the night and says nothing was logged yet", () => {
		renderWithProviders(
			<PastGameNightItem gameNight={gameNight(addDays(new Date(), -2))} settings={settings} players={[]} games={[]} />,
		);

		expect(screen.getByRole("link")).toHaveAttribute("href", "/game-nights/4");
		expect(screen.getByText("Home · past-night.nothing-logged")).toBeInTheDocument();
	});

	it("counts the recorded attendance of a night without games", () => {
		const night = {
			...gameNight(addDays(new Date(), -2)),
			summary: {
				gamesPlayed: 0,
				totalMinutes: 0,
				mostWins: 0,
				winnerIds: [],
				runningGames: 0,
				attendeeIds: [1, 7],
				attendanceRecorded: true,
				playedGameIds: [],
			},
		} as GameNight;

		renderWithProviders(<PastGameNightItem gameNight={night} settings={settings} players={[]} games={[]} />);

		expect(screen.getByText("Home · past-night.attended-count · past-night.nothing-logged")).toBeInTheDocument();
	});

	it("shows what was played and who shared the most wins", () => {
		const night = {
			...gameNight(addDays(new Date(), -2)),
			summary: {
				gamesPlayed: 4,
				totalMinutes: 200,
				mostWins: 2,
				winnerIds: [1, 7],
				runningGames: 0,
				attendeeIds: [1, 7],
				attendanceRecorded: false,
				playedGameIds: [3, 5, 6, 8, 9],
			},
		} as GameNight;
		const games = [3, 5, 6, 8, 9].map((id) => ({ id, title: `Game ${id}`, image: null })) as unknown as Game[];

		renderWithProviders(
			<PastGameNightItem
				gameNight={night}
				settings={settings}
				players={[player(1, "Host"), player(7, "Guest")]}
				games={games}
			/>,
		);

		expect(screen.getByText("Home · past-night.attended-count · past-night.games-played")).toBeInTheDocument();
		expect(screen.getByText("past-night.played")).toHaveClass("sr-only");
		expect(within(screen.getByTestId("played-games")).getByText("+1")).toBeInTheDocument();
		expect(screen.getByText("past-night.shared-most-wins: Host, Guest")).toHaveClass("sr-only");
		expect(screen.getByText("Guest")).toBeInTheDocument();
	});
});

describe("GameNightCard title", () => {
	it("links to the night", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 1))}
				players={[]}
				settings={settings}
				canWrite={false}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.getByRole("link", { name: "Strategy night" })).toHaveAttribute("href", "/game-nights/4");
	});
});

describe("GameNightCard main action", () => {
	it("copies the invite link from the highlighted upcoming night, and leaves it out of its menu", async () => {
		const user = userEvent.setup();
		const writeText = vi.fn().mockResolvedValue(undefined);
		Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 1))}
				players={[]}
				settings={settings}
				canWrite
				highlight
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		const copyButton = screen.getByRole("button", { name: "card.copy-link" });
		expect(copyButton.className).toContain("bg-primary");
		await user.click(copyButton);
		expect(writeText).toHaveBeenCalledWith("https://games.example.com/rsvp?linkId=abc-123");
		expect(await screen.findByRole("button", { name: "card.copied" })).toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "card.actions" }));
		expect(screen.queryByRole("menuitem", { name: "card.copy-link" })).not.toBeInTheDocument();
	});

	it("starts a game from a live night and says what is on the table", async () => {
		const user = userEvent.setup();
		const night = gameNight(new Date());
		night.summary.runningGames = 2;
		renderWithProviders(
			<GameNightCard
				gameNight={night}
				players={[]}
				settings={settings}
				canWrite
				highlight
				live
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.getByText("card.live-since")).toBeInTheDocument();
		expect(screen.getByText("card.running")).toBeInTheDocument();
		expect(screen.queryByText("card.waiting")).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "play.start.button" }));

		expect(navigate).toHaveBeenCalledWith({
			to: "/game-nights/$gameNightId",
			params: { gameNightId: 4 },
			search: { start: true },
		});
	});

	it("opens every card from its title", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 8))}
				players={[]}
				settings={settings}
				canWrite={false}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.getByRole("link", { name: "Strategy night" })).toHaveAttribute("href", "/game-nights/4");
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});
});

describe("RsvpAvatarStack answer badges", () => {
	it("marks coming, maybe and not coming with a symbol and leaves no answer plain", () => {
		const states = [
			GameNightRsvpState.Accepted,
			GameNightRsvpState.Maybe,
			GameNightRsvpState.Pending,
			GameNightRsvpState.Declined,
		];
		const rsvps = states.map((state, index) => ({
			id: 200 + index,
			playerId: 200 + index,
			gameNightId: 4,
			state,
			player: player(200 + index, `P${index}`),
		}));
		const { container } = renderWithProviders(<RsvpAvatarStack rsvps={rsvps} linkPlayers={false} />);

		expect([...container.querySelectorAll("[data-rsvp-badge]")].map((x) => x.getAttribute("data-rsvp-badge"))).toEqual([
			"accepted",
			"maybe",
			"declined",
		]);
	});
});
