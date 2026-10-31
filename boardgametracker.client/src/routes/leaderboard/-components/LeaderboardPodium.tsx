import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CrownIcon from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { LeaderboardEntry, LeaderboardRankBy } from "@/models";

interface Props {
	players: LeaderboardEntry[];
	rankBy: LeaderboardRankBy;
}

const places = [
	{
		order: "order-2",
		avatar: "big",
		value: "text-2xl md:text-3xl",
		ring: "ring-gold",
		step: "h-36 border-gold/60 border-t-gold from-gold/15 to-gold/[0.03] text-gold md:h-40",
		delay: "motion-safe:[animation-delay:240ms]",
	},
	{
		order: "order-1",
		avatar: "large",
		value: "text-xl md:text-2xl",
		ring: "ring-primary-text/70",
		step: "h-32 border-primary/40 border-t-primary-text from-primary/30 to-primary/5 text-white md:h-36",
		delay: "motion-safe:[animation-delay:120ms]",
	},
	{
		order: "order-3",
		avatar: "large",
		value: "text-xl md:text-2xl",
		ring: "ring-primary/50",
		step: "h-28 border-primary/25 border-t-primary/70 from-primary/20 to-primary/5 text-white/80 md:h-[7.75rem]",
		delay: "",
	},
] as const;

const metricLabel: Record<LeaderboardRankBy, string> = {
	wins: "columns.wins",
	winRate: "columns.win-rate",
	sessions: "columns.sessions",
};

export const LeaderboardPodium = ({ players, rankBy }: Props) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();
	const top = players.filter((entry) => entry.isRanked).slice(0, places.length);

	if (top.length < 2) return null;

	const metric = (entry: LeaderboardEntry) => {
		if (rankBy === "winRate") return format.percent(entry.winPercentage / 100);
		if (rankBy === "sessions") return format.integer(entry.playCount);
		return format.integer(entry.winCount);
	};

	const details = (entry: LeaderboardEntry) => {
		const rate = format.percent(entry.winPercentage / 100);
		const wins = t("values.wins", { count: entry.winCount });
		const sessions = t("values.sessions", { count: entry.playCount });
		return [rankBy === "wins" ? rate : wins, rankBy === "sessions" ? rate : sessions].join(" · ");
	};

	return (
		<section aria-label={t("ranking.podium")} className="py-2 md:py-4">
			<ol
				className={cx(
					"isolate mx-auto grid w-full items-end gap-2 border-b-2 border-primary/40 sm:gap-4",
					top.length === 3 ? "max-w-3xl grid-cols-3" : "max-w-xl grid-cols-2",
				)}
			>
				{top.map((entry, index) => {
					const place = places[index];
					return (
						<li key={entry.playerId} className={cx("relative min-w-0", place.order)}>
							{index === 0 && (
								<span
									aria-hidden="true"
									className="pointer-events-none absolute -inset-x-1/4 -top-8 bottom-1/3 -z-10 bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--color-gold)_22%,transparent),transparent)]"
								/>
							)}
							<Link
								to="/players/$playerId"
								params={{ playerId: entry.playerId }}
								className="group flex min-w-0 flex-col items-center text-center"
							>
								<CrownIcon
									aria-hidden="true"
									className={cx("mb-1 size-6 text-gold md:size-7", index !== 0 && "invisible")}
								/>
								<span aria-hidden="true" className={cx("inline-flex rounded-full p-0.5 ring-2", place.ring)}>
									<BgtAvatar image={entry.image} title={entry.name} size={place.avatar} shape="round" />
								</span>
								<strong className="mt-2 mb-2 max-w-full truncate text-base group-hover:text-primary-text md:text-lg">
									{entry.name}
								</strong>
								<span
									className={cx(
										"flex w-full flex-col items-center rounded-t-lg border border-b-0 bg-linear-to-b px-1 pt-2 tabular-nums transition-colors group-hover:border-primary/60",
										"motion-safe:animate-podium-rise motion-safe:[animation-fill-mode:backwards]",
										place.step,
										place.delay,
									)}
								>
									<span className="text-4xl font-bold leading-none md:text-5xl">
										<span className="sr-only">{t("ranking.place")} </span>
										{entry.rank}
									</span>
									<span className={cx("mt-1 font-bold leading-tight text-card-value", place.value)}>
										{metric(entry)}
										<span className="sr-only"> {t(metricLabel[rankBy])}</span>
									</span>
									<small className="max-w-full truncate text-xs text-white/60">{details(entry)}</small>
								</span>
							</Link>
						</li>
					);
				})}
			</ol>
		</section>
	);
};
