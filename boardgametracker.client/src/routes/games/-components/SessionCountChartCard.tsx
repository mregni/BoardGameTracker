import type { HtmlHTMLAttributes } from "react";
import { useTranslation } from "react-i18next";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtBarChart } from "@/components/BgtCharts/BgtBarChart";
import { transformSessionCountChartData } from "../-utils/gameDataTransformers";

const MIN_SESSIONS_FOR_DAY_CHART = 5;

interface PlayByDayItem {
	dayOfWeek: string;
	playCount: number;
}

interface Props extends HtmlHTMLAttributes<HTMLDivElement> {
	playByDayChart: PlayByDayItem[];
}

export const SessionCountChartCard = (props: Props) => {
	const { playByDayChart, className } = props;
	const { t, i18n } = useTranslation("game");

	const playCount = playByDayChart.reduce((total, day) => total + day.playCount, 0);
	if (playCount < MIN_SESSIONS_FOR_DAY_CHART) return null;

	const chartData = transformSessionCountChartData(playByDayChart, t, i18n.language);
	const shortDayByDay = new Map(chartData.map((item) => [item.day, item.shortDay]));

	return (
		<BgtCard title={t("titles.session-count-per-day")} icon={BarChart} className={className}>
			<BgtBarChart
				index="day"
				keys={["sessions"]}
				data={chartData}
				tickFormat={(value) => shortDayByDay.get(String(value)) ?? String(value)}
			/>
		</BgtCard>
	);
};
