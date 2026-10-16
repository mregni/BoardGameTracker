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
	games,
	playerOne,
	playerTwo,
	reggi,
	translationFor,
} from "./-test/compareTestSetup";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
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
	createFileRoute: () => (config: { component: FC }) => {
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

		expect(await screen.findByRole("region", { name: "Record over all games together" })).toBeInTheDocument();
		expect(screen.getByRole("combobox", { name: "Change left player, now Mikhaël" })).toHaveTextContent(
			"142 games · 38% won",
		);
		expect(screen.getByRole("combobox", { name: "Change right player, now Kathleen" })).toHaveTextContent(
			"118 games · 41% won",
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

		expect(await screen.findByText("No games together yet")).toBeInTheDocument();
		expect(screen.getByText("Mikhaël and reggi haven't played a game together yet.")).toBeInTheDocument();
		expect(screen.queryByRole("region", { name: "Record over all games together" })).toBeNull();
	});

	it("asks for more players when there are fewer than two", async () => {
		mocks.players = [playerOne];
		renderPage();

		expect(await screen.findByText("Not enough players to compare")).toBeInTheDocument();
		expect(mocks.getDefaultPairCall).not.toHaveBeenCalled();
		expect(mocks.getCompareCall).not.toHaveBeenCalled();
	});
});
