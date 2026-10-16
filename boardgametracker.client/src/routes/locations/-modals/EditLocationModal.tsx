import { useTranslation } from "react-i18next";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtInputField } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import { CreateLocationSchema, type Location } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useLocationModal } from "../-hooks/useLocationModal";

interface Props {
	location: Location;
	open: boolean;
	close: () => void;
}

export const EditLocationModal = (props: Props) => {
	const { location, open, close } = props;
	const { t } = useTranslation(["location", "common"]);

	const { updateLocation, isLoading } = useLocationModal({
		onUpdateSuccess: close,
	});

	const form = useAppForm({
		defaultValues: {
			name: location.name,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateLocationSchema.parse(value);
			const updatedLocation: Location = {
				...location,
				name: validatedData.name,
			};
			await updateLocation(updatedLocation);
		},
	});

	if (location === null) return null;

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("edit.title")}</BgtDialogTitle>
					<div className="flex flex-col gap-2 mb-3">
						<form.Field name="name" validators={zodValidator(CreateLocationSchema, "name")}>
							{(field) => (
								<BgtInputField field={field} type="text" label={t("new.name.placeholder")} disabled={isLoading} />
							)}
						</form.Field>
					</div>
					<BgtDialogFooter
						onCancel={close}
						confirmLabel={t("common:save")}
						confirmType="submit"
						disabled={isLoading}
						cancelDisabled={isLoading}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
