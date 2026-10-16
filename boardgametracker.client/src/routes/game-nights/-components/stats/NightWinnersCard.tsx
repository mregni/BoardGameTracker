import { useTranslation } from "react-i18next";
import CrownIcon from "@/assets/icons/crown.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { NightWinnerStatistic } from "@/models";
import { StatRow, StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

interface Props {
	winners: NightWinnerStatistic[];
	tiedNights: number;
	lookup: StatsLookup;
}

export const NightWinnersCard = ({ winners, tiedNights, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const format = useNumberFormat();

	return (
		<StatsCard id="night-winners" title={t("stats.winners.title")} subtitle={t("stats.winners.subtitle")}>
			{winners.length === 0 ? (
				<StatsEmpty>{t("stats.winners.empty")}</StatsEmpty>
			) : (
				<>
					<div className="flex flex-col gap-1">
						{winners.map((winner) => {
							const player = lookup.players.get(winner.playerId);
							const name = player?.name ?? "";
							const share = format.ratio(winner.nightsWon, winner.nightsAttended);
							return (
								<StatRow
									key={winner.playerId}
									rank={winner.rank}
									highlight={winner.rank === 1}
									avatar={<BgtAvatar title={name} image={player?.image} size="medium" shape="round" />}
									name={name}
									detail={[
										t("stats.winners.nights", { count: winner.nightsAttended }),
										t("stats.winners.games-won", { count: winner.gamesWon }),
									].join(" · ")}
									bar={
										<BgtProgressBar
											value={winner.nightsWon}
											max={winner.nightsAttended}
											size="thin"
											aria-label={t("stats.winners.bar", {
												name,
												won: winner.nightsWon,
												attended: winner.nightsAttended,
											})}
										/>
									}
									value={
										<>
											<span className="inline-flex items-center gap-1">
												{format.integer(winner.nightsWon)}
												<CrownIcon className="size-3.5 text-gold" aria-hidden="true" />
											</span>
											<span className="block text-[11px] font-normal text-gray-400">
												{t("stats.winners.share", { value: share })}
											</span>
										</>
									}
								/>
							);
						})}
					</div>
					<p className="text-xs text-gray-400">
						{t("stats.winners.footer")}
						{tiedNights > 0 && ` ${t("stats.winners.ties", { count: tiedNights })}`}
					</p>
				</>
			)}
		</StatsCard>
	);
};
