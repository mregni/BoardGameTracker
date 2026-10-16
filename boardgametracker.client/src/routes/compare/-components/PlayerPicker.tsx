import { cva } from "class-variance-authority";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import CaretDown from "@/assets/icons/caret-down.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtSimpleSelect } from "@/components/BgtForm";
import type { CompareSide, Player } from "@/models";

const pickerVariants = cva(
	[
		"grid w-full cursor-pointer items-center rounded-xl border text-white transition-colors",
		"grid-cols-1 justify-items-center gap-1 px-1.5 py-2.5 text-center",
		"sm:grid-cols-[44px_minmax(0,1fr)_auto] sm:justify-items-stretch sm:gap-2.5 sm:px-3 sm:text-left",
	],
	{
		variants: {
			side: {
				playerOne: "border-player-one/30 bg-player-one/10 hover:border-player-one/60",
				playerTwo: "border-player-two/35 bg-player-two/10 hover:border-player-two/60",
			},
		},
	},
);

interface Props {
	side: CompareSide;
	player: Player;
	players: Player[];
	excludeId: number;
	summary: string;
	onPlayerChange: (playerId: number) => void;
}

export const PlayerPicker = ({ side, player, players, excludeId, summary, onPlayerChange }: Props) => {
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
			hasSearch
			value={player.id}
			onValueChange={(value) => onPlayerChange(Number(value))}
			aria-label={t(side === "playerOne" ? "change-left" : "change-right", { player: player.name })}
			triggerClassName={pickerVariants({ side })}
			trigger={
				<>
					<BgtAvatar image={player.image} title={player.name} size="large" />
					<span className="flex min-w-0 max-w-full flex-col">
						<strong className="truncate text-sm sm:text-base">{player.name}</strong>
						<small className="truncate text-xs text-white/60">{summary}</small>
					</span>
					<CaretDown aria-hidden="true" className="hidden size-5 text-white/60 sm:block" />
				</>
			}
		/>
	);
};
