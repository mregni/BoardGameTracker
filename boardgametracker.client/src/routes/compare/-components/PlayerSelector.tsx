import { memo, useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtSimpleSelect } from "@/components/BgtForm";

import type { Player } from "@/models";
import { PlayerAvatarWithCrown } from "./PlayerAvatarWithCrown";

interface PlayerSelectorProps {
	player: Player;
	players: Player[];
	excludeId?: number;
	isWinner: boolean;
	onPlayerChange: (playerId: number) => void;
}

const PlayerSelectorComponent = ({ player, players, excludeId, isWinner, onPlayerChange }: PlayerSelectorProps) => {
	const { t } = useTranslation("compare");

	const playerItems = useMemo(
		() => players.filter((p) => p.id !== excludeId).map((p) => ({ value: p.id, label: p.name, image: p.image })),
		[players, excludeId],
	);

	const handleValueChange = useCallback(
		(value: string | number) => {
			onPlayerChange(Number(value));
		},
		[onPlayerChange],
	);

	return (
		<div className="flex flex-col items-center">
			<PlayerAvatarWithCrown player={player} isWinner={isWinner} />
			<BgtSimpleSelect
				items={playerItems}
				showAvatars
				placeholder={t("select-player")}
				hasSearch={true}
				value={player.id}
				onValueChange={handleValueChange}
			/>
		</div>
	);
};

PlayerSelectorComponent.displayName = "PlayerSelector";

export const PlayerSelector = memo(PlayerSelectorComponent);
