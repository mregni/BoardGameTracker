import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPieChart } from "@/components/BgtCharts/BgtPieChart";
import { chartColors } from "@/models/Charts/PieChartDatum";

interface Props {
	total: number;
	wins: number;
}

export const PlayerWinRecordCard = ({ total, wins }: Props) => {
	const { t } = useTranslation(["common", "player"]);
	const losses = Math.max(total - wins, 0);

	const pieData = useMemo(
		() => [
			{
				id: "Wins",
				label: t("wins"),
				value: wins,
				color: chartColors.win,
			},
			{
				id: "Losses",
				label: t("losses"),
				value: losses,
				color: chartColors.loss,
			},
		],
		[wins, losses, t],
	);

	return (
		<BgtCard title={t("player:cards.win-record")} icon={Target}>
			<BgtPieChart
				data={pieData}
				showLegend
				tooltipPrefix="common:sessions"
				ariaLabel={t("player:cards.win-record-summary", { wins, losses, total: wins + losses })}
			/>
		</BgtCard>
	);
};
