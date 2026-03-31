import { useTranslation } from "react-i18next";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtBarChart } from "@/components/BgtCharts/BgtBarChart";
import { transformSessionCountChartData } from "@/routes/games/-utils/gameDataTransformers";

interface PlayByDayItem {
	dayOfWeek: number;
	playCount: number;
}

interface Props {
	playByDayChart: PlayByDayItem[];
}

export const SessionByDayCard = (props: Props) => {
	const { playByDayChart } = props;
	const { t } = useTranslation("game");

	const chartData = transformSessionCountChartData(playByDayChart, t);

	return (
		<BgtCard title={t("titles.session-count-per-day")} icon={BarChart}>
			<BgtBarChart index="day" keys={["sessions"]} data={chartData} />
		</BgtCard>
	);
};
