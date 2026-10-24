import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";
import type { PlayerLinkSuggestions, ProfileResponse, User } from "@/models/Auth/Auth";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	suggestions: vi.fn(),
	link: vi.fn(),
	create: vi.fn(),
	skip: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/services/authService", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/services/authService")>()),
	getPlayerLinkSuggestionsCall: mocks.suggestions,
	linkOwnPlayerCall: mocks.link,
	createOwnPlayerCall: mocks.create,
	skipPlayerLinkCall: mocks.skip,
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

import { PlayerLinkPrompt } from "./PlayerLinkPrompt";

const signedIn = (needsPlayerLink: boolean, roles = ["User"]): User => ({
	id: "u1",
	username: "mregni",
	displayName: "Mikhaël Regni",
	roles,
	profileImage: null,
	needsPlayerLink,
});

const profile: ProfileResponse = {
	id: "u1",
	username: "mregni",
	email: "mikhael@example.com",
	displayName: "Mikhaël Regni",
	roles: ["User"],
	createdAt: new Date(),
	lastLoginAt: null,
	playerId: 3,
	profileImage: null,
	needsPlayerLink: false,
};

const suggestions = (overrides: Partial<PlayerLinkSuggestions> = {}): PlayerLinkSuggestions => ({
	suggestions: [
		{ id: 3, name: "Mikhaël R", image: "/images/3.png", reason: "email" },
		{ id: 4, name: "Mikhaël", image: null, reason: "partial" },
	],
	players: [
		{ id: 5, name: "Anna", image: null },
		{ id: 3, name: "Mikhaël R", image: "/images/3.png" },
		{ id: 4, name: "Mikhaël", image: null },
	],
	canCreatePlayer: true,
	...overrides,
});

const radios = () => screen.getAllByRole("radio");

describe("PlayerLinkPrompt", () => {
	beforeAll(() => {
		Element.prototype.hasPointerCapture = vi.fn(() => false);
		Element.prototype.releasePointerCapture = vi.fn();
		Element.prototype.scrollIntoView = vi.fn();
	});

	beforeEach(() => {
		vi.clearAllMocks();
		mocks.link.mockResolvedValue(profile);
		mocks.create.mockResolvedValue(profile);
		mocks.skip.mockResolvedValue({ ...profile, playerId: null });
	});

	afterEach(() => {
		useAuth.setState({ user: null, isAuthenticated: false });
	});

	it("stays hidden and asks nothing when the account does not need a player", () => {
		useAuth.getState().setSession("access", signedIn(false));

		renderWithProviders(<PlayerLinkPrompt />);

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(mocks.suggestions).not.toHaveBeenCalled();
	});

	it("shows the suggestions in the order of the server, best first and selected, with the reason", async () => {
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));

		renderWithProviders(<PlayerLinkPrompt />);

		expect(await screen.findByRole("dialog")).toBeInTheDocument();
		expect(screen.getByText("player-link.title", { selector: "h2" })).toBeInTheDocument();
		const options = radios();
		expect(options).toHaveLength(3);
		expect(options[0]).toHaveAccessibleName(/Mikhaël R.*player-link\.reason\.email/);
		expect(options[1]).toHaveAccessibleName(/Mikhaël.*player-link\.reason\.partial/);
		expect(options[2]).toHaveAccessibleName(/player-link\.new\.title.*player-link\.new\.hint/);
		expect(options[0]).toBeChecked();
		await waitFor(() => expect(options[0]).toHaveFocus());
		expect(screen.getByRole("img", { name: "Mikhaël R" })).toHaveClass("rounded-lg");
		expect(screen.getByRole("combobox", { name: "player-link.someone-else" })).toHaveTextContent(
			"player-link.search-players",
		);
		expect(screen.getByText("player-link.later-hint")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "player-link.skip" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "player-link.create" })).not.toBeInTheDocument();
	});

	it("asks whether the one suggestion is you", async () => {
		mocks.suggestions.mockResolvedValue(
			suggestions({ suggestions: [{ id: 3, name: "Mikhaël R", image: null, reason: "email" }] }),
		);
		useAuth.getState().setSession("access", signedIn(true));

		renderWithProviders(<PlayerLinkPrompt />);

		expect(await screen.findByText("player-link.title-one", { selector: "h2" })).toBeInTheDocument();
	});

	it("links the chosen suggestion, clears the flag and confirms it", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(radios()[1]);
		await user.click(screen.getByRole("button", { name: "player-link.confirm" }));

		await waitFor(() => expect(mocks.link).toHaveBeenCalledWith(4));
		await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
		expect(useAuth.getState().user?.needsPlayerLink).toBe(false);
		expect(mocks.successToast).toHaveBeenCalledWith("auth:player-link.linked", undefined, { name: "Mikhaël" });
	});

	it("links someone else picked from the list of the other players", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(screen.getByRole("combobox", { name: "player-link.someone-else" }));
		const options = await screen.findAllByRole("option");
		expect(options).toHaveLength(1);
		await user.click(await screen.findByRole("option", { name: /Anna/ }));
		expect(radios().every((radio) => !(radio as HTMLInputElement).checked)).toBe(true);
		await user.click(screen.getByRole("button", { name: "player-link.confirm" }));

		await waitFor(() => expect(mocks.link).toHaveBeenCalledWith(5));
	});

	it("creates a new player when the new card is chosen", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(screen.getByRole("radio", { name: /player-link\.new\.title/ }));
		await user.click(screen.getByRole("button", { name: "player-link.create" }));

		await waitFor(() => expect(mocks.create).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(useAuth.getState().user?.needsPlayerLink).toBe(false));
		expect(mocks.link).not.toHaveBeenCalled();
		expect(mocks.successToast).toHaveBeenCalledWith("auth:player-link.created", undefined, { name: undefined });
	});

	it("skips for now", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(screen.getByRole("button", { name: "player-link.skip" }));

		await waitFor(() => expect(mocks.skip).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
		expect(useAuth.getState().user?.needsPlayerLink).toBe(false);
		expect(mocks.link).not.toHaveBeenCalled();
	});

	it("skips as well when dismissed with Escape", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.keyboard("{Escape}");

		await waitFor(() => expect(mocks.skip).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
		expect(useAuth.getState().user?.needsPlayerLink).toBe(false);
	});

	it("ignores a dismiss while a request is running", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		mocks.link.mockReturnValue(new Promise(() => {}));
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(screen.getByRole("button", { name: "player-link.confirm" }));
		await waitFor(() => expect(mocks.link).toHaveBeenCalledWith(3));
		await waitFor(() => expect(screen.getByRole("radio", { name: /player-link\.new\.title/ })).toBeDisabled());
		await user.keyboard("{Escape}");

		expect(mocks.skip).not.toHaveBeenCalled();
		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	it("shows the error and keeps the dialog open when the player was taken meanwhile", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions());
		mocks.link.mockRejectedValue(new Error("conflict"));
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);
		await screen.findByRole("dialog");

		await user.click(screen.getByRole("button", { name: "player-link.confirm" }));

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalledWith("auth:player-link.failed"));
		expect(screen.getByRole("dialog")).toBeInTheDocument();
		expect(useAuth.getState().user?.needsPlayerLink).toBe(true);
		await waitFor(() => expect(mocks.suggestions).toHaveBeenCalledTimes(2));
	});

	it("lets a reader link a player but not create one", async () => {
		mocks.suggestions.mockResolvedValue(suggestions({ canCreatePlayer: false }));
		useAuth.getState().setSession("access", signedIn(true, ["Reader"]));

		renderWithProviders(<PlayerLinkPrompt />);

		await screen.findByRole("dialog");
		expect(screen.queryByRole("radio", { name: /player-link\.new\.title/ })).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "player-link.confirm" })).toBeEnabled();
		expect(screen.getByRole("button", { name: "player-link.skip" })).toBeInTheDocument();
	});

	it("lets the user pick from all players or create one right away when nothing matches", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions({ suggestions: [] }));
		useAuth.getState().setSession("access", signedIn(true));

		renderWithProviders(<PlayerLinkPrompt />);

		await screen.findByRole("dialog");
		expect(screen.getByRole("combobox", { name: "player-link.pick-player" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "player-link.confirm" })).toBeDisabled();
		const fresh = screen.getByRole("radio", { name: /player-link\.new\.title/ });
		expect(fresh).toBeEnabled();
		await waitFor(() => expect(fresh).toHaveFocus());

		await user.click(fresh);
		await user.click(screen.getByRole("button", { name: "player-link.create" }));

		await waitFor(() => expect(mocks.create).toHaveBeenCalledTimes(1));
		expect(mocks.link).not.toHaveBeenCalled();
	});

	it("has no choice cards for a reader without suggestions", async () => {
		mocks.suggestions.mockResolvedValue(suggestions({ suggestions: [], canCreatePlayer: false }));
		useAuth.getState().setSession("access", signedIn(true, ["Reader"]));

		renderWithProviders(<PlayerLinkPrompt />);

		await screen.findByRole("dialog");
		expect(screen.queryAllByRole("radio")).toHaveLength(0);
		expect(screen.getByRole("button", { name: "player-link.confirm" })).toBeDisabled();
	});

	it("only offers to create a player or skip when there are no unlinked players", async () => {
		const user = userEvent.setup();
		mocks.suggestions.mockResolvedValue(suggestions({ suggestions: [], players: [] }));
		useAuth.getState().setSession("access", signedIn(true));
		renderWithProviders(<PlayerLinkPrompt />);

		await screen.findByRole("dialog");
		expect(screen.getByText("player-link.title-new", { selector: "h2" })).toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "player-link.confirm" })).not.toBeInTheDocument();
		expect(screen.getAllByRole("button", { name: "player-link.create" })).toHaveLength(1);

		await user.click(screen.getByRole("button", { name: "player-link.create" }));

		await waitFor(() => expect(mocks.create).toHaveBeenCalledTimes(1));
	});

	it("skips quietly without a dialog when there is nothing to link or create", async () => {
		mocks.suggestions.mockResolvedValue(suggestions({ suggestions: [], players: [], canCreatePlayer: false }));
		useAuth.getState().setSession("access", signedIn(true, ["Reader"]));

		renderWithProviders(<PlayerLinkPrompt />);

		await waitFor(() => expect(mocks.skip).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(useAuth.getState().user?.needsPlayerLink).toBe(false));
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});
