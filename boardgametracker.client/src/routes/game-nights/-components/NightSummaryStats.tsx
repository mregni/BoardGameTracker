import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import type { GameNightSummary, Player } from "@/models";
import { formatMinutesToDuration } from "@/utils/dateUtils";

interface Props {
	summary: GameNightSummary;
	players: Player[];
	isOver: boolean;
	uiLanguage: string;
}

export const NightSummaryStats = (props: Props) => {
	const { summary, players, isOver, uiLanguage } = props;
	const { t } = useTranslation("game-nights");

	const winners = summary.winnerIds
		.map((id) => players.find((player) => player.id === id)?.name)
		.filter((name): name is string => name !== undefined)
		.join(", ");

	return (
		<div className="grid grid-cols-2 gap-3 md:grid-cols-3">
			<BgtTextStatistic title={t("summary.games-played")} content={summary.gamesPlayed} textSize="4" />
			<BgtTextStatistic
				title={t("summary.play-time")}
				content={formatMinutesToDuration(summary.totalMinutes, ["hours", "minutes"], uiLanguage)}
				textSize="4"
			/>
			<div className="col-span-2 md:col-span-1">
				<BgtTextStatistic
					title={isOver ? t("summary.night-winner") : t("summary.leading")}
					content={winners || t("summary.no-winner")}
					suffix={winners ? t("summary.wins", { count: summary.mostWins, total: summary.gamesPlayed }) : null}
					textSize="4"
				/>
			</div>
		</div>
	);
};
