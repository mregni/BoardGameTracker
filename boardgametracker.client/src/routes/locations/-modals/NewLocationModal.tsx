import { useForm } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtInputField } from "@/components/BgtForm";
import { CreateLocationSchema } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { useLocationModal } from "../-hooks/useLocationModal";

interface Props {
	open: boolean;
	close: () => void;
}

export const NewLocationModal = (props: Props) => {
	const { open, close } = props;
	const { t } = useTranslation(["location", "common"]);

	const { saveLocation, isLoading } = useLocationModal({
		onSaveSuccess: close,
	});

	const form = useForm({
		defaultValues: {
			name: "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateLocationSchema.parse(value);
			await saveLocation(validatedData);
		},
	});

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("new.title")}</BgtDialogTitle>
					<div className="flex flex-col gap-2 mb-3">
						<form.Field
							name="name"
							validators={{
								onChange: ({ value }) => {
									const result = CreateLocationSchema.shape.name.safeParse(value);
									if (!result.success) {
										return t(result.error.errors[0].message);
									}
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField field={field} type="text" label={t("new.name.placeholder")} disabled={isLoading} />
							)}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={() => close()} disabled={isLoading}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton variant="primary" type="submit" disabled={isLoading}>
							{t("common:save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
