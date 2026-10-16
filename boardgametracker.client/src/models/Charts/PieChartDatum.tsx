import type { DefaultRawDatum } from "@nivo/pie";

export interface PieChartDatum extends DefaultRawDatum {
	label: string;
	color?: string;
}

export const chartColors = {
	win: "#34d399",
	loss: "#f87171",
	amber: "#f59e0b",
	blue: "#3b82f6",
} as const;

export const pieColors: string[] = [
	"#22d3ee",
	"#10b981",
	"#3b82f6",
	"#14b8a6",
	"#06b6d4",
	"#f59e0b",
	"#6366f1",
	"#84cc16",
	"#ef4444",
	"#f97316",
	"#0ea5e9",
	"#a3e635",
	"#eab308",
	"#8b5cf6",
	"#ec4899",
];
