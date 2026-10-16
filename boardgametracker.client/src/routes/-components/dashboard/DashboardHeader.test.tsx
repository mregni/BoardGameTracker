import { describe, expect, it, vi } from "vitest";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen } from "@/test/test-utils";
import { DashboardHeader } from "./DashboardHeader";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

describe("DashboardHeader", () => {
	it("welcomes the linked player with this month's numbers", () => {
		renderWithTheme(
			<DashboardHeader
				linkedPlayer={{ id: 7, name: "Mikhaël", sessionsThisMonth: 9, winsThisMonth: 4 }}
				sessionsThisMonth={14}
				canWrite
			/>,
		);

		expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("welcome.title");
		expect(screen.getByText("welcome.linked")).toBeInTheDocument();
	});

	it("falls back to the group line without a linked player", () => {
		renderWithTheme(<DashboardHeader linkedPlayer={null} sessionsThisMonth={3} canWrite />);

		expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("common:dashboard");
		expect(screen.getByText("welcome.group")).toBeInTheDocument();
	});

	it("links to adding a game and logging a session for writers", () => {
		renderWithTheme(<DashboardHeader linkedPlayer={null} sessionsThisMonth={0} canWrite />);

		expect(screen.getByRole("link", { name: "actions.add-game" })).toHaveAttribute("href", "/games/add");
		expect(screen.getByRole("link", { name: "actions.log-session" })).toHaveAttribute("href", "/sessions/new");
	});

	it("hides the actions for readers", () => {
		renderWithTheme(<DashboardHeader linkedPlayer={null} sessionsThisMonth={0} canWrite={false} />);

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});
