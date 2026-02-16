import { useForm } from "@tanstack/react-form";
import { useState } from "react";
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
import { BgtImageSelector, BgtInputField } from "@/components/BgtForm";
import { CreatePlayerSchema, type ModalProps, type Player } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { usePlayerModal } from "../-hooks/usePlayerModal";

interface Props extends ModalProps {
	onPlayerCreated?: (player: Player) => void;
}

export const CreatePlayerModal = (props: Props) => {
	const { open, close, onPlayerCreated } = props;
	const { t } = useTranslation();
	const [image, setImage] = useState<File | undefined | null>(undefined);

	const { savePlayer, uploadImage, isLoading } = usePlayerModal({});

	const form = useForm({
		defaultValues: {
			name: "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreatePlayerSchema.parse(value);

			const player: Player = {
				id: 0,
				name: validatedData.name,
				image: null,
				badges: [],
			};

			if (image !== undefined) {
				const savedImage = await uploadImage({ type: 0, file: image });
				player.image = savedImage ?? null;
			}

			const savedPlayer = await savePlayer(player);
			if (onPlayerCreated) {
				onPlayerCreated(savedPlayer);
			}

			form.reset();
			setImage(undefined);
			close();
		},
	});

	const handleCancel = () => {
		form.reset();
		setImage(undefined);
		close();
	};

	return (
		<BgtDialog open={open} onClose={handleCancel}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("player.new.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("player.new.description")}</BgtDialogDescription>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-row gap-3 mt-3 mb-6">
						<div className="flex-none">
							<BgtImageSelector image={image} setImage={setImage} />
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
										placeholder={t("player.name.placeholder")}
										label={t("common.name")}
										disabled={isLoading}
									/>
								)}
							</form.Field>
						</div>
					</div>
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={handleCancel} disabled={isLoading}>
							{t("common.cancel")}
						</BgtButton>
						<BgtButton type="submit" variant="primary" disabled={isLoading} className="flex-1">
							{isLoading && <Bars className="size-4" />}
							{t("player.new.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
