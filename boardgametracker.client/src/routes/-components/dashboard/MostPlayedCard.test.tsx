import { describe, expect, it, vi } from "vitest";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { MostPlayedCard } from "./MostPlayedCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const allTime = [
	{ id: 1, title: "Azul", image: null, totalSessions: 24 },
	{ id: 2, title: "Carcassonne", image: null, totalSessions: 21 },
];
const thisMonth = [{ id: 3, title: "Brass", image: null, totalSessions: 3 }];

describe("MostPlayedCard", () => {
	it("shows all time by default with links to the games", () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisMonth={thisMonth} />);

		expect(screen.getByRole("button", { name: "most-played.all-time" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "most-played.this-month" })).toHaveAttribute("aria-pressed", "false");
		expect(screen.getByRole("link", { name: /Azul/ })).toHaveAttribute("href", "/games/1");
		expect(screen.getByRole("link", { name: /Carcassonne/ })).toHaveAttribute("href", "/games/2");
		expect(screen.queryByText("Brass")).not.toBeInTheDocument();
	});

	it("switches to this month", async () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisMonth={thisMonth} />);

		await userEvent.click(screen.getByRole("button", { name: "most-played.this-month" }));

		expect(screen.getByRole("button", { name: "most-played.this-month" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("link", { name: /Brass/ })).toHaveAttribute("href", "/games/3");
		expect(screen.queryByText("Azul")).not.toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "most-played.all-time" }));

		expect(screen.getByText("Azul")).toBeInTheDocument();
	});

	it("shows the month empty state when nothing was played this month", async () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisMonth={[]} />);

		await userEvent.click(screen.getByRole("button", { name: "most-played.this-month" }));

		expect(screen.getByText("most-played.empty-month")).toBeInTheDocument();
	});

	it("shows the all time empty state without sessions", () => {
		renderWithTheme(<MostPlayedCard allTime={[]} thisMonth={[]} />);

		expect(screen.getByText("most-played.empty")).toBeInTheDocument();
	});
});
