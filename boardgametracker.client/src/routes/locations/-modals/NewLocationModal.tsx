import { useRef } from "react";
import { useTranslation } from "react-i18next";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtInputField } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import { CreateLocationSchema } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
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

	const fieldsRef = useRef<HTMLDivElement>(null);

	const focusName = (event: Event) => {
		const field = fieldsRef.current?.querySelector("input");
		if (field) {
			event.preventDefault();
			field.focus();
		}
	};

	const form = useAppForm({
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
			<BgtDialogContent onOpenAutoFocus={focusName}>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("new.title")}</BgtDialogTitle>
					<div ref={fieldsRef} className="flex flex-col gap-2 mb-3">
						<form.Field name="name" validators={zodValidator(CreateLocationSchema, "name")}>
							{(field) => <BgtInputField field={field} type="text" label={t("new.name.label")} disabled={isLoading} />}
						</form.Field>
					</div>
					<BgtDialogFooter
						onCancel={close}
						confirmLabel={t("new.save")}
						confirmType="submit"
						disabled={isLoading}
						cancelDisabled={isLoading}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
