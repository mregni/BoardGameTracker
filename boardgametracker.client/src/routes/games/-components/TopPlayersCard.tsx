import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import TrendDownIcon from "@/assets/icons/trend-down.svg?react";
import TrendUpIcon from "@/assets/icons/trend-up.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { type TopPlayer, Trend } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";

const TREND_WINDOW = 5;

interface Props {
	topPlayers: TopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
	const { topPlayers } = props;
	const { t } = useTranslation(["game", "common", "statistics"]);
	const { playerById } = usePlayerById();
	const { decimal, percent } = useNumberFormat();
	const winners = topPlayers.filter((player) => player.wins > 0);

	return (
		<BgtCard title={t("titles.top-players")} icon={Trophy}>
			{winners.length === 0 ? (
				<BgtNoData />
			) : (
				<ul className="flex flex-col divide-y divide-primary/10">
					{winners.map((player) => {
						const info = playerById(player.playerId);
						const details = [
							t("common:sessions", { count: player.playCount }),
							player.averageScore != null
								? `${decimal(player.averageScore)} ${t("statistics:average-abreviation")}`
								: null,
						]
							.filter(Boolean)
							.join(" · ");
						const trendLabel =
							player.trend === Trend.Up
								? t("top-players.trend-up", { sessions: TREND_WINDOW })
								: player.trend === Trend.Down
									? t("top-players.trend-down", { sessions: TREND_WINDOW })
									: null;

						return (
							<li key={player.playerId} title={details}>
								<Link
									to="/players/$playerId"
									params={{ playerId: player.playerId }}
									className="flex items-center gap-2 py-2 hover:text-primary-text"
								>
									<BgtAvatar image={info?.image} title={info?.name} size="medium" />
									<span className="min-w-0 flex-1 truncate">
										{info?.name}
										<span className="sr-only">, {details}</span>
									</span>
									<span className="shrink-0 text-sm font-semibold tabular-nums">
										{t("common:win", { count: player.wins })}
									</span>
									<span
										className={cx(
											"flex w-16 shrink-0 items-center justify-end gap-1 text-xs tabular-nums",
											player.trend === Trend.Up && "text-green-400",
											player.trend === Trend.Down && "text-bad",
											player.trend === Trend.Equal && "text-white/60",
										)}
									>
										{trendLabel && (
											<span role="img" aria-label={trendLabel} title={trendLabel} className="inline-flex">
												{player.trend === Trend.Up ? (
													<TrendUpIcon className="size-4" aria-hidden="true" />
												) : (
													<TrendDownIcon className="size-4" aria-hidden="true" />
												)}
											</span>
										)}
										{percent(player.winPercentage)}
									</span>
								</Link>
							</li>
						);
					})}
				</ul>
			)}
		</BgtCard>
	);
};
