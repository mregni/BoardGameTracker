import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useCountUp } from "@/hooks/useCountUp";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { GameNightSummary, Player } from "@/models";

interface Props {
	summary: GameNightSummary;
	players: Player[];
	isOver: boolean;
}

export const NightSummaryStats = (props: Props) => {
	const { summary, players, isOver } = props;
	const { t } = useTranslation("game-nights");
	const shortDuration = useShortDuration();
	const gamesPlayed = useCountUp(summary.gamesPlayed);
	const totalMinutes = useCountUp(summary.totalMinutes);

	const winners = summary.winnerIds
		.map((id) => players.find((player) => player.id === id)?.name)
		.filter((name): name is string => name !== undefined)
		.join(", ");

	return (
		<div className="grid grid-cols-2 gap-3 md:grid-cols-3">
			<BgtTextStatistic title={t("summary.games-played")} content={gamesPlayed} textSize="4" />
			<BgtTextStatistic title={t("summary.play-time")} content={shortDuration(totalMinutes)} textSize="4" />
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
