import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

type Search = Record<string, unknown>;

const mocks = vi.hoisted(() => ({
	captured: {} as {
		config: {
			component: FC;
			validateSearch: (search: Search) => Search;
			loader: (args: { context: { queryClient: { query: () => Promise<unknown> } } }) => void;
		};
	},
	games: [] as unknown[],
	search: {} as Search,
	navigate: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: typeof mocks.captured.config) => {
		mocks.captured.config = config;
		return { ...config, fullPath: "/games/pick", useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("./-hooks/useGamesData", () => ({
	useGamesData: () => ({ games: mocks.games, isLoading: false }),
}));

await import("./pick");

const game = (id: number, overrides: Record<string, unknown> = {}) => ({
	id,
	title: `Game ${id}`,
	state: GameState.Owned,
	image: null,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 30,
	maxPlayTime: 30,
	categories: [{ id: 1, name: "Strategy" }],
	isLoaned: false,
	playCount: 0,
	lastPlayed: null,
	...overrides,
});

const renderPage = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

const lastSearch = () => {
	const [options] = mocks.navigate.mock.lastCall as [{ search: (prev: Search) => Search }];
	return options.search(mocks.search);
};

beforeEach(() => {
	mocks.games = [game(1), game(2), game(3, { isLoaned: true })];
	mocks.search = {};
	mocks.navigate.mockReset();
});

describe("Pick route search", () => {
	it("keeps valid wheel state and drops the rest", () => {
		expect(
			mocks.captured.config.validateSearch({ players: "4", wheel: [1, "2"], pick: "2", out: "x", fresh: "true" }),
		).toEqual({
			players: 4,
			playTime: undefined,
			category: undefined,
			wheel: [1, 2],
			pick: 2,
			out: undefined,
			fresh: true,
		});
		expect(mocks.captured.config.validateSearch({ pick: "-1", fresh: "no" })).toMatchObject({
			pick: undefined,
			fresh: undefined,
		});
	});

	it("starts loading the games in the loader", () => {
		const query = vi.fn(() => Promise.resolve());
		mocks.captured.config.loader({ context: { queryClient: { query } } });
		expect(query).toHaveBeenCalledOnce();
	});
});

describe("Pick page", () => {
	it("fills the wheel with playable games only", () => {
		renderPage();

		expect([...(lastSearch().wheel as number[])].sort()).toEqual([1, 2]);
	});

	it("shows the wheel from the URL and the filter summary", () => {
		mocks.search = { wheel: [1, 2], players: 4, category: "Strategy" };
		renderPage();

		expect(screen.getByRole("button", { name: /pick\.spin/ })).toBeInTheDocument();
		expect(mocks.navigate).not.toHaveBeenCalled();
		expect(screen.getByText(/Strategy/, { selector: "span" })).toBeInTheDocument();
	});

	it("names excluded games and puts one or all of them back", async () => {
		mocks.games = [game(1), game(2), game(4), game(5)];
		mocks.search = { wheel: [1, 2], out: [4, 5] };
		renderPage();

		await userEvent.click(screen.getAllByRole("button", { name: /pick\.restore-one/ })[0]);
		expect(lastSearch().out).toEqual([5]);

		await userEvent.click(screen.getByRole("button", { name: /pick\.restore$/ }));
		expect(lastSearch().out).toBeUndefined();
	});

	it("offers to loosen each set filter when nothing matches", async () => {
		mocks.search = { players: 8, category: "Strategy" };
		renderPage();

		expect(screen.getByText(/pick\.none$/)).toBeInTheDocument();
		const loosen = screen.getAllByRole("button", { name: /filters\.any/ });
		expect(loosen).toHaveLength(2);

		await userEvent.click(loosen[0]);
		expect(lastSearch()).toEqual({ category: "Strategy" });
	});

	it("sends the user to the games list when no game is playable at all", async () => {
		mocks.games = [game(3, { isLoaned: true })];
		renderPage();

		expect(screen.getByText(/pick\.none-owned/)).toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: /pick\.to-games/ }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games" });
	});

	it("turns on unplayed games first and redraws the wheel", async () => {
		mocks.search = { wheel: [1, 2], pick: 1 };
		renderPage();

		await userEvent.click(screen.getByRole("switch"));
		expect(lastSearch()).toEqual({ fresh: true });
	});
});
