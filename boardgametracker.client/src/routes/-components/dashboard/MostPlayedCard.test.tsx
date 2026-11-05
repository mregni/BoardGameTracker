import { describe, expect, it, vi } from "vitest";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { MostPlayedCard } from "./MostPlayedCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const allTime = [
	{ id: 1, title: "Azul", image: null, totalSessions: 24 },
	{ id: 2, title: "Carcassonne", image: null, totalSessions: 21 },
];
const thisYear = [{ id: 3, title: "Brass", image: null, totalSessions: 3 }];
const thisMonth = [{ id: 4, title: "Cascadia", image: null, totalSessions: 7 }];

describe("MostPlayedCard", () => {
	it("offers the shared periods and shows all time by default with links to the games", () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisYear={thisYear} thisMonth={thisMonth} />);

		expect(screen.getAllByRole("button").map((button) => button.textContent)).toEqual([
			"common:period.all-time",
			"common:period.this-year",
			"common:period.this-month",
		]);
		expect(screen.getByRole("button", { name: "common:period.all-time" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("link", { name: /Azul/ })).toHaveAttribute("href", "/games/1");
		expect(screen.getByRole("link", { name: /Carcassonne/ })).toHaveAttribute("href", "/games/2");
		expect(screen.queryByText("Brass")).not.toBeInTheDocument();
	});

	it("switches between this year, this month and all time", async () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisYear={thisYear} thisMonth={thisMonth} />);

		await userEvent.click(screen.getByRole("button", { name: "common:period.this-year" }));
		expect(screen.getByRole("button", { name: "common:period.this-year" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("link", { name: /Brass/ })).toHaveAttribute("href", "/games/3");
		expect(screen.queryByText("Azul")).not.toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "common:period.this-month" }));
		expect(screen.getByRole("link", { name: /Cascadia/ })).toHaveAttribute("href", "/games/4");

		await userEvent.click(screen.getByRole("button", { name: "common:period.all-time" }));
		expect(screen.getByText("Azul")).toBeInTheDocument();
	});

	it("shows the period empty state when nothing was played in the period", async () => {
		renderWithTheme(<MostPlayedCard allTime={allTime} thisYear={[]} thisMonth={[]} />);

		await userEvent.click(screen.getByRole("button", { name: "common:period.this-year" }));

		expect(screen.getByText("most-played.empty-period")).toBeInTheDocument();
	});

	it("shows the all time empty state without sessions", () => {
		renderWithTheme(<MostPlayedCard allTime={[]} thisYear={[]} thisMonth={[]} />);

		expect(screen.getByText("most-played.empty")).toBeInTheDocument();
	});
});
