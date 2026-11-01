import { useTranslation } from "react-i18next";
import InfoIcon from "@/assets/icons/info.svg?react";
import type { Leaderboard, LeaderboardRankBy } from "@/models";

interface Props {
	leaderboard: Leaderboard;
}

const tieRule: Record<LeaderboardRankBy, string> = {
	wins: "ranking.ties-wins",
	winRate: "ranking.ties-win-rate",
	sessions: "ranking.ties-sessions",
};

export const LeaderboardRules = ({ leaderboard }: Props) => {
	const { t } = useTranslation("leaderboard");
	const showPodiums = leaderboard.players.some((entry) => entry.podiumCount > 0);
	const rules = [
		leaderboard.coopWinCount > 0
			? t("ranking.coop", { count: leaderboard.coopWinCount, total: leaderboard.winCount })
			: null,
		leaderboard.rankedCount > 1 ? t(tieRule[leaderboard.rankBy]) : null,
	]
		.filter(Boolean)
		.join(" ");

	if (!rules && !showPodiums) return null;

	return (
		<details className="self-end text-sm text-white/70 open:self-stretch">
			<summary className="ml-auto flex w-fit cursor-pointer list-none items-center gap-1.5 py-1 transition-colors hover:text-white [&::-webkit-details-marker]:hidden">
				<InfoIcon aria-hidden="true" className="size-4" />
				{t("ranking.how")}
			</summary>
			<div className="mt-1 flex max-w-[34rem] flex-col gap-1 rounded-lg border border-card-border bg-card px-3 py-2 text-xs leading-5 text-pretty md:ml-auto">
				{rules && <p>{rules}</p>}
				{showPodiums && <p>{t("columns.podiums-hint", { count: leaderboard.minimumPlayersForPodium })}</p>}
			</div>
		</details>
	);
};
