import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { i18n as I18n } from "i18next";
import type { FC, ReactNode } from "react";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import TrendUp from "@/assets/icons/trend-up.svg?react";
import type { ComparePair, CompareResult, Player } from "@/models";
import { menuItems } from "@/routes/-hooks/useMenuInfo";
import { render, renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import {
	compareResult,
	createCompareI18n,
	gameRow,
	games,
	playerOne,
	playerTwo,
	reggi,
	translationFor,
} from "./-test/compareTestSetup";

const mocks = vi.hoisted(() => ({
	captured: {} as {
		config: { component: FC; validateSearch: { parse: (input: unknown) => { left?: number; right?: number } } };
	},
	search: {} as { left?: number; right?: number },
	navigate: vi.fn(),
	players: [] as Player[],
	getCompareCall: vi.fn(),
	getDefaultPairCall: vi.fn(),
	i18n: null as I18n | null,
}));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(mocks.i18n, namespace),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: typeof mocks.captured.config) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children }: { children: ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: false }),
}));

vi.mock("@/services/playerService", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/services/playerService")>()),
	getPlayersCall: () => Promise.resolve(mocks.players),
}));

vi.mock("@/services/settingsService", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/services/settingsService")>()),
	getSettingsCall: () => Promise.resolve({ dateFormat: "dd-MM-yyyy", uiLanguage: "en-US" }),
}));

vi.mock("@/services/compareService", () => ({
	getCompareCall: mocks.getCompareCall,
	getDefaultPairCall: mocks.getDefaultPairCall,
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

const deferred = <T,>() => {
	let resolve: (value: T) => void = () => undefined;
	const promise = new Promise<T>((done) => {
		resolve = done;
	});
	return { promise, resolve };
};

describe("Compare page", () => {
	beforeAll(async () => {
		mocks.i18n = await createCompareI18n("en-US");
	});

	it("keeps valid player ids in the search and drops invalid ones", () => {
		const { parse } = mocks.captured.config.validateSearch;
		expect(parse({ left: 1, right: 2 })).toEqual({ left: 1, right: 2 });
		expect(parse({ left: "abc", right: 2 })).toEqual({ right: 2 });
	});

	beforeEach(() => {
		mocks.search = {};
		mocks.players = [playerOne, playerTwo, reggi];
		mocks.navigate.mockReset();
		mocks.getCompareCall.mockReset().mockResolvedValue(compareResult());
		mocks.getDefaultPairCall.mockReset().mockResolvedValue({ playerOneId: 2, playerTwoId: 3, sessionCount: 8 });
	});

	it("reads Player vs player and uses the sidebar's compare icon in the page header", async () => {
		mocks.search = { left: 1, right: 2 };
		renderPage();

		const heading = await screen.findByRole("heading", { name: "Player vs player" });
		const headerIcon = heading.parentElement?.querySelector("svg");
		const { container } = render(<TrendUp className="text-primary size-7" />);

		expect(menuItems.find((item) => item.path === "/compare")?.icon).toBe(TrendUp);
		expect(headerIcon?.outerHTML).toBe(container.querySelector("svg")?.outerHTML);
	});

	it("waits for the pair with the most games together before asking for a comparison, then writes it to the URL", async () => {
		const pair = deferred<ComparePair | null>();
		mocks.getDefaultPairCall.mockReturnValue(pair.promise);
		renderPage();

		await waitFor(() => expect(mocks.getDefaultPairCall).toHaveBeenCalledTimes(1));
		expect(mocks.getCompareCall).not.toHaveBeenCalled();

		pair.resolve({ playerOneId: 2, playerTwoId: 3, sessionCount: 8 });

		await waitFor(() =>
			expect(mocks.navigate).toHaveBeenCalledWith({ to: "/compare", search: { left: 2, right: 3 }, replace: true }),
		);
		expect(mocks.getCompareCall).toHaveBeenCalledWith(2, 3);
		expect(mocks.getCompareCall).not.toHaveBeenCalledWith(0, expect.anything());
	});

	it("uses the players from the URL without asking for a default pair", async () => {
		mocks.search = { left: 3, right: 1 };
		renderPage();

		await waitFor(() => expect(mocks.getCompareCall).toHaveBeenCalledWith(3, 1));
		expect(mocks.getDefaultPairCall).not.toHaveBeenCalled();
		expect(mocks.navigate).not.toHaveBeenCalled();
	});

	it("fills in only the missing side", async () => {
		mocks.search = { left: 3 };
		mocks.getDefaultPairCall.mockResolvedValue({ playerOneId: 1, playerTwoId: 2, sessionCount: 4 });
		renderPage();

		await waitFor(() =>
			expect(mocks.navigate).toHaveBeenCalledWith({ to: "/compare", search: { left: 3, right: 1 }, replace: true }),
		);
	});

	it("falls back to the first two players when nobody played together", async () => {
		mocks.getDefaultPairCall.mockResolvedValue(null);
		renderPage();

		await waitFor(() =>
			expect(mocks.navigate).toHaveBeenCalledWith({ to: "/compare", search: { left: 1, right: 2 }, replace: true }),
		);
	});

	it("swaps the players", async () => {
		const user = userEvent.setup();
		mocks.search = { left: 1, right: 2 };
		renderPage();

		await user.click(await screen.findByRole("button", { name: "Swap players" }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/compare", search: { left: 2, right: 1 }, replace: true });
	});

	it("shows the record, the pickers with each player's summary and the collapsed overall table", async () => {
		mocks.search = { left: 1, right: 2 };
		renderPage();

		expect(await screen.findByRole("region", { name: "Record over all sessions together" })).toBeInTheDocument();
		expect(screen.getByRole("combobox", { name: "Change left player, now Mikhaël" })).toHaveTextContent(
			"142 sessions · 38% won",
		);
		expect(screen.getByRole("combobox", { name: "Change right player, now Kathleen" })).toHaveTextContent(
			"118 sessions · 41% won",
		);
		expect(screen.getByText("VS")).toBeInTheDocument();
		expect(screen.getByText("Overall, not only together").closest("details")).not.toHaveAttribute("open");
	});

	it("says when the two players never played together", async () => {
		mocks.search = { left: 1, right: 3 };
		mocks.getCompareCall.mockResolvedValue(
			compareResult({ totalSessionsTogether: 0, games: [], recentSessions: [] }) satisfies CompareResult,
		);
		renderPage();

		expect(await screen.findByText("No sessions together yet")).toBeInTheDocument();
		expect(screen.getByText("Mikhaël and reggi haven't played together yet.")).toBeInTheDocument();
		expect(screen.queryByRole("region", { name: "Record over all sessions together" })).toBeNull();
	});

	it("asks for more players when there are fewer than two", async () => {
		mocks.players = [playerOne];
		renderPage();

		expect(await screen.findByText("Not enough players to compare")).toBeInTheDocument();
		expect(mocks.getDefaultPairCall).not.toHaveBeenCalled();
		expect(mocks.getCompareCall).not.toHaveBeenCalled();
	});

	describe("changing players", () => {
		const renderStable = () => {
			const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
			const Component = mocks.captured.config.component;
			const view = render(
				<QueryClientProvider client={client}>
					<Component />
				</QueryClientProvider>,
			);
			return {
				rerender: () =>
					view.rerender(
						<QueryClientProvider client={client}>
							<Component />
						</QueryClientProvider>,
					),
			};
		};

		const rowFor = (title: string) =>
			screen.getAllByTestId("per-game-row").find((row) => row.querySelector("strong")?.textContent === title);

		it("keeps the previous comparison on screen, dimmed, while the next one loads", async () => {
			mocks.search = { left: 1, right: 2 };
			const next = deferred<CompareResult>();
			mocks.getCompareCall.mockImplementation((one: number, two: number) =>
				one === 1 && two === 2 ? Promise.resolve(compareResult()) : next.promise,
			);
			const { rerender } = renderStable();
			await screen.findByTestId("record-playerOne");

			mocks.search = { left: 1, right: 3 };
			rerender();

			await waitFor(() => expect(mocks.getCompareCall).toHaveBeenCalledWith(1, 3));
			expect(screen.getByTestId("record-playerOne")).toHaveTextContent("23");
			expect(screen.getByTestId("compare-content")).toHaveAttribute("aria-busy", "true");
			expect(screen.getByTestId("compare-content")).toHaveClass("opacity-60");
			expect(screen.getByRole("status", { name: "Updating the comparison" })).toBeInTheDocument();
			expect(screen.getByRole("combobox", { name: "Change right player, now reggi" })).toBeInTheDocument();
			expect(screen.getByText("wins for Kathleen")).toBeInTheDocument();

			next.resolve(compareResult({ directWins: { playerOne: 5, playerTwo: 2 } }));

			await waitFor(() => expect(screen.getByTestId("record-playerOne")).toHaveTextContent("5"));
			expect(screen.getByTestId("compare-content")).toHaveAttribute("aria-busy", "false");
			expect(screen.getByText("wins for reggi")).toBeInTheDocument();
		});

		it("keeps the same bar elements for a game both pairs played, and grows new games from zero", async () => {
			mocks.search = { left: 1, right: 2 };
			mocks.getCompareCall.mockImplementation((one: number, two: number) =>
				Promise.resolve(
					one === 1 && two === 2
						? compareResult()
						: compareResult({
								games: [
									gameRow({ gameId: 10, sessionCount: 4, playerOneWins: 3, playerTwoWins: 1 }),
									gameRow({ gameId: 15, sessionCount: 2, playerOneWins: 1, playerTwoWins: 1 }),
								],
							}),
				),
			);
			const { rerender } = renderStable();
			await screen.findByTestId("record-playerOne");
			const azulBefore = rowFor("Azul")?.querySelector('[data-testid="progress-segment"]');
			const recordBefore = screen.getByRole("region", { name: "Record over all sessions together" });
			await waitFor(() => expect(azulBefore).toHaveStyle({ flexGrow: "36.36" }));

			mocks.search = { left: 1, right: 3 };
			rerender();

			await waitFor(() => expect(rowFor("Cascadia")).toBeDefined());
			const azulAfter = rowFor("Azul")?.querySelector('[data-testid="progress-segment"]');
			expect(azulAfter).toBe(azulBefore);
			expect(azulAfter).toHaveStyle({ flexGrow: "75" });
			expect(screen.getByRole("region", { name: "Record over all sessions together" })).toBe(recordBefore);
			const cascadia = rowFor("Cascadia")?.querySelector('[data-testid="progress-segment"]');
			expect(cascadia).toHaveStyle({ flexGrow: "0" });
			await waitFor(() => expect(cascadia).toHaveStyle({ flexGrow: "50" }));
		});

		it("mirrors the same rows when the players are swapped", async () => {
			mocks.search = { left: 1, right: 2 };
			mocks.getCompareCall.mockImplementation((one: number) =>
				Promise.resolve(
					one === 1
						? compareResult()
						: compareResult({
								directWins: { playerOne: 17, playerTwo: 23 },
								games: [gameRow({ gameId: 10, sessionCount: 11, playerOneWins: 7, playerTwoWins: 4 })],
							}),
				),
			);
			const { rerender } = renderStable();
			await screen.findByTestId("record-playerOne");
			const first = rowFor("Azul")?.querySelector('[data-testid="progress-segment"]');

			mocks.search = { left: 2, right: 1 };
			rerender();

			await waitFor(() => expect(screen.getByTestId("record-playerOne")).toHaveTextContent("17"));
			const after = rowFor("Azul")?.querySelector('[data-testid="progress-segment"]');
			expect(after).toBe(first);
			expect(after).toHaveStyle({ flexGrow: "63.64" });
		});
	});
});
