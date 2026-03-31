import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import type { Game, GameNight, Location, ModalProps, Player } from "@/models";
import { GameNightForm, type GameNightFormValues } from "../-components/GameNightForm";

interface Props extends ModalProps {
	gameNight: GameNight | null;
	players: Player[];
	games: Game[];
	locations: Location[];
	isLoading: boolean;
	onSave: (gameNight: GameNight) => Promise<unknown>;
}

export const EditGameNightModal = (props: Props) => {
	const { open, close, gameNight, players, games, locations, isLoading, onSave } = props;
	const { t } = useTranslation(["game-nights", "common"]);

	if (!gameNight) return null;

	const handleSubmit = async (values: GameNightFormValues) => {
		await onSave({
			...gameNight,
			title: values.title,
			notes: values.notes,
			startDate: values.startDate,
			hostId: values.hostId,
			locationId: values.locationId,
			suggestedGames: games.filter((g) => values.suggestedGameIds.includes(g.id)),
			invitedPlayers: gameNight.invitedPlayers,
		});
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-2xl!">
				<GameNightForm
					defaultValues={{
						title: gameNight.title,
						startDate: new Date(gameNight.startDate),
						locationId: gameNight.locationId,
						hostId: gameNight.hostId,
						notes: gameNight.notes,
						suggestedGameIds: gameNight.suggestedGames.map((g) => g.id),
						invitedPlayerIds: gameNight.invitedPlayers
							.filter((p) => p.playerId !== gameNight.hostId)
							.map((p) => p.playerId),
					}}
					players={players}
					games={games}
					locations={locations}
					isLoading={isLoading}
					onSubmit={handleSubmit}
					onClose={close}
				>
					<BgtDialogTitle>{t("edit.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("edit.description")}</BgtDialogDescription>
				</GameNightForm>
				<BgtDialogClose>
					<BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={close}>
						{t("common:cancel")}
					</BgtButton>
					<BgtButton type="submit" form="game-night-form" disabled={isLoading} className="flex-1" variant="primary">
						{isLoading && <Bars className="size-4" />}
						{t("edit.save")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
