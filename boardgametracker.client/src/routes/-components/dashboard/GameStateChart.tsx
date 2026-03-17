import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import GamePad from "@/assets/icons/gamepad.svg?react";
import type { GameStateChart } from "@/models";

import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { BgtChartCard } from "../../../components/BgtCard/BgtChartCard";
import { BgtPieChart } from "../../../components/BgtCharts/BgtPieChart";

interface Props extends React.HTMLAttributes<HTMLDivElement> {
	data: GameStateChart[];
}

export const GameStateChartCard = (props: Props) => {
	const { data, className } = props;
	const { t } = useTranslation("dashboard");

	const pieData = useMemo(() => {
		if (data !== undefined) {
			return data
				.map((rank) => ({
					id: rank.type,
					label: t(getItemStateTranslationKey(rank.type, false)),
					value: rank.gameCount,
				}))
				.reverse();
		}
		return [];
	}, [data, t]);

	if (data === undefined) return null;

	return (
		<BgtChartCard title={t("charts.collection")} icon={GamePad} className={className}>
			<BgtPieChart data={pieData} showLegend tooltipPrefix="" />
		</BgtChartCard>
	);
};
