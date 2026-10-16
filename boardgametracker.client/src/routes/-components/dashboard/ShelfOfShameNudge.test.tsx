import { describe, expect, it, vi } from "vitest";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen } from "@/test/test-utils";
import { ShelfOfShameNudge } from "./ShelfOfShameNudge";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

describe("ShelfOfShameNudge", () => {
	it("links to the shelf of shame with the count, never played and money", () => {
		renderWithTheme(
			<ShelfOfShameNudge shelfOfShame={{ count: 9, neverPlayedCount: 5, totalValue: 365 }} currency="EUR" />,
		);

		const link = screen.getByRole("link");
		expect(link).toHaveAttribute("href", "/shames");
		expect(link).toHaveTextContent("shame.title");
		expect(link).toHaveTextContent("shame.never-played · shame.paid");
	});

	it("leaves out the details that are empty", () => {
		renderWithTheme(
			<ShelfOfShameNudge shelfOfShame={{ count: 2, neverPlayedCount: 0, totalValue: null }} currency="EUR" />,
		);

		expect(screen.getByRole("link")).toHaveTextContent("shame.title");
		expect(screen.queryByText(/shame.never-played/)).not.toBeInTheDocument();
		expect(screen.queryByText(/shame.paid/)).not.toBeInTheDocument();
	});

	it("is hidden when the shelf is empty", () => {
		const { container } = renderWithTheme(
			<ShelfOfShameNudge shelfOfShame={{ count: 0, neverPlayedCount: 0, totalValue: null }} currency="EUR" />,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("is hidden when the feature is off", () => {
		const { container } = renderWithTheme(<ShelfOfShameNudge shelfOfShame={null} currency="EUR" />);

		expect(container).toBeEmptyDOMElement();
	});
});
