import { cva } from "class-variance-authority";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import CaretDown from "@/assets/icons/caret-down.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtSimpleSelect } from "@/components/BgtForm";
import type { CompareSide, Player } from "@/models";

const pickerVariants = cva(
	[
		"grid w-full cursor-pointer items-center rounded-xl border-2 text-white shadow-lg transition-colors",
		"grid-cols-1 justify-items-center gap-1 px-1.5 py-3 text-center",
		"sm:grid-cols-[44px_minmax(0,1fr)_auto] sm:justify-items-stretch sm:gap-3 sm:px-4 sm:py-4 sm:text-left",
	],
	{
		variants: {
			side: {
				playerOne:
					"border-player-one/60 bg-linear-to-br from-player-one/30 to-player-one/5 shadow-player-one/20 hover:border-player-one",
				playerTwo:
					"border-player-two/60 bg-linear-to-bl from-player-two/30 to-player-two/5 shadow-player-two/20 hover:border-player-two",
			},
		},
	},
);

interface Props {
	side: CompareSide;
	player: Player;
	players: Player[];
	excludeId: number;
	onPlayerChange: (playerId: number) => void;
}

export const PlayerPicker = ({ side, player, players, excludeId, onPlayerChange }: Props) => {
	const { t } = useTranslation("compare");

	const items = useMemo(
		() =>
			players
				.filter((candidate) => candidate.id !== excludeId)
				.map((candidate) => ({ value: candidate.id, label: candidate.name, image: candidate.image })),
		[players, excludeId],
	);

	return (
		<BgtSimpleSelect
			items={items}
			showAvatars
			hasSearch={players.length > 8}
			value={player.id}
			onValueChange={(value) => onPlayerChange(Number(value))}
			aria-label={t(side === "playerOne" ? "change-left" : "change-right", { player: player.name })}
			triggerClassName={pickerVariants({ side })}
			trigger={
				<>
					<BgtAvatar
						image={player.image}
						title={player.name}
						color={`color-mix(in oklab, var(--color-${side === "playerOne" ? "player-one" : "player-two"}) 55%, black)`}
						size="large"
					/>
					<strong className="min-w-0 max-w-full truncate text-base font-extrabold sm:text-xl">{player.name}</strong>
					<CaretDown aria-hidden="true" className="hidden size-5 text-white/60 sm:block" />
				</>
			}
		/>
	);
};
