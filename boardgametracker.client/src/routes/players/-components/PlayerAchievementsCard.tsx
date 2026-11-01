import { cx } from "class-variance-authority";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Award from "@/assets/icons/award.svg?react";
import { BgtAchievementIcon } from "@/components/BgtAchievement/BgtAchievement";
import BgtButton from "@/components/BgtButton/BgtButton";
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
				<div className="line-clamp-2 text-xs text-white/50" title={goal}>
					{goal}
				</div>
			</div>
			<div className="flex shrink-0 flex-col items-end gap-1">
				<span className="text-xs tabular-nums text-white/60">
					<span className="sr-only">
						{t("achievements.levels", {
							earned: milestone.earnedLevels.length,
							total: milestone.levels.length,
						})}
					</span>
					<span aria-hidden="true">
						{milestone.earnedLevels.length}/{milestone.levels.length}
					</span>
				</span>
				<div className="flex gap-0.5" aria-hidden="true">
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
			</div>
		</li>
	);
};

const FeatItem = ({ feat }: { feat: Feat }) => {
	const { t } = useTranslation(["badges", "player"]);

	return (
		<li className="flex w-20 flex-col items-center gap-1 text-center">
			<BgtAchievementIcon badge={feat.badge} lockedLabel={feat.earned ? undefined : t("player:achievements.locked")} />
			<span aria-hidden="true" className={cx("text-xs leading-tight", feat.earned ? "text-white" : "text-white/50")}>
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
	const [showLocked, setShowLocked] = useState(false);

	const earnedMilestones = milestones.filter((milestone) => milestone.current !== null);
	const earnedFeats = feats.filter((feat) => feat.earned);
	const lockedCount = milestones.length - earnedMilestones.length + feats.length - earnedFeats.length;
	const nothingEarned = earnedMilestones.length + earnedFeats.length === 0;
	const showAll = showLocked || nothingEarned;
	const shownMilestones = showAll ? milestones : earnedMilestones;
	const shownFeats = showAll ? feats : earnedFeats;

	return (
		<BgtCard title={t("titles.achievements")} icon={Award}>
			<div className="flex flex-col gap-4">
				{shownMilestones.length > 0 && (
					<section className="flex flex-col gap-2">
						<h3 className="text-sm text-stat-title">{t("achievements.milestones")}</h3>
						<ul className="grid grid-cols-1 gap-2 sm:grid-cols-2 2xl:grid-cols-4">
							{shownMilestones.map((milestone) => (
								<MilestoneTile key={milestone.type} milestone={milestone} />
							))}
						</ul>
					</section>
				)}
				{shownFeats.length > 0 && (
					<section className="flex flex-col gap-2">
						<h3 className="text-sm text-stat-title">{t("achievements.feats")}</h3>
						<ul className="flex flex-wrap gap-3">
							{shownFeats.map((feat) => (
								<FeatItem key={feat.badge.id} feat={feat} />
							))}
						</ul>
					</section>
				)}
				{lockedCount > 0 && !nothingEarned && (
					<BgtButton
						variant="text"
						size="1"
						className="self-start"
						aria-expanded={showLocked}
						onClick={() => setShowLocked((value) => !value)}
					>
						{showLocked ? t("achievements.hide-locked") : t("achievements.show-locked", { count: lockedCount })}
					</BgtButton>
				)}
			</div>
		</BgtCard>
	);
};
