import { describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";

import { fireEvent, renderWithTheme, screen } from "@/test/test-utils";
import { BgtImageCard } from "./BgtImageCard";

// i18next is mocked globally in setup.ts

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

describe("BgtImageCard", () => {
	const defaultProps = {
		title: "Catan",
		image: "/game.jpg",
		link: "/games/1",
	};

	describe("Rendering", () => {
		it("should render title", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} />);
			expect(screen.getByText("Catan")).toBeInTheDocument();
		});

		it("should render as link with correct href", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} />);
			const link = screen.getByRole("link");
			expect(link).toHaveAttribute("href", "/games/1");
		});

		it("should show a cover tile with the title when no image", () => {
			const { container } = renderWithTheme(<BgtImageCard {...defaultProps} image={null} />);
			expect(container.querySelector('[aria-hidden="true"][style*="--fallback-color"]')).toBeInTheDocument();
			expect(screen.getAllByText("Catan")).toHaveLength(2);
		});

		it("should render meta content", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} meta={<span>2–4</span>} />);
			expect(screen.getByText("2–4")).toBeInTheDocument();
		});
	});

	describe("Image Handling", () => {
		it("should render a lazy async decorative image when image provided", () => {
			const { container } = renderWithTheme(<BgtImageCard {...defaultProps} />);
			const img = container.querySelector("img");
			expect(img).toHaveAttribute("alt", "");
			expect(img).toHaveAttribute("src", "/game.jpg");
			expect(img).toHaveAttribute("loading", "lazy");
			expect(img).toHaveAttribute("decoding", "async");
		});

		it("should apply fallback color when no image", () => {
			const { container } = renderWithTheme(<BgtImageCard {...defaultProps} image={null} />);
			const imageDiv = container.querySelector('[style*="--fallback-color"]');
			expect(imageDiv).toBeInTheDocument();
		});

		it("should fall back to the cover tile when the image cannot be loaded", () => {
			const { container } = renderWithTheme(<BgtImageCard {...defaultProps} />);

			fireEvent.error(container.querySelector("img") as HTMLImageElement);

			expect(container.querySelector("img")).not.toBeInTheDocument();
			expect(container.querySelector('[style*="--fallback-color"]')).toBeInTheDocument();
		});
	});

	describe("Game State", () => {
		it("should render game state when not owned", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Wanted} />);
			expect(screen.getByText("game:state.wanted")).toBeInTheDocument();
		});

		it("should not render the owned state", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} />);
			expect(screen.queryByText("game:state.owned")).not.toBeInTheDocument();
		});

		it("should not render state when null", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={null as unknown as GameState} />);
			expect(screen.queryByText("game:state.")).not.toBeInTheDocument();
		});

		it("should not render state when undefined", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={undefined} />);
			expect(screen.queryByText("game:state.")).not.toBeInTheDocument();
		});
	});

	describe("Loaned State", () => {
		it("should not be loaned by default", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} />);
			expect(screen.queryByText("game:state.on-loan")).not.toBeInTheDocument();
		});

		it("should display loaned state when isLoaned is true", () => {
			renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} isLoaned={true} />);
			expect(screen.getByText("game:state.on-loan")).toBeInTheDocument();
		});
	});

	describe("Combined Props", () => {
		it("should handle all props together", () => {
			renderWithTheme(
				<BgtImageCard
					title="Wingspan"
					image="/wingspan.jpg"
					link="/games/2"
					state={GameState.ForTrade}
					isLoaned={false}
				/>,
			);

			expect(screen.getByText(/Wingspan/)).toBeInTheDocument();
			expect(screen.getByText("game:state.for-trade")).toBeInTheDocument();
			expect(screen.getByRole("link")).toHaveAttribute("href", "/games/2");
		});
	});
});
