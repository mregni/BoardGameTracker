import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtSelect } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChipButton } from "@/components/GameNight/AvatarChip";
import type { Game, Player, RunningGame, StartRunningGame } from "@/models";
import { toGameSelectItems } from "@/routes/sessions/-utils/gameSelectItems";
import { splitSuggested } from "@/routes/sessions/-utils/splitSuggested";
import { busyPlayers } from "../-utils/gameNightPlay";

interface Props {
	close: () => void;
	games: Game[];
	players: Player[];
	nightGameIds: number[];
	attendeeIds: number[];
	runningGames?: RunningGame[];
	initialGameId: number | null;
	initialPlayerIds: number[];
	isPending: boolean;
	onStart: (command: StartRunningGame) => Promise<unknown>;
}

export const StartGameSheet = (props: Props) => {
	const {
		close,
		games,
		players,
		nightGameIds,
		attendeeIds,
		runningGames = [],
		initialGameId,
		initialPlayerIds,
		isPending,
		onStart,
	} = props;
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

	const selectedGame = games.find((game) => game.id === gameId);
	const busy = busyPlayers(runningGames);
	const busyWarnings = selectedPlayers
		.filter((player) => busy.has(player.id))
		.map((player) => ({
			id: player.id,
			text: t("play.start.busy", {
				name: player.name,
				game: games.find((game) => game.id === busy.get(player.id))?.title ?? "",
			}),
		}));

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
				{selectedGame?.hasCoopMode && (
					<BgtText size="2" color="gray">
						{selectedGame.coopByDefault ? t("play.start.coop-default") : t("play.start.coop-optional")}
					</BgtText>
				)}
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
					{busyWarnings.map((warning) => (
						<BgtText key={warning.id} size="2" color="amber" role="status">
							{warning.text}
						</BgtText>
					))}
				</div>
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("play.start.save")}
					onConfirm={handleStart}
					disabled={gameId === null || playerIds.length === 0}
					loading={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
