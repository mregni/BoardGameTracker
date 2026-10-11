import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtTile } from "@/components/BgtTile/BgtTile";
import { BgtTileSessions } from "@/components/BgtTile/BgtTileSessions";
import type { PlayerOverview } from "@/models";

interface Props {
	player: PlayerOverview;
}

const formatWinRate = (player: PlayerOverview, language: string): string => {
	const rate = player.playCount === 0 ? 0 : (player.winCount / player.playCount) * 100;
	try {
		return new Intl.NumberFormat(language || undefined, { maximumFractionDigits: 1 }).format(rate);
	} catch {
		return (Math.round(rate * 10) / 10).toString();
	}
};

export const PlayerTile = (props: Props) => {
	const { player } = props;
	const { t, i18n } = useTranslation("common");

	return (
		<BgtTile
			title={player.name}
			link={`/players/${player.id}`}
			visual={<BgtAvatar image={player.image} title={player.name} size="large" />}
			stats={
				<BgtTileSessions
					count={player.playCount}
					prefix={
						<>
							<span className="font-semibold text-white">{formatWinRate(player, i18n.language)}%</span> {t("tile.wins")}
						</>
					}
				/>
			}
		/>
	);
};
