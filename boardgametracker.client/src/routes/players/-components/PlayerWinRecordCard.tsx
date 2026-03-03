import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPieChart } from "@/components/BgtCharts/BgtPieChart";

interface Props {
	total: number;
	wins: number;
}

export const PlayerWinRecordCard = ({ total, wins }: Props) => {
	const { t } = useTranslation();

	const pieData = useMemo(() => {
		let losses = total - wins;
		if (losses <= 0) {
			losses = 0;
		}

		return [
			{
				id: "Wins",
				label: t("common.wins"),
				value: wins,
			},
			{
				id: "Losses",
				label: t("common.losses"),
				value: losses,
			},
		];
	}, [total, wins, t]);

	return (
		<BgtCard title={t("player.cards.win-record")} icon={Target}>
			<BgtPieChart data={pieData} showLegend tooltipPrefix="common.sessions" />
		</BgtCard>
	);
};
