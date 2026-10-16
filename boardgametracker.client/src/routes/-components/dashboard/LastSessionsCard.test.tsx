import { describe, expect, it, vi } from "vitest";
import type { RecentActivity } from "@/models";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen, within } from "@/test/test-utils";
import { LastSessionsCard } from "./LastSessionsCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const sessions: RecentActivity[] = [
	{
		id: 1,
		gameId: 21,
		gameTitle: "Carcassonne",
		gameImage: null,
		start: "2026-10-16T19:00:00",
		playerCount: 4,
		durationInMinutes: 80,
		winners: [{ id: 2, name: "Kathleen", image: null }],
	},
	{
		id: 2,
		gameId: 22,
		gameTitle: "Pandemic",
		gameImage: null,
		start: "2026-10-13T19:00:00",
		playerCount: 2,
		durationInMinutes: 45,
		winners: [],
	},
];

describe("LastSessionsCard", () => {
	it("shows each session as a row that opens the game's sessions", () => {
		renderWithTheme(<LastSessionsCard sessions={sessions} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />);

		const links = screen.getAllByRole("link");
		expect(links).toHaveLength(2);
		expect(links[0]).toHaveAttribute("href", "/games/21/sessions");
		expect(links[1]).toHaveAttribute("href", "/games/22/sessions");
		expect(within(links[0]).getByText("Carcassonne")).toBeInTheDocument();
		expect(within(links[0]).getByText(/16-10-2026/)).toBeInTheDocument();
	});

	it("shows the winner as a pill and a neutral pill without a winner", () => {
		renderWithTheme(<LastSessionsCard sessions={sessions} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />);

		const [first, second] = screen.getAllByRole("link");
		expect(within(first).getByText("Kathleen")).toBeInTheDocument();
		expect(within(first).getByRole("img", { name: "common:won" })).toBeInTheDocument();
		expect(within(second).getByText("last-sessions.no-winner")).toBeInTheDocument();
	});

	it("shows an empty state without sessions", () => {
		renderWithTheme(<LastSessionsCard sessions={[]} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />);

		expect(screen.getByText("last-sessions.empty")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});
