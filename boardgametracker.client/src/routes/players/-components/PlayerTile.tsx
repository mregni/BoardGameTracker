import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtTile } from "@/components/BgtTile/BgtTile";
import { BgtTileSessions } from "@/components/BgtTile/BgtTileSessions";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { PlayerOverview } from "@/models";

interface Props {
	player: PlayerOverview;
}

export const PlayerTile = (props: Props) => {
	const { player } = props;
	const { t } = useTranslation("common");
	const { ratio } = useNumberFormat();

	return (
		<BgtTile
			title={player.name}
			link={`/players/${player.id}`}
			visual={<BgtAvatar image={player.image} title={player.name} size="large" />}
			stats={
				<>
					{player.playCount > 0 && (
						<>
							<span className="font-semibold text-white">{ratio(player.winCount, player.playCount)}</span>{" "}
							{t("tile.wins")} ·{" "}
						</>
					)}
					<BgtTileSessions count={player.playCount} />
				</>
			}
		/>
	);
};
