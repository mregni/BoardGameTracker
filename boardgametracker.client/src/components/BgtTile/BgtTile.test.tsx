import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { BgtTile } from "./BgtTile";
import { BgtTileGrid } from "./BgtTileGrid";
import { BgtTileSessions } from "./BgtTileSessions";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to, className }: { children: React.ReactNode; to: string; className?: string }) => (
		<a href={to} className={className}>
			{children}
		</a>
	),
}));

const longName = "Café De Spelerij Gent-Sint-Pieters met een heel lange naam";

describe("BgtTile", () => {
	it("renders the whole tile as one link to the detail page", () => {
		renderWithTheme(<BgtTile title="Thuis" link="/locations/3" visual={<span>pin</span>} stats="9 sessions" />);

		const link = screen.getByRole("link");
		expect(link).toHaveAttribute("href", "/locations/3");
		expect(link).toHaveTextContent("Thuis");
		expect(link).toHaveTextContent("9 sessions");
		expect(link).toHaveTextContent("pin");
	});

	it("keeps the name on one line with the full name as a tooltip", () => {
		renderWithTheme(<BgtTile title={longName} link="/players/1" visual={null} stats="" />);

		const name = screen.getByTitle(longName);
		expect(name).toHaveTextContent(longName);
		expect(name).toHaveClass("truncate");
		expect(name.parentElement).toHaveClass("min-w-0");
	});

	it("shows a chevron so the tile reads as a link", () => {
		const { container } = renderWithTheme(<BgtTile title="Thuis" link="/locations/3" visual={null} stats="" />);

		expect(container.querySelector("svg[aria-hidden='true']")).toBeInTheDocument();
	});

	it("highlights the border on hover", () => {
		renderWithTheme(<BgtTile title="Thuis" link="/locations/3" visual={null} stats="" />);

		expect(screen.getByRole("link")).toHaveClass("cursor-pointer", "hover:border-primary/60");
	});
});

describe("BgtTileSessions", () => {
	it("says there are no sessions yet when the count is zero", () => {
		renderWithTheme(<BgtTileSessions count={0} prefix={<span>50% wins</span>} />);

		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
		expect(screen.queryByText("50% wins")).not.toBeInTheDocument();
	});

	it("shows the session count and the prefix when there are sessions", () => {
		const { container } = renderWithTheme(<BgtTileSessions count={9} prefix={<span>40% wins</span>} />);

		expect(screen.getByText("9")).toBeInTheDocument();
		expect(screen.getByText("40% wins")).toBeInTheDocument();
		expect(container).toHaveTextContent("40% wins · 9 tile.sessions");
	});
});

describe("BgtTileGrid", () => {
	it("uses one column on phones and an auto-fill grid on larger screens", () => {
		renderWithTheme(
			<BgtTileGrid data-testid="grid">
				<span />
			</BgtTileGrid>,
		);

		expect(screen.getByTestId("grid")).toHaveClass("grid-cols-1", "sm:grid-cols-[repeat(auto-fill,minmax(240px,1fr))]");
	});
});
