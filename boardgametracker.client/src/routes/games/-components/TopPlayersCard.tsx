import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import TrendDownIcon from "@/assets/icons/trend-down.svg?react";
import TrendUpIcon from "@/assets/icons/trend-up.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtNoData } from "@/components/BgtNoData/BgtNoData";
import { type TopPlayer, Trend } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { RoundDecimal } from "@/utils/numberUtils";

interface Props {
	topPlayers: TopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
	const { topPlayers } = props;
	const { t } = useTranslation(["game", "common", "statistics"]);
	const { playerById } = usePlayerById();

	return (
		<BgtCard title={t("titles.top-players")} icon={Trophy}>
			{topPlayers.length === 0 ? (
				<BgtNoData />
			) : (
				<ul className="flex flex-col divide-y divide-primary/10">
					{topPlayers.map((player) => {
						const info = playerById(player.playerId);
						const details = [
							t("common:win", { count: player.wins }),
							t("common:game", { count: player.playCount }),
							player.averageScore != null
								? `${RoundDecimal(player.averageScore, 0.1)} ${t("statistics:average-abreviation")}`
								: null,
						]
							.filter(Boolean)
							.join(" • ");

						return (
							<li key={player.playerId} title={details}>
								<Link
									to="/players/$playerId"
									params={{ playerId: player.playerId }}
									className="flex items-center gap-2 py-2 hover:text-primary"
								>
									<BgtAvatar image={info?.image} title={info?.name} size="medium" />
									<span className="min-w-0 flex-1 truncate">{info?.name}</span>
									<span
										className={cx(
											"flex shrink-0 items-center gap-1 text-sm font-semibold",
											player.trend === Trend.Up && "text-green-400",
											player.trend === Trend.Down && "text-red-500",
											player.trend === Trend.Equal && "text-orange-400",
										)}
									>
										{player.trend === Trend.Up && <TrendUpIcon className="size-4" />}
										{player.trend === Trend.Down && <TrendDownIcon className="size-4" />}
										{RoundDecimal(player.winPercentage * 100, 0.1)}%
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
