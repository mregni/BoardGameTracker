import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { type Badge, BadgeLevel } from "@/models";
import { type Feat, type Milestone, useAchievementProgress } from "../-hooks/useAchievementProgress";

const levelColors: Record<BadgeLevel, string> = {
	[BadgeLevel.green]: "bg-green-500",
	[BadgeLevel.blue]: "bg-blue-500",
	[BadgeLevel.red]: "bg-red-500",
	[BadgeLevel.gold]: "bg-yellow-500",
};

const MilestoneTile = ({ milestone }: { milestone: Milestone }) => {
	const { t } = useTranslation(["player", "badges"]);
	const shown = milestone.current ?? milestone.levels[0];
	const earned = milestone.current !== null;
	const goal = milestone.next
		? t("achievements.next", { goal: t(`badges:${milestone.next.descriptionKey}`) })
		: t("achievements.complete");

	return (
		<li className="flex items-center gap-3 rounded-lg border border-primary/10 bg-primary/5 p-2">
			<img
				src={`/images/badges/${shown.image}`}
				alt=""
				className={cx("size-9 shrink-0", !earned && "opacity-40 grayscale")}
			/>
			<div className="min-w-0 flex-1">
				<div className={cx("truncate", earned ? "text-white" : "text-white/50")}>{t(`badges:${shown.titleKey}`)}</div>
				<div className="truncate text-xs text-white/50" title={goal}>
					{goal}
				</div>
			</div>
			<div
				className="flex shrink-0 gap-0.5"
				role="img"
				aria-label={`${milestone.earnedLevels.length}/${milestone.levels.length}`}
			>
				{milestone.levels.map((level) => (
					<span
						key={level.id}
						className={cx(
							"h-1 w-2.5 rounded-full",
							level.level && milestone.earnedLevels.includes(level.level) ? levelColors[level.level] : "bg-white/10",
						)}
					/>
				))}
			</div>
		</li>
	);
};

const FeatItem = ({ feat }: { feat: Feat }) => {
	const { t } = useTranslation("badges");

	return (
		<li className="flex w-20 flex-col items-center gap-1 text-center" title={t(feat.badge.descriptionKey)}>
			<img
				src={`/images/badges/${feat.badge.image}`}
				alt=""
				className={cx("size-10", !feat.earned && "opacity-40 grayscale")}
			/>
			<span className={cx("text-xs leading-tight", feat.earned ? "text-white" : "text-white/50")}>
				{t(feat.badge.titleKey)}
			</span>
		</li>
	);
};

interface Props {
	playerBadges: Badge[];
	badges: Badge[];
}

export const PlayerAchievementsCard = (props: Props) => {
	const { badges, playerBadges } = props;
	const { t } = useTranslation("player");
	const { milestones, feats } = useAchievementProgress(badges, playerBadges);

	return (
		<BgtCard title={t("titles.achievements")} icon={Award}>
			<div className="flex flex-col gap-4">
				<section className="flex flex-col gap-2">
					<h3 className="text-sm text-primary/70">{t("achievements.milestones")}</h3>
					<ul className="grid grid-cols-1 gap-2 sm:grid-cols-2 2xl:grid-cols-4">
						{milestones.map((milestone) => (
							<MilestoneTile key={milestone.type} milestone={milestone} />
						))}
					</ul>
				</section>
				{feats.length > 0 && (
					<section className="flex flex-col gap-2">
						<h3 className="text-sm text-primary/70">{t("achievements.feats")}</h3>
						<ul className="flex flex-wrap gap-3">
							{feats.map((feat) => (
								<FeatItem key={feat.badge.id} feat={feat} />
							))}
						</ul>
					</section>
				)}
			</div>
		</BgtCard>
	);
};
