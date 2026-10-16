import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
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
			<BgtDialogContent size="wide">
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
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("create.save")}
					confirmType="submit"
					form="game-night-form"
					loading={isLoading}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
