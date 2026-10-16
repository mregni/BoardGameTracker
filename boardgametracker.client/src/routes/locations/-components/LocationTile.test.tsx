import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LocationTile } from "./LocationTile";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

describe("LocationTile", () => {
	it("links to the location detail page with the session count", () => {
		renderWithTheme(<LocationTile location={{ id: 3, name: "Thuis", playCount: 9 }} />);

		expect(screen.getByRole("link")).toHaveAttribute("href", "/locations/3");
		expect(screen.getByTitle("Thuis")).toBeInTheDocument();
		expect(screen.getByText("9")).toBeInTheDocument();
	});

	it("says there are no sessions yet for an unused location", () => {
		renderWithTheme(<LocationTile location={{ id: 4, name: "Bij Kathleen", playCount: 0 }} />);

		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
	});
});
