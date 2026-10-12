import { addDays } from "date-fns";
import { describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const media = vi.hoisted(() => ({ isDesktop: false }));

vi.mock("@/hooks/useMediaQuery", () => ({
	useMediaQuery: () => media.isDesktop,
}));

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => vi.fn(),
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
	}) as unknown as GameNight;

describe("GameNightCard", () => {
	it("keeps only the time pill and the title next to the date box", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 1))}
				settings={settings}
				canWrite
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		const heading = screen.getByRole("heading", { name: "Strategy night" });
		const head = heading.parentElement as HTMLElement;
		expect(head.children).toHaveLength(2);
		expect(within(head).getByText("card.when")).toBeInTheDocument();
		expect(within(head).queryByRole("button")).not.toBeInTheDocument();
		expect(within(head).queryByText("Home")).not.toBeInTheDocument();
	});

	it("shows location, host, RSVP summary and suggested games", () => {
		renderWithProviders(
			<GameNightCard
				gameNight={gameNight(addDays(new Date(), 3))}
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
		expect(screen.getByRole("button", { name: "card.copy-link" })).toBeInTheDocument();
	});

	it("opens the RSVP dialog for this night", async () => {
		const user = userEvent.setup();
		const onManageRsvps = vi.fn();
		const night = gameNight(addDays(new Date(), 3));
		renderWithProviders(
			<GameNightCard
				gameNight={night}
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
				settings={settings}
				canWrite={false}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
				onManageRsvps={vi.fn()}
			/>,
		);

		expect(screen.queryByRole("button", { name: "card.copy-link" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "card.manage-rsvps" })).not.toBeInTheDocument();
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

	it("shows five avatars and a +N chip on phones", () => {
		media.isDesktop = false;
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("button")).toHaveLength(MOBILE_MAX);
		expect(within(stack).getByText("+5")).toHaveAttribute("title", "Player 5, Player 6, Player 7, Player 8, Player 9");
	});

	it("shows eight avatars and a +N chip from md up", () => {
		media.isDesktop = true;
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("button")).toHaveLength(DESKTOP_MAX);
		expect(within(stack).getByText("+2")).toHaveAttribute("title", "Player 8, Player 9");
	});

	it("shows no chip when everyone fits", () => {
		renderWithProviders(<RsvpAvatarStack rsvps={rsvps.slice(0, 5)} />);

		const stack = screen.getByTestId("rsvp-avatar-stack");
		expect(within(stack).getAllByRole("button")).toHaveLength(5);
		expect(within(stack).queryByText(/^\+/)).not.toBeInTheDocument();
	});
});

describe("PastGameNightItem", () => {
	it("lists only the players who were coming", async () => {
		const user = userEvent.setup();
		renderWithProviders(
			<PastGameNightItem
				gameNight={gameNight(addDays(new Date(), -2))}
				settings={settings}
				canWrite
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		await user.click(screen.getByText("Strategy night"));

		expect(screen.getByText("past-night.summary")).toBeInTheDocument();
		expect(screen.getByText("Host")).toBeInTheDocument();
		expect(screen.queryByText("Guest")).not.toBeInTheDocument();
		expect(screen.queryByText("Other")).not.toBeInTheDocument();
	});
});
