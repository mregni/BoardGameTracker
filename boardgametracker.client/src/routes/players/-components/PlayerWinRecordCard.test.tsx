import { describe, expect, it, vi } from "vitest";
import { chartColors } from "@/models/Charts/PieChartDatum";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerWinRecordCard } from "./PlayerWinRecordCard";

const pie = vi.hoisted(() => ({ colors: [] as string[] }));

vi.mock("@nivo/pie", () => ({
	ResponsivePie: ({ data, colors }: { data: Array<{ id: string; color: string }>; colors: { datum: string } }) => {
		pie.colors = data.map((item) => (colors.datum === "data.color" ? item.color : ""));
		return <div data-testid="responsive-pie" />;
	},
}));

vi.mock("react-i18next", () => ({
	useTranslation: () => ({
		t: (key: string, options?: Record<string, unknown>) => (options ? `${key} ${JSON.stringify(options)}` : key),
	}),
}));

const swatchColor = (id: string) => screen.getByTestId(`legend-swatch-${id}`).style.backgroundColor;
const rgb = (hex: string) => {
	const value = Number.parseInt(hex.slice(1), 16);
	return `rgb(${value >> 16}, ${(value >> 8) & 255}, ${value & 255})`;
};

describe("PlayerWinRecordCard", () => {
	it("paints wins in the win colour and losses in the loss colour", () => {
		renderWithTheme(<PlayerWinRecordCard total={10} wins={7} />);

		expect(pie.colors).toEqual([chartColors.win, chartColors.loss]);
		expect(swatchColor("Wins")).toBe(rgb(chartColors.win));
		expect(swatchColor("Losses")).toBe(rgb(chartColors.loss));
	});

	it("describes the donut with the wins, losses and total", () => {
		renderWithTheme(<PlayerWinRecordCard total={10} wins={7} />);

		expect(
			screen.getByRole("img", { name: 'player:cards.win-record-summary {"wins":7,"losses":3,"total":10}' }),
		).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
		expect(screen.getByText("3")).toBeInTheDocument();
	});

	it("never shows negative losses", () => {
		renderWithTheme(<PlayerWinRecordCard total={2} wins={4} />);

		expect(screen.getByText("0")).toBeInTheDocument();
	});
});
