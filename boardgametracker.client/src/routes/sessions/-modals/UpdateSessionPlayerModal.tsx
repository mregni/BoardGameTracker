import { Dialog } from "@radix-ui/themes";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtInputField, BgtSwitch } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import {
	type CreatePlayerSessionNoScoring,
	CreatePlayerSessionNoScoringSchema,
	CreatePlayerSessionSchema,
	type CreateSessionPlayer,
} from "@/models/";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";

interface Props {
	open: boolean;
	hasScoring: boolean;
	onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
	onCancel: () => void;
	selectedPlayerIds: number[];
	playerToEdit: CreateSessionPlayer | CreatePlayerSessionNoScoring | undefined;
}

const UpdateSessionPlayerForm = (props: Props) => {
	const { open, hasScoring, onClose, playerToEdit, onCancel } = props;
	const { t } = useTranslation(["player-session", "common"]);
	const { playerById } = usePlayerById();

	const schema = hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema;

	const form = useAppForm({
		defaultValues: {
			firstPlay: playerToEdit?.firstPlay ?? false,
			won: playerToEdit?.won ?? false,
			score: playerToEdit !== undefined && "score" in playerToEdit ? playerToEdit?.score : 0,
			playerId: playerToEdit?.playerId ?? "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = schema.parse(value);
			onClose(validatedData);
		},
	});

	return (
		<BgtDialog open={open}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("update.title")}</BgtDialogTitle>
				<Dialog.Description>
					{t("update.description", {
						name: playerById(playerToEdit?.playerId)?.name,
					})}
				</Dialog.Description>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-col gap-4 mt-3 mb-6">
						{hasScoring && (
							<form.Field name="score" validators={zodValidator(CreatePlayerSessionSchema, "score")}>
								{(field: AnyFieldApi) => <BgtInputField field={field} type="number" label={t("score.label")} />}
							</form.Field>
						)}
						<form.Field name="won" validators={zodValidator(schema, "won")}>
							{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("won.label")} className="mt-2" />}
						</form.Field>
						<form.Field name="firstPlay" validators={zodValidator(schema, "firstPlay")}>
							{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("first-play.label")} className="mt-2" />}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton type="button" variant="cancel" onClick={() => onCancel()}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton type="submit" variant="primary">
							{t("update.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};

export const UpdateSessionPlayerModal = (props: Props) => {
	return props.open && props.playerToEdit && <UpdateSessionPlayerForm {...props} />;
};
