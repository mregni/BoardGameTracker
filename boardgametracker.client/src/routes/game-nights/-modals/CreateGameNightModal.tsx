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
import type { CreateGameNight, Game, Location, ModalProps, Player } from "@/models";
import { GameNightForm, type GameNightFormValues } from "../-components/GameNightForm";

interface Props extends ModalProps {
	players: Player[];
	games: Game[];
	locations: Location[];
	isLoading: boolean;
	onSave: (gameNight: CreateGameNight) => Promise<void>;
}

export const CreateGameNightModal = (props: Props) => {
	const { open, close, players, games, locations, isLoading, onSave } = props;
	const { t } = useTranslation(["game-nights", "common"]);

	const handleSubmit = async (values: GameNightFormValues) => {
		const gameNight: CreateGameNight = {
			title: values.title,
			notes: values.notes,
			startDate: values.startDate,
			hostId: values.hostId,
			locationId: values.locationId,
			suggestedGameIds: values.suggestedGameIds,
			invitedPlayerIds: values.invitedPlayerIds,
		};
		await onSave(gameNight);
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-2xl!">
				<GameNightForm
					players={players}
					games={games}
					locations={locations}
					isLoading={isLoading}
					onSubmit={handleSubmit}
					onClose={close}
				>
					<BgtDialogTitle>{t("create.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("create.description")}</BgtDialogDescription>
				</GameNightForm>
				<BgtDialogClose>
					<BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={close}>
						{t("common:cancel")}
					</BgtButton>
					<BgtButton type="submit" form="game-night-form" disabled={isLoading} className="flex-1" variant="primary">
						{isLoading && <Bars className="size-4" />}
						{t("create.save")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
