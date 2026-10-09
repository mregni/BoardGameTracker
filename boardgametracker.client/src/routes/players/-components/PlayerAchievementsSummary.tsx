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

	return (
		<BgtCard title={`${t("titles.achievements")} (${earnedCount}/${total})`} icon={Award}>
			<progress
				value={earnedCount}
				max={Math.max(total, 1)}
				aria-label={t("titles.achievements")}
				className="block h-1.5 w-full appearance-none overflow-hidden rounded-full bg-white/10 [&::-moz-progress-bar]:rounded-full [&::-moz-progress-bar]:bg-primary [&::-webkit-progress-bar]:bg-white/10 [&::-webkit-progress-value]:rounded-full [&::-webkit-progress-value]:bg-primary"
			/>
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
