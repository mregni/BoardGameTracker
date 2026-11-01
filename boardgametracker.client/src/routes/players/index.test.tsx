import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PlayerOverview } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	navigate: vi.fn(),
	players: [] as PlayerOverview[] | Error,
	search: {} as { q?: string; sort?: string },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: { playerId: number } }) => (
		<a href={params ? to.replace("$playerId", String(params.playerId)) : to}>{children}</a>
	),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayersOverview: () => ({
		queryKey: ["players", "overview"],
		queryFn: () => (mocks.players instanceof Error ? Promise.reject(mocks.players) : Promise.resolve(mocks.players)),
		retry: false,
	}),
}));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Players overview", () => {
	beforeEach(() => {
		mocks.navigate.mockReset();
		mocks.search = {};
		mocks.players = [
			{ id: 1, name: "dfgh", image: null, playCount: 0, winCount: 0, lastPlayed: null },
			{ id: 3, name: "anna", image: null, playCount: 2, winCount: 2, lastPlayed: "2026-10-01T18:00:00Z" },
			{ id: 2, name: "reggi", image: null, playCount: 10, winCount: 4, lastPlayed: "2026-10-30T18:00:00Z" },
		];
	});

	it("prefetches the players overview in the loader", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.query).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ["players", "overview"] }));
	});

	it("lists players by most recent play and groups those who haven't played", async () => {
		renderPage();

		const links = await screen.findAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual([
			"/players/2",
			"/players/3",
			"/sessions/new",
			"/players/1",
		]);
		expect(screen.getByRole("heading", { name: "list.not-played" })).toBeInTheDocument();
	});

	it("follows the sort and query from the URL", async () => {
		mocks.search = { sort: "name", q: "a" };
		renderPage();

		const links = await screen.findAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual(["/players/3"]);
		expect(screen.getByRole("button", { name: "list.sort-name" })).toHaveAttribute("aria-pressed", "true");
	});

	it("writes a picked sort to the URL", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "list.sort-sessions" }));

		const call = mocks.navigate.mock.calls.at(-1)?.[0];
		expect(call).toMatchObject({ to: "/players", replace: true });
		expect(call.search({ q: "x" })).toEqual({ q: "x", sort: "sessions" });
	});

	it("offers to clear a filter that matches nobody", async () => {
		const user = userEvent.setup();
		mocks.search = { q: "zzz" };
		renderPage();

		await user.click(await screen.findByRole("button", { name: "list.clear" }));

		expect(screen.getByText("list.no-match")).toBeInTheDocument();
		expect(mocks.navigate.mock.calls.at(-1)?.[0].search({ q: "zzz" })).toEqual({});
	});

	it("shows a retry state instead of the empty roster when loading fails", async () => {
		mocks.players = new Error("down");
		renderPage();

		expect(await screen.findByText("list.error-title")).toBeInTheDocument();
		expect(screen.queryByText("empty.title")).not.toBeInTheDocument();
	});

	it("opens the new player page from the header", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "player:new.button" }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/players/new" });
	});

	it("shows the empty state when there are no players", async () => {
		mocks.players = [];
		renderPage();

		expect(await screen.findByText("empty.title")).toBeInTheDocument();
	});
});
