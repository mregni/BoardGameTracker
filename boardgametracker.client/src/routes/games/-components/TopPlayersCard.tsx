import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import TrendDownIcon from "@/assets/icons/trend-down.svg?react";
import TrendUpIcon from "@/assets/icons/trend-up.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { type TopPlayer, Trend } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { RoundDecimal } from "@/utils/numberUtils";

interface Props {
	topPlayers: TopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
	const { topPlayers } = props;
	const { t } = useTranslation(["common", "game", "statistics"]);

	return (
		<BgtCard title={t("game:titles.top-players")} icon={Trophy}>
			<div className="flex flex-col gap-3">
				{topPlayers.map((player) => (
					<TopPlayerCardItem key={player.playerId} player={player} />
				))}
			</div>
		</BgtCard>
	);
};

interface ItemProps {
	player: TopPlayer;
}

const TopPlayerCardItem = (props: ItemProps) => {
	const { player } = props;
	const { t } = useTranslation(["common", "game", "statistics"]);
	const { playerById } = usePlayerById();
	const navigate = useNavigate();

	return (
		<div className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
			<BgtAvatar
				onClick={() => navigate({ to: `/players/${player.playerId}` })}
				image={playerById(player.playerId)?.image}
				title={playerById(player.playerId)?.name}
				size="large"
			/>
			<div className="flex-1">
				<BgtText color="white" className="uppercase">
					{playerById(player.playerId)?.name}
				</BgtText>
				<BgtText color="primary" opacity={70}>
					{t("win", { count: player.wins })} • {t("game", { count: player.playCount })}
				</BgtText>
			</div>
			<div className="text-right flex flex-col items-end">
				<div
					className={cx(
						"flex flex-row gap-2",
						player.trend === Trend.Up && "text-green-400",
						player.trend === Trend.Down && "text-red-500",
						player.trend === Trend.Equal && "text-orange-400",
					)}
				>
					{player.trend === Trend.Up && <TrendUpIcon className="size-5 mt-0.5" />}
					{player.trend === Trend.Down && <TrendDownIcon className="size-5 mt-0.5" />}
					{RoundDecimal(player.winPercentage * 100, 0.1)}%
				</div>
				{player.averageScore && (
					<div className="text-white/50 text-sm">
						{player.averageScore} {t("statistics:average-abreviation")}
					</div>
				)}
			</div>
		</div>
	);
};
