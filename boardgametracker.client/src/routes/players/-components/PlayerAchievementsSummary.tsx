import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAchievementIcon } from "@/components/BgtAchievement/BgtAchievement";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import type { Badge } from "@/models";
import { useAchievementProgress } from "../-hooks/useAchievementProgress";
import { useRecentBadges } from "../-hooks/useRecentBadges";

export const ACHIEVEMENTS_ANCHOR = "player-achievements";

interface Props {
	playerBadges: Badge[];
	badges: Badge[];
}

export const PlayerAchievementsSummary = (props: Props) => {
	const { playerBadges, badges } = props;
	const { t } = useTranslation("player");
	const { earnedCount, total } = useAchievementProgress(badges, playerBadges);
	const recentBadges = useRecentBadges(playerBadges);

	return (
		<BgtCard title={`${t("titles.achievements")} (${earnedCount}/${total})`} icon={Award}>
			<BgtProgressBar size="xs" tone="primary" value={earnedCount} max={total} aria-label={t("titles.achievements")} />
			{recentBadges.length > 0 && (
				<ul className="mt-3 flex flex-wrap gap-2">
					{recentBadges.map((badge) => (
						<li key={badge.id}>
							<BgtAchievementIcon badge={badge} />
						</li>
					))}
				</ul>
			)}
			<a
				href={`#${ACHIEVEMENTS_ANCHOR}`}
				className="mt-3 inline-flex items-center gap-1 text-sm text-primary-text hover:text-indigo-300"
			>
				{t("achievements.see-all")}
				<CaretRight className="size-3.5" aria-hidden="true" />
			</a>
		</BgtCard>
	);
};
