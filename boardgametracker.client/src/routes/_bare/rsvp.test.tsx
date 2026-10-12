import { addDays } from "date-fns";
import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { Page: FC },
	navigate: vi.fn(),
	getGameNightByLinkCall: vi.fn(),
	updateGameNightRsvpByLinkCall: vi.fn(),
	getGameNightManualsCall: vi.fn(),
	errorToast: vi.fn(),
	settings: {} as Record<string, unknown>,
	auth: { isAuthenticated: false, authStatus: { authEnabled: false } },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return { ...config, useSearch: () => ({ linkId: "abc" }) };
	},
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/gameNightService", () => ({
	getGameNightsCall: vi.fn(),
	getGameNightByLinkCall: (linkId: string) => mocks.getGameNightByLinkCall(linkId),
	updateGameNightRsvpByLinkCall: (linkId: string, rsvp: unknown) => mocks.updateGameNightRsvpByLinkCall(linkId, rsvp),
}));

vi.mock("@/services/manualService", () => ({
	getGameManualsCall: vi.fn(),
	getGameIdsWithManualsCall: vi.fn(),
	getGameNightManualsCall: (linkId: string) => mocks.getGameNightManualsCall(linkId),
	manualInviteDownloadUrl: (linkId: string, manualId: number) => `/manuals/${linkId}/${manualId}`,
}));

vi.mock("@/routes/settings/-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings: mocks.settings }),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: typeof mocks.auth) => unknown) => selector(mocks.auth),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: mocks.errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (error: unknown, fallbackKey: string) => (error instanceof Error ? error.message : fallbackKey),
}));

vi.mock("@/hooks/useMediaQuery", () => ({
	useMediaQuery: () => false,
}));

vi.mock("@/components/BgtForm", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/components/BgtForm")>()),
	BgtSelect: ({
		label,
		items,
		value,
		onValueChange,
	}: {
		label: string;
		items: { value: number; label: string }[];
		value: number | null;
		onValueChange: (value: number) => void;
	}) => (
		<label>
			{label}
			<select value={String(value ?? "")} onChange={(event) => onValueChange(Number(event.target.value))}>
				<option value="">-</option>
				{items.map((item) => (
					<option key={item.value} value={item.value}>
						{item.label}
					</option>
				))}
			</select>
		</label>
	),
}));

import "./rsvp";

const player = (id: number, name: string) => ({ id, name, image: null }) as unknown as Player;

const gameNight = (startDate: Date) =>
	({
		id: 4,
		title: "Strategy night",
		notes: "Bring snacks",
		startDate,
		hostId: 1,
		host: player(1, "Hank"),
		locationId: 3,
		location: { id: 3, name: "Home" },
		linkId: "abc",
		suggestedGames: [{ id: 9, title: "Carcassonne", image: null }],
		invitedPlayers: [
			{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: player(1, "Hank") },
			{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending, player: player(7, "Gina") },
			{ id: 13, playerId: 8, gameNightId: 4, state: GameNightRsvpState.Declined, player: player(8, "Olaf") },
		],
	}) as unknown as GameNight;

const renderPage = () => renderWithProviders(<mocks.captured.Page />);

describe("RsvpPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.settings = {
			timeFormat: "HH:mm",
			dateFormat: "dd-MM-yyyy",
			uiLanguage: "en-US",
			emailEnabled: true,
			rsvpAuthenticationEnabled: false,
		} satisfies Partial<Settings>;
		mocks.auth = { isAuthenticated: false, authStatus: { authEnabled: false } };
		mocks.getGameNightManualsCall.mockResolvedValue([]);
		mocks.updateGameNightRsvpByLinkCall.mockResolvedValue({});
	});

	it("shows a loader while the game night loads", () => {
		mocks.getGameNightByLinkCall.mockReturnValue(new Promise(() => {}));

		renderPage();

		expect(screen.getByText("loading-data")).toBeInTheDocument();
	});

	it("explains an unknown or expired link", async () => {
		mocks.getGameNightByLinkCall.mockRejectedValue(new Error("404"));

		renderPage();

		expect(await screen.findByText("not-found")).toBeInTheDocument();
		expect(screen.getByText("not-found-description")).toBeInTheDocument();
	});

	it("asks to log in when RSVPs require an account", async () => {
		const user = userEvent.setup();
		mocks.settings.rsvpAuthenticationEnabled = true;
		mocks.auth = { isAuthenticated: false, authStatus: { authEnabled: true } };
		mocks.getGameNightByLinkCall.mockResolvedValue(gameNight(addDays(new Date(), 2)));

		renderPage();
		await user.click(screen.getByRole("button", { name: "auth:login" }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login", search: { redirect: "/rsvp?linkId=abc" } });
	});

	it("shows the event, who is coming and the suggested games", async () => {
		mocks.getGameNightByLinkCall.mockResolvedValue(gameNight(addDays(new Date(), 2)));

		renderPage();

		expect(await screen.findByRole("heading", { name: "Strategy night" })).toBeInTheDocument();
		expect(screen.getByText("Home")).toBeInTheDocument();
		expect(screen.getByText("Bring snacks")).toBeInTheDocument();
		expect(screen.getByText("whos-coming")).toBeInTheDocument();
		expect(screen.getByTestId("rsvp-summary")).toHaveTextContent("rsvp.summary.coming · rsvp.summary.pending");
		expect(within(screen.getByRole("region", { name: "group.coming" })).getByText("Hank")).toBeInTheDocument();
		expect(within(screen.getByRole("region", { name: "group.pending" })).getByText("Gina")).toBeInTheDocument();
		expect(within(screen.getByRole("region", { name: "group.declined" })).getByText("Olaf")).toBeInTheDocument();
		expect(screen.getByText("Carcassonne")).toBeInTheDocument();
	});

	it("submits an answer and reopens it with the name and answer preselected", async () => {
		const user = userEvent.setup();
		mocks.getGameNightByLinkCall.mockResolvedValue(gameNight(addDays(new Date(), 2)));

		renderPage();
		const select = await screen.findByRole("combobox", { name: "who-are-you" });
		expect(within(select).queryByRole("option", { name: "Hank" })).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "send-answer" })).toBeDisabled();

		await user.selectOptions(select, "7");
		await user.click(screen.getByRole("button", { name: "coming" }));
		await user.click(screen.getByRole("button", { name: "send-answer" }));

		expect(mocks.updateGameNightRsvpByLinkCall).toHaveBeenCalledWith("abc", {
			id: 12,
			gameNightId: 4,
			playerId: 7,
			state: GameNightRsvpState.Accepted,
		});
		const confirmation = await screen.findByRole("status");
		expect(confirmation).toHaveTextContent("answered-accepted");
		expect(confirmation).toHaveTextContent("host-notified");
		expect(screen.getByText("whos-coming")).toBeInTheDocument();
		expect(mocks.getGameNightByLinkCall).toHaveBeenCalledTimes(2);
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();

		await user.click(within(confirmation).getByRole("button", { name: "change-response" }));

		expect(screen.getByRole("combobox", { name: "who-are-you" })).toHaveValue("7");
		expect(screen.getByRole("button", { name: "coming" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "maybe" })).toHaveAttribute("aria-pressed", "false");
	});

	it("keeps the form open and warns when the answer cannot be saved", async () => {
		const user = userEvent.setup();
		mocks.updateGameNightRsvpByLinkCall.mockRejectedValue(new Error("error.game-night.rsvp-closed"));
		mocks.getGameNightByLinkCall.mockResolvedValue(gameNight(addDays(new Date(), 2)));

		renderPage();
		await user.selectOptions(await screen.findByRole("combobox", { name: "who-are-you" }), "8");
		await user.click(screen.getByRole("button", { name: "cant-come" }));
		await user.click(screen.getByRole("button", { name: "send-answer" }));

		expect(mocks.errorToast).toHaveBeenCalledWith("error.game-night.rsvp-closed");
		expect(screen.getByRole("combobox", { name: "who-are-you" })).toBeInTheDocument();
	});

	it("shows only a message for a past game night", async () => {
		mocks.getGameNightByLinkCall.mockResolvedValue(gameNight(new Date(2020, 0, 5, 20, 0)));

		renderPage();

		expect(await screen.findByRole("heading", { name: "past-title" })).toBeInTheDocument();
		expect(screen.getByText("Sunday, January 5th, 2020")).toBeInTheDocument();
		expect(screen.getByText("past-description")).toBeInTheDocument();
		expect(screen.queryByText("whos-coming")).not.toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		expect(screen.queryByText("Carcassonne")).not.toBeInTheDocument();
		expect(screen.queryByText("Bring snacks")).not.toBeInTheDocument();
	});
});
