import { useForm } from "@tanstack/react-form";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtInputField, BgtSelect } from "@/components/BgtForm";
import { type BgtSelectItem, isApiError, type RegisterRequest } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";

interface Props {
	open: boolean;
	close: () => void;
	onSubmit: (request: RegisterRequest) => Promise<unknown>;
	isLoading: boolean;
}

export const CreateUserModal = ({ open, close, onSubmit, isLoading }: Props) => {
	const { t } = useTranslation(["settings", "common"]);
	const [error, setError] = useState<string | null>(null);

	const roleItems: BgtSelectItem[] = useMemo(
		() => [
			{ value: "User", label: t("account.users.roles.user") },
			{ value: "Reader", label: t("account.users.roles.reader") },
			{ value: "Admin", label: t("account.users.roles.admin") },
		],
		[t],
	);

	const form = useForm({
		defaultValues: {
			username: "",
			email: "",
			password: "",
			role: "User",
		},
		onSubmit: async ({ value }) => {
			setError(null);
			try {
				await onSubmit({
					username: value.username,
					email: value.email,
					password: value.password,
					role: value.role,
				});
				close();
			} catch (e) {
				setError(isApiError(e) ? t(e.message) : t("account.notifications.user-create-failed"));
			}
		},
	});

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("account.users.create.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("account.users.create.description")}</BgtDialogDescription>
					<div className="flex flex-col gap-2 mb-3 mt-3">
						<form.Field
							name="username"
							validators={{
								onChange: ({ value }) => {
									if (!value) return t("common:required", "Required");
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("account.users.create.username.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field
							name="email"
							validators={{
								onChange: ({ value }) => {
									if (!value) return t("common:required", "Required");
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("account.users.create.email.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field
							name="password"
							validators={{
								onChange: ({ value }) => {
									if (!value) return t("common:required", "Required");
									if (value.length < 4) return t("account.password.min-length");
									return undefined;
								},
							}}
						>
							{(field) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("account.users.create.password.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field name="role">
							{(field) => (
								<BgtSelect field={field} label={t("account.users.role")} items={roleItems} disabled={isLoading} />
							)}
						</form.Field>
					</div>
					{error && <div className="text-error text-sm mb-2">{error}</div>}
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={close} disabled={isLoading}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton variant="primary" type="submit" disabled={isLoading}>
							{t("account.users.create.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
