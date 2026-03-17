import { useForm } from "@tanstack/react-form";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtImageSelector, BgtInputField } from "@/components/BgtForm";
import { CreatePlayerSchema, type ModalProps, type Player } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { usePlayerModal } from "../-hooks/usePlayerModal";

interface Props extends ModalProps {
	player: Player;
}

export const EditPlayerModal = (props: Props) => {
	const { open, close, player } = props;
	const { t } = useTranslation(["player", "common"]);
	const [image, setImage] = useState<File | undefined | null>(undefined);

	const { updatePlayer, uploadImage, isLoading } = usePlayerModal({});

	const form = useForm({
		defaultValues: {
			name: player.name,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreatePlayerSchema.parse(value);

			const updatedPlayer: Player = {
				...player,
				name: validatedData.name,
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
			<BgtDialogContent>
				<BgtDialogTitle>{t("update.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("update.description")}</BgtDialogDescription>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-row gap-3 mt-3 mb-6">
						<div className="flex-none">
							<BgtImageSelector image={image} setImage={setImage} defaultImage={player.image} />
						</div>
						<div className="grow">
							<form.Field
								name="name"
								validators={{
									onChange: ({ value }) => {
										const result = CreatePlayerSchema.shape.name.safeParse(value);
										if (!result.success) {
											return t(result.error.errors[0].message);
										}
										return undefined;
									},
								}}
							>
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
						</div>
					</div>
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={close} disabled={isLoading}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton type="submit" variant="primary" disabled={isLoading}>
							{t("update.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
