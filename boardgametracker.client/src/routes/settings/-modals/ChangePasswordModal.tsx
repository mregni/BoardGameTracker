import { useForm } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtInputField } from "@/components/BgtForm";
import type { ChangePasswordRequest } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";

interface Props {
	open: boolean;
	close: () => void;
	onSubmit: (request: ChangePasswordRequest) => Promise<void>;
	isLoading: boolean;
}

export const ChangePasswordModal = ({ open, close, onSubmit, isLoading }: Props) => {
	const { t } = useTranslation();

	const form = useForm({
		defaultValues: {
			currentPassword: "",
			newPassword: "",
			confirmPassword: "",
		},
		onSubmit: async ({ value }) => {
			if (value.newPassword !== value.confirmPassword) {
				return;
			}
			try {
				await onSubmit({
					currentPassword: value.currentPassword,
					newPassword: value.newPassword,
				});
			} finally {
				close();
			}
		},
	});

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("settings.account.password.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("settings.account.password.description")}</BgtDialogDescription>
					<div className="flex flex-col gap-2 mb-3 mt-3">
						<form.Field
							name="currentPassword"
							validators={{
								onChange: ({ value }) => {
									if (!value) return t("common.required", "Required");
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("settings.account.password.current.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field
							name="newPassword"
							validators={{
								onChange: ({ value }) => {
									if (!value) return t("common.required", "Required");
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("settings.account.password.new.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field
							name="confirmPassword"
							validators={{
								onChangeListenTo: ["newPassword"],
								onChange: ({ value, fieldApi }) => {
									const newPassword = fieldApi.form.getFieldValue("newPassword");
									if (value !== newPassword) {
										return t("settings.account.password.mismatch");
									}
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("settings.account.password.confirm.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={close} disabled={isLoading}>
							{t("common.cancel")}
						</BgtButton>
						<BgtButton variant="primary" type="submit" disabled={isLoading}>
							{t("settings.account.password.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
