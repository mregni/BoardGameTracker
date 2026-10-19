import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtImageSelector, BgtInputField } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import { CreatePlayerSchema, type ModalProps, type Player } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { usePlayerModal } from "../-hooks/usePlayerModal";

interface Props extends ModalProps {
	player: Player;
}

export const EditPlayerModal = (props: Props) => {
	const { open, close, player } = props;
	const { t } = useTranslation(["player", "common"]);
	const [image, setImage] = useState<File | undefined | null>(undefined);

	const { updatePlayer, uploadImage, isLoading } = usePlayerModal({});

	const form = useAppForm({
		defaultValues: {
			name: player.name,
			email: player.email ?? "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreatePlayerSchema.parse(value);

			const updatedPlayer: Player = {
				...player,
				name: validatedData.name,
				email: validatedData.email || null,
			};

			if (image !== undefined && image !== null) {
				const savedImage = await uploadImage({ type: 0, file: image });
				updatedPlayer.image = savedImage ?? null;
			} else if (image === null) {
				updatedPlayer.image = null;
			}

			await updatePlayer(updatedPlayer);
			close();
		},
	});

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent aria-describedby={undefined}>
				<BgtDialogTitle>{t("update.title")}</BgtDialogTitle>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-row gap-3 mt-3 mb-6">
						<div className="flex-none">
							<BgtImageSelector image={image} setImage={setImage} defaultImage={player.image} />
						</div>
						<div className="grow">
							<form.Field name="name" validators={zodValidator(CreatePlayerSchema, "name")}>
								{(field) => (
									<BgtInputField
										field={field}
										type="text"
										placeholder={t("name.placeholder")}
										label={t("common:name")}
										disabled={isLoading}
									/>
								)}
							</form.Field>
							<form.Field name="email" validators={zodValidator(CreatePlayerSchema, "email")}>
								{(field) => (
									<BgtInputField
										field={field}
										type="text"
										placeholder={t("email.placeholder")}
										label={t("email.label")}
										disabled={isLoading}
									/>
								)}
							</form.Field>
						</div>
					</div>
					<BgtDialogFooter
						onCancel={close}
						confirmLabel={t("update.save")}
						confirmType="submit"
						disabled={isLoading}
						cancelDisabled={isLoading}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
