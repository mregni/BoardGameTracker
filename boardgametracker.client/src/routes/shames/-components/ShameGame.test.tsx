import { describe, expect, it, vi } from "vitest";
import type { Shame } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { ShameGame } from "./ShameGame";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

const shame: Shame = {
	id: 7,
	title: "Dwar7s Winter",
	image: "/images/cover/Dwar7s Winter (2)_ab12cd.webp",
	additionDate: new Date(2022, 7, 30),
	price: 45,
	lastSessionDate: null,
};

describe("ShameGame", () => {
	it("links to the game and shows an uploaded cover with spaces and parentheses", () => {
		renderWithTheme(<ShameGame shame={shame} dateFormat="dd/MM/yyyy" currency="€" />);

		expect(screen.getByRole("link")).toHaveAttribute("href", "/games/7");
		expect(screen.getByRole("img", { name: "Dwar7s Winter" })).toHaveAttribute(
			"src",
			"/images/cover/Dwar7s Winter (2)_ab12cd.webp",
		);
	});

	it("shows the first letter on a colour when the game has no cover", () => {
		renderWithTheme(<ShameGame shame={{ ...shame, image: null }} dateFormat="dd/MM/yyyy" currency="€" />);

		expect(screen.getByRole("img", { name: "Dwar7s Winter" })).not.toHaveAttribute("src");
		expect(screen.getByText("D")).toBeInTheDocument();
	});
});
