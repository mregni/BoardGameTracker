import { ResponsivePie } from "@nivo/pie";
import { useTranslation } from "react-i18next";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { type PieChartDatum, pieColors } from "@/models/Charts/PieChartDatum";
import { BgtText } from "../BgtText/BgtText";

const theme = {
	text: {
		fontFamily: "Chakra Petch",
		fontSize: 15,
	},
	tooltip: {
		container: {
			background: "transparent",
			padding: 0,
		},
	},
};

interface Props {
	data: PieChartDatum[];
	showLegend?: boolean;
	tooltipPrefix: string;
	ariaLabel?: string;
}

export const BgtPieChart = (props: Props) => {
	const { data, showLegend = false, tooltipPrefix, ariaLabel } = props;
	const { t } = useTranslation();
	const { percent, integer } = useNumberFormat();

	if (!data || data.length === 0) return null;

	const coloredData = data.map((item, index) => ({
		...item,
		color: item.color ?? pieColors[index % pieColors.length],
	}));
	const total = data.reduce((sum, item) => sum + item.value, 0);
	const label =
		ariaLabel ?? `${data.map((item) => `${item.label}: ${item.value}`).join(", ")}, ${t("total")}: ${total}`;

	return (
		<>
			<div className="flex items-center flex-col justify-between h-full mb-4">
				<div className="relative w-48 h-48" role="img" aria-label={label}>
					<ResponsivePie
						data={coloredData}
						margin={{ top: 10, right: 10, bottom: 10, left: 10 }}
						innerRadius={0.6}
						padAngle={2}
						cornerRadius={3}
						activeOuterRadiusOffset={8}
						colors={{ datum: "data.color" }}
						enableArcLinkLabels={false}
						enableArcLabels={false}
						borderWidth={0}
						borderColor={{
							from: "color",
							modifiers: [["darker", 0.9]],
						}}
						theme={theme}
						tooltip={({ datum }) => {
							const percentage = percent(total === 0 ? 0 : datum.value / total, 1);
							return (
								<div className="bg-background border border-primary/30 rounded-lg p-3 shadow-lg min-w-40">
									<BgtText color="white">{datum.label}</BgtText>
									<BgtText color="cyan">
										{t(tooltipPrefix, { count: datum.value })} ({percentage})
									</BgtText>
								</div>
							);
						}}
					/>
					<div className="absolute inset-0 flex items-center justify-center flex-col pointer-events-none">
						<BgtText color="cyan" size="8">
							{integer(total)}
						</BgtText>
						<div className="text-white/50 text-xs">{t("total")}</div>
					</div>
				</div>
			</div>
			{showLegend && (
				<div className="space-y-2">
					{coloredData.map((item) => (
						<div key={String(item.id ?? item.label)} className="flex items-center justify-between">
							<div className="flex items-center gap-3">
								<div
									className="w-3 h-3 rounded-full"
									style={{ backgroundColor: item.color }}
									data-testid={`legend-swatch-${item.id}`}
								/>
								<span className="text-white/70 text-sm">{item.label}</span>
							</div>
							<BgtText color="white">{integer(item.value)}</BgtText>
						</div>
					))}
				</div>
			)}
		</>
	);
};
