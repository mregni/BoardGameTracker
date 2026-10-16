import { describe, expect, it, vi } from "vitest";
import type { DashboardGameNight } from "@/models";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen } from "@/test/test-utils";
import { NextGameNightCard } from "./NextGameNightCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const night: DashboardGameNight = {
	id: 3,
	title: "Friday-ish Strategy Night",
	startDate: "2026-10-23T19:30:00",
	locationName: "Thuis",
	hostId: 1,
	hostName: "Mikhaël",
	acceptedCount: 4,
	pendingCount: 1,
	declinedCount: 2,
	suggestedGames: [
		{ id: 11, title: "Azul", image: null },
		{ id: 12, title: "Brass: Birmingham", image: null },
	],
};

const renderCard = (overrides: Partial<Parameters<typeof NextGameNightCard>[0]> = {}) =>
	renderWithTheme(
		<NextGameNightCard
			gameNight={night}
			plannedGameNights={1}
			uiLanguage="en-US"
			timeFormat="HH:mm"
			canWrite
			{...overrides}
		/>,
	);

describe("NextGameNightCard", () => {
	it("shows the night with its date, details and RSVP pills", () => {
		renderCard();

		expect(screen.getByRole("heading", { name: "Friday-ish Strategy Night" })).toBeInTheDocument();
		expect(screen.getByText("Oct")).toBeInTheDocument();
		expect(screen.getByText("23")).toBeInTheDocument();
		expect(screen.getByText("night.details")).toBeInTheDocument();
		expect(screen.getByText("night.coming")).toBeInTheDocument();
		expect(screen.getByText("night.pending")).toBeInTheDocument();
		expect(screen.getByText("night.declined")).toBeInTheDocument();
	});

	it("hides empty RSVP pills", () => {
		renderCard({ gameNight: { ...night, pendingCount: 0, declinedCount: 0 } });

		expect(screen.getByText("night.coming")).toBeInTheDocument();
		expect(screen.queryByText("night.pending")).not.toBeInTheDocument();
		expect(screen.queryByText("night.declined")).not.toBeInTheDocument();
	});

	it("links the suggested games and opens the night", () => {
		renderCard();

		expect(screen.getByRole("link", { name: /Azul/ })).toHaveAttribute("href", "/games/11");
		expect(screen.getByRole("link", { name: /Brass: Birmingham/ })).toHaveAttribute("href", "/games/12");
		expect(screen.getByRole("link", { name: "night.open" })).toHaveAttribute("href", "/game-nights");
	});

	it("hides the suggestions without suggested games", () => {
		renderCard({ gameNight: { ...night, suggestedGames: [] } });

		expect(screen.queryByText("night.suggested")).not.toBeInTheDocument();
	});

	it("links to the other planned nights when there are more", () => {
		renderCard({ plannedGameNights: 3 });

		expect(screen.getByRole("link", { name: "night.more" })).toHaveAttribute("href", "/game-nights");
	});

	it("does not mention more nights when only one is planned", () => {
		renderCard();

		expect(screen.queryByText("night.more")).not.toBeInTheDocument();
	});

	it("invites writers to plan a night when none is planned", () => {
		renderCard({ gameNight: null, plannedGameNights: 0 });

		expect(screen.getByText("night.none")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "night.plan" })).toHaveAttribute("href", "/game-nights");
	});

	it("does not offer readers to plan a night", () => {
		renderCard({ gameNight: null, plannedGameNights: 0, canWrite: false });

		expect(screen.getByText("night.none")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});
