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
		value: "text-3xl md:text-4xl",
		ring: "ring-gold",
		step: "h-32 border-gold/60 border-t-gold from-gold/15 to-gold/[0.03] text-gold md:h-36",
		delay: "motion-safe:[animation-delay:240ms]",
	},
	{
		order: "order-1",
		avatar: "large",
		value: "text-2xl md:text-3xl",
		ring: "ring-primary-text/70",
		step: "h-28 border-primary/40 border-t-primary-text from-primary/30 to-primary/5 text-white md:h-32",
		delay: "motion-safe:[animation-delay:120ms]",
	},
	{
		order: "order-3",
		avatar: "large",
		value: "text-2xl md:text-3xl",
		ring: "ring-primary/50",
		step: "h-[6.75rem] border-primary/25 border-t-primary/70 from-primary/20 to-primary/5 text-white/80 md:h-28",
		delay: "",
	},
] as const;

const metricLabel: Record<LeaderboardRankBy, string> = {
	wins: "columns.wins",
	winRate: "columns.win-rate",
	sessions: "columns.sessions",
};

export const podiumPlayers = (players: LeaderboardEntry[]) => {
	const top = players.filter((entry) => entry.isRanked).slice(0, places.length);
	return top.length < 2 ? [] : top;
};

export const LeaderboardPodium = ({ players, rankBy }: Props) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();
	const top = podiumPlayers(players);

	if (top.length === 0) return null;

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
			<h2 className="sr-only">{t("ranking.podium")}</h2>
			<ol
				className={cx(
					"isolate mx-auto grid w-full items-end gap-2 border-b-2 border-primary/40 sm:gap-4",
					top.length === 3 ? "max-w-3xl grid-cols-3" : "max-w-xl grid-cols-2",
				)}
			>
				{top.map((entry, index) => {
					const place = places[index];
					return (
						<li key={entry.playerId} className={cx("relative min-w-0", top.length === 3 && place.order)}>
							{index === 0 && (
								<span
									aria-hidden="true"
									className="pointer-events-none absolute -inset-x-1/4 -top-8 bottom-1/3 -z-10 bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--color-gold)_22%,transparent),transparent)]"
								/>
							)}
							<Link
								to="/players/$playerId"
								params={{ playerId: entry.playerId }}
								aria-label={`${t("ranking.place")} ${entry.rank}, ${entry.name}, ${metric(entry)} ${t(metricLabel[rankBy])}, ${details(entry)}`}
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
									<span className="text-sm font-bold leading-none">{entry.rank}</span>
									<span className={cx("mt-1 font-bold leading-none text-card-value", place.value)}>
										{metric(entry)}
									</span>
									<span className="mt-0.5 text-xs text-white/80">{t(metricLabel[rankBy])}</span>
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
