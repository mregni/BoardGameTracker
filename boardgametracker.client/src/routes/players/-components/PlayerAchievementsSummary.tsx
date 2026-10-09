import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import { BgtAchievementIcon } from "@/components/BgtAchievement/BgtAchievement";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import type { Badge } from "@/models";
import { useAchievementProgress } from "../-hooks/useAchievementProgress";
import { useBadgeProcessing } from "../-hooks/useBadgeProcessing";

interface Props {
	playerBadges: Badge[];
	badges: Badge[];
}

export const PlayerAchievementsSummary = (props: Props) => {
	const { playerBadges, badges } = props;
	const { t } = useTranslation("player");
	const { earnedCount, total } = useAchievementProgress(badges, playerBadges);
	const { displayBadges } = useBadgeProcessing(playerBadges);
	const percentage = total === 0 ? 0 : Math.round((earnedCount / total) * 100);

	return (
		<BgtCard title={`${t("titles.achievements")} (${earnedCount}/${total})`} icon={Award}>
			<div
				role="progressbar"
				aria-valuenow={earnedCount}
				aria-valuemin={0}
				aria-valuemax={total}
				aria-label={t("titles.achievements")}
				className="h-1.5 rounded-full bg-white/10"
			>
				<div className="h-1.5 rounded-full bg-primary" style={{ width: `${percentage}%` }} />
			</div>
			{displayBadges.length > 0 && (
				<div className="mt-3 flex flex-wrap gap-2">
					{displayBadges.map((badge) => (
						<BgtAchievementIcon key={badge.id} badge={badge} />
					))}
				</div>
			)}
		</BgtCard>
	);
};
