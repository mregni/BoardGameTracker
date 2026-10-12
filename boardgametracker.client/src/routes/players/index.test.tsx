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
	players: [] as PlayerOverview[],
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayersOverview: () => ({ queryKey: ["players", "overview"], queryFn: () => Promise.resolve(mocks.players) }),
}));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Players overview", () => {
	beforeEach(() => {
		mocks.navigate.mockReset();
		mocks.players = [
			{ id: 2, name: "reggi", image: null, playCount: 10, winCount: 4 },
			{ id: 1, name: "dfgh", image: null, playCount: 0, winCount: 0 },
		];
	});

	it("prefetches the players overview in the loader", () => {
		const queryClient = { prefetchQuery: vi.fn() };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.prefetchQuery).toHaveBeenCalledWith(
			expect.objectContaining({ queryKey: ["players", "overview"] }),
		);
	});

	it("shows a tile per player in the order the server returns", async () => {
		renderPage();

		const links = await screen.findAllByRole("link");
		expect(links.map((x) => x.getAttribute("href"))).toEqual(["/players/2", "/players/1"]);
		expect(screen.getByText("40%")).toBeInTheDocument();
		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
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
