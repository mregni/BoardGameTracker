import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, waitFor, within } from "@/test/test-utils";
import { ScoringResultsCard } from "./ScoringResultsCard";

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({
		playerById: (id: number) =>
			({ 1: { id: 1, name: "Kathleen", image: null }, 2: { id: 2, name: "Mikhaël", image: null } })[id],
	}),
}));

const chart = [
	{ key: "top-score", playerId: 1, score: 120 },
	{ key: "average", playerId: 2, score: 90.456 },
	{ key: "lowest", playerId: 2, score: -12 },
];

describe("ScoringResultsCard", () => {
	it("renders nothing without results", () => {
		const { container } = renderWithTheme(<ScoringResultsCard scoreRankChart={[]} />);

		expect(container).toBeEmptyDOMElement();
	});

	it("shows one labelled bar per result relative to the top score", async () => {
		renderWithTheme(<ScoringResultsCard scoreRankChart={chart} />);

		const top = screen.getByRole("progressbar", { name: "charts.top-scoring.top-score" });
		expect(top).toHaveAttribute("value", "120");
		expect(top).toHaveAttribute("max", "120");
		expect(top).toHaveAttribute("aria-valuetext", "120");
		expect(top.parentElement).toHaveClass("h-2", "bg-primary/10");
		await waitFor(() =>
			expect(within(top.parentElement as HTMLElement).getByTestId("progress-fill")).toHaveStyle({
				width: "100%",
				backgroundColor: "#10b981",
			}),
		);

		const average = screen.getByRole("progressbar", { name: "charts.top-scoring.average" });
		expect(average).toHaveAttribute("aria-valuetext", "90");
		expect(within(average.parentElement as HTMLElement).getByTestId("progress-fill")).toHaveStyle({
			width: "75.38%",
			backgroundColor: "#f59e0b",
		});
	});

	it("shows the player and the rounded score", () => {
		renderWithTheme(<ScoringResultsCard scoreRankChart={chart} />);

		expect(screen.getByText("Kathleen")).toBeInTheDocument();
		expect(screen.getAllByText("Mikhaël")).toHaveLength(2);
		expect(screen.getByText("90")).toBeInTheDocument();
	});

	it("clamps a negative score to an empty bar", () => {
		renderWithTheme(<ScoringResultsCard scoreRankChart={chart} />);

		const lowest = screen.getByRole("progressbar", { name: "charts.top-scoring.lowest" });
		expect(lowest).toHaveAttribute("value", "0");
		expect(within(lowest.parentElement as HTMLElement).getByTestId("progress-fill")).toHaveStyle({ width: "0%" });
	});
});
