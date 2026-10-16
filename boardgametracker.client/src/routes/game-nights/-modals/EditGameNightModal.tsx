import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import type { Game, GameNight, Location, ModalProps, Player, UpdateGameNight } from "@/models";
import { GameNightForm, type GameNightFormValues } from "../-components/GameNightForm";
import { toUpdateGameNight } from "../-utils/toUpdateGameNight";

interface Props extends ModalProps {
	gameNight: GameNight | null;
	players: Player[];
	games: Game[];
	locations: Location[];
	isLoading: boolean;
	onSave: (gameNight: UpdateGameNight) => Promise<unknown>;
}

export const EditGameNightModal = (props: Props) => {
	const { open, close, gameNight, players, games, locations, isLoading, onSave } = props;
	const { t } = useTranslation(["game-nights", "common"]);

	if (!gameNight) return null;

	const handleSubmit = async (values: GameNightFormValues) => {
		await onSave({ id: gameNight.id, ...values });
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent size="wide">
				<GameNightForm
					defaultValues={toUpdateGameNight(gameNight)}
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
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("edit.save")}
					confirmType="submit"
					form="game-night-form"
					loading={isLoading}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
