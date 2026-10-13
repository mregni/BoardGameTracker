import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import XIcon from "@/assets/icons/x.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtSelect } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChipButton } from "@/components/GameNight/AvatarChip";
import type { Game, Player, StartRunningGame } from "@/models";
import { toGameSelectItems } from "@/routes/sessions/-utils/gameSelectItems";
import { splitSuggested } from "@/routes/sessions/-utils/splitSuggested";

interface Props {
	close: () => void;
	games: Game[];
	players: Player[];
	nightGameIds: number[];
	attendeeIds: number[];
	initialGameId: number | null;
	initialPlayerIds: number[];
	isPending: boolean;
	onStart: (command: StartRunningGame) => Promise<unknown>;
}

export const StartGameSheet = (props: Props) => {
	const { close, games, players, nightGameIds, attendeeIds, initialGameId, initialPlayerIds, isPending, onStart } =
		props;
	const { t } = useTranslation(["game-nights", "player-session", "common"]);
	const [gameId, setGameId] = useState<number | null>(initialGameId);
	const [playerIds, setPlayerIds] = useState<number[]>(initialPlayerIds);

	const gameItems = useMemo(
		() => toGameSelectItems(games, nightGameIds, t("play.games.tonight"), t("player-session:form.game.all")),
		[games, nightGameIds, t],
	);

	const playerItems = useMemo(() => {
		const available = players.filter((player) => !playerIds.includes(player.id));
		const { suggested, rest } = splitSuggested(available, attendeeIds);
		return [...suggested, ...rest].map((player) => ({ value: player.id, label: player.name, image: player.image }));
	}, [players, playerIds, attendeeIds]);

	const selectedPlayers = playerIds
		.map((id) => players.find((player) => player.id === id))
		.filter((player): player is Player => player !== undefined);

	const handleStart = async () => {
		if (gameId === null) return;
		await onStart({ gameId, playerIds });
		close();
	};

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent size="sheet">
				<BgtDialogTitle>{t("play.start.title")}</BgtDialogTitle>
				<BgtSelect
					value={gameId}
					onValueChange={(value) => setGameId(Number(value))}
					items={gameItems}
					hasSearch
					label={t("play.start.game")}
					placeholder={t("player-session:new.game.placeholder")}
				/>
				<div className="flex flex-col gap-2">
					<BgtSelect
						value={null}
						onValueChange={(value) => setPlayerIds((ids) => [...ids, Number(value)])}
						items={playerItems}
						hasSearch
						showAvatars
						label={t("play.start.players")}
						placeholder={t("player-session:form.players.search")}
					/>
					{selectedPlayers.length > 0 && (
						<div className="flex flex-col gap-1.5">
							<BgtText size="2" color="gray">
								{t("play.leave-out-hint")}
							</BgtText>
							<div className="flex flex-wrap gap-2">
								{selectedPlayers.map((player) => (
									<AvatarChipButton
										key={player.id}
										title={player.name}
										image={player.image}
										label={t("play.leave-out", { name: player.name })}
										onClick={() => setPlayerIds((ids) => ids.filter((id) => id !== player.id))}
									>
										<XIcon className="size-3.5 text-white/60" />
									</AvatarChipButton>
								))}
							</div>
						</div>
					)}
				</div>
				<BgtDialogClose>
					<BgtButton variant="cancel" onClick={close}>
						{t("common:cancel")}
					</BgtButton>
					<BgtButton
						variant="primary"
						className="flex-1"
						disabled={gameId === null || playerIds.length === 0 || isPending}
						onClick={handleStart}
					>
						{t("play.start.save")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
