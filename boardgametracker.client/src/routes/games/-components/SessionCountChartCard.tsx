import type { HtmlHTMLAttributes } from "react";
import { useTranslation } from "react-i18next";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtBarChart } from "@/components/BgtCharts/BgtBarChart";
import { transformSessionCountChartData } from "../-utils/gameDataTransformers";

interface PlayByDayItem {
	dayOfWeek: number;
	playCount: number;
}

interface Props extends HtmlHTMLAttributes<HTMLDivElement> {
	playByDayChart: PlayByDayItem[];
}

export const SessionCountChartCard = (props: Props) => {
	const { playByDayChart, className } = props;
	const { t } = useTranslation();

	const chartData = transformSessionCountChartData(playByDayChart, t);

	return (
		<BgtCard title={t("game.titles.session-count-per-day")} icon={BarChart} className={className}>
			<BgtBarChart index="day" keys={["sessions"]} data={chartData} />
		</BgtCard>
	);
};
