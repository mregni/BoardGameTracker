import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CrownIcon from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { LeaderboardEntry, LeaderboardRankBy } from "@/models";

interface Props {
	players: LeaderboardEntry[];
	rankBy: LeaderboardRankBy;
}

const places = [
	{ order: "order-2", step: "h-[4.5rem] border-yellow-400/40 bg-yellow-400/10 text-yellow-400" },
	{ order: "order-1", step: "h-14 border-card-border bg-card-light text-white/60" },
	{ order: "order-3", step: "h-10 border-card-border bg-card-light text-white/60" },
];

export const LeaderboardPodium = ({ players, rankBy }: Props) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();

	const summary = (entry: LeaderboardEntry) => {
		const wins = t("values.wins", { count: entry.winCount });
		const rate = format.percent(entry.winPercentage / 100, 1);
		const sessions = t("values.sessions", { count: entry.playCount });
		if (rankBy === "winRate") return `${rate} · ${wins}`;
		if (rankBy === "sessions") return `${sessions} · ${rate}`;
		return `${wins} · ${rate}`;
	};

	return (
		<BgtCard aria-label={t("ranking.podium")} role="region">
			<ol className="mx-auto grid w-full max-w-xl grid-cols-3 items-end gap-3">
				{players.slice(0, places.length).map((entry, index) => {
					const { order, step } = places[index];
					return (
						<li key={entry.playerId} className={cx("min-w-0", order)}>
							<Link
								to="/players/$playerId"
								params={{ playerId: entry.playerId }}
								className="group grid min-w-0 justify-items-center gap-1 text-center"
							>
								{index === 0 && <CrownIcon aria-hidden="true" className="size-5 text-yellow-400" />}
								<BgtAvatar image={entry.image} title={entry.name} size="large" shape="round" />
								<strong className="max-w-full truncate group-hover:text-primary-text group-hover:underline">
									{entry.name}
								</strong>
								<small className="text-xs text-white/60">{summary(entry)}</small>
								<span
									className={cx(
										"grid w-full place-items-center rounded-t-lg border border-b-0 text-lg font-bold",
										step,
									)}
								>
									<span className="sr-only">{t("columns.rank")} </span>
									{entry.rank}
								</span>
							</Link>
						</li>
					);
				})}
			</ol>
		</BgtCard>
	);
};
