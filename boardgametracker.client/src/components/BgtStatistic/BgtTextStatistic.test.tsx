import { describe, expect, it, vi } from "vitest";
import { render, renderWithTheme, screen } from "@/test/test-utils";
import { BgtTextStatistic } from "./BgtTextStatistic";

vi.mock("@/assets/icons/trophy.svg?react", () => ({
	default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="trophy-icon" {...props} />,
}));

describe("BgtTextStatistic", () => {
	describe("Rendering", () => {
		it("should render title", () => {
			renderWithTheme(<BgtTextStatistic title="Total Games" content={42} />);
			expect(screen.getByText("Total Games")).toBeInTheDocument();
		});

		it("should render content as string", () => {
			renderWithTheme(<BgtTextStatistic title="Best Game" content="Chess" />);
			expect(screen.getByText("Chess")).toBeInTheDocument();
		});

		it("should render content as number", () => {
			renderWithTheme(<BgtTextStatistic title="Wins" content={100} />);
			expect(screen.getByText("100")).toBeInTheDocument();
		});

		it("should format large numbers with locale", () => {
			const { container } = renderWithTheme(<BgtTextStatistic title="Points" content={1234567} />);
			// toLocaleString format varies by locale (1,234,567 or 1.234.567)
			const textContent = container.textContent;
			expect(textContent).toContain("1");
			expect(textContent).toContain("234");
			expect(textContent).toContain("567");
		});
	});

	describe("Null/Undefined Content", () => {
		it("should return null when content is null", () => {
			const { container } = render(<BgtTextStatistic title="Test" content={null} />);
			expect(container.firstChild).toBeNull();
		});

		it("should return null when content is undefined", () => {
			const { container } = render(<BgtTextStatistic title="Test" content={undefined as unknown as null} />);
			expect(container.firstChild).toBeNull();
		});
	});

	describe("Prefix and Suffix", () => {
		it("should render formatted price content as is", () => {
			renderWithTheme(<BgtTextStatistic title="Price" content="€63.33" />);
			expect(screen.getByText("€63.33")).toBeInTheDocument();
		});

		it("should render suffix", () => {
			renderWithTheme(<BgtTextStatistic title="Duration" content={45} suffix="min" />);
			expect(screen.getByText("min")).toBeInTheDocument();
		});

		it("should render a percentage suffix without a space", () => {
			const { container } = renderWithTheme(<BgtTextStatistic title="Win percentage" content={100} suffix="%" />);
			expect(container.textContent).toContain("100%");
		});

		it("should format numbers with the app language", () => {
			const { container } = renderWithTheme(<BgtTextStatistic title="Points" content={1234.5} />);
			expect(container.textContent).toContain("1,234.5");
		});

		it("should not render suffix when null", () => {
			renderWithTheme(<BgtTextStatistic title="Value" content={50} suffix={null} />);
			expect(screen.queryByText("null")).not.toBeInTheDocument();
		});
	});

	describe("Title", () => {
		it("should expose the full title as a tooltip", () => {
			renderWithTheme(<BgtTextStatistic title="Average session duration" content={5} />);
			expect(screen.getByText("Average session duration")).toHaveAttribute("title", "Average session duration");
		});
	});

	describe("Icon", () => {
		it("should render icon when provided", () => {
			const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="test-icon" {...props} />;
			renderWithTheme(<BgtTextStatistic title="Achievements" content={5} icon={<TestIcon />} />);
			expect(screen.getByTestId("test-icon")).toBeInTheDocument();
		});
	});

	describe("Edge Cases", () => {
		it("should handle zero content", () => {
			renderWithTheme(<BgtTextStatistic title="Losses" content={0} />);
			expect(screen.getByText("0")).toBeInTheDocument();
		});

		it("should handle empty string content", () => {
			renderWithTheme(<BgtTextStatistic title="Status" content="" />);
			expect(screen.getByText("Status")).toBeInTheDocument();
		});

		it("should handle negative numbers", () => {
			renderWithTheme(<BgtTextStatistic title="Change" content={-50} />);
			expect(screen.getByText("-50")).toBeInTheDocument();
		});

		it("should handle decimal numbers", () => {
			const { container } = renderWithTheme(<BgtTextStatistic title="Average" content={3.5} />);
			// toLocaleString may format as 3.5 or 3,5 depending on locale
			const textContent = container.textContent;
			expect(textContent).toContain("3");
			expect(textContent?.includes(".5") || textContent?.includes(",5")).toBe(true);
		});
	});
});
