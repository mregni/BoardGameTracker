import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import Crown from "@/assets/icons/crown.svg?react";
import LeftRightArrow from "@/assets/icons/left-right-arrow.svg?react";
import Plus from "@/assets/icons/plus.svg?react";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtPoster } from "../../-components/BgtPoster";
import { usePlayerStanding } from "../-hooks/usePlayerStanding";

interface Props {
	playerId: number;
	playerName: string;
	image: string | null;
	playCount: number;
	canWrite: boolean;
	onEdit: () => void;
	onDelete: () => void;
}

const actionClasses = "inline-flex items-center gap-1.5 rounded-sm px-3 py-2 text-sm font-bold transition-colors";

export const PlayerHeader = (props: Props) => {
	const { playerId, playerName, image, playCount, canWrite, onEdit, onDelete } = props;
	const { t } = useTranslation("player");
	const standing = usePlayerStanding(playerId);

	return (
		<header className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-start gap-x-3 gap-y-3">
			<div className={cx("col-start-1 row-start-1 size-16", image && "lg:hidden")}>
				<BgtPoster title={playerName} image={image} />
			</div>
			<div
				className={cx(
					"col-span-3 row-start-2 flex min-w-0 flex-col gap-1 sm:col-span-1 sm:col-start-2 sm:row-start-1",
					image && "lg:col-span-2 lg:col-start-1",
				)}
			>
				<BgtHeading as="h1" size="8" className="line-clamp-none text-balance hyphens-auto max-sm:text-[28px]/[34px]">
					{playerName}
				</BgtHeading>
				{standing && (
					<Link
						to="/leaderboard"
						className="inline-flex w-fit items-center gap-1.5 py-1 font-semibold text-white/80 hover:text-white"
					>
						{standing.rank === 1 && <Crown className="size-5 text-gold" aria-hidden="true" />}
						{standing.isRanked
							? t("standing.ranked", { rank: standing.rank, count: standing.rankedCount })
							: t("standing.to-rank", { count: standing.sessionsToRank })}
					</Link>
				)}
			</div>
			{canWrite && (
				<div className="col-start-3 row-start-1 justify-self-end">
					<BgtEditDeleteButtons onDelete={onDelete} onEdit={onEdit} />
				</div>
			)}
			{playCount > 0 && (
				<div
					className={cx(
						"col-span-3 flex flex-wrap gap-2 sm:col-span-2 sm:col-start-2",
						image && "lg:col-span-3 lg:col-start-1",
					)}
				>
					{canWrite && (
						<Link
							to="/sessions/new"
							search={{ player: playerId }}
							className={cx(actionClasses, "bg-primary/60 text-white hover:bg-primary/50")}
						>
							<Plus className="size-4" aria-hidden="true" />
							{t("actions.log-session")}
						</Link>
					)}
					<Link
						to="/compare"
						search={{ left: playerId }}
						className={cx(
							actionClasses,
							"border border-primary/40 text-primary-text hover:border-primary hover:text-white",
						)}
					>
						<LeftRightArrow className="size-4" aria-hidden="true" />
						{t("actions.compare")}
					</Link>
				</div>
			)}
		</header>
	);
};
