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
import { type BgtSelectItem, isApiError, type UserDto } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";

interface Props {
	open: boolean;
	close: () => void;
	user: UserDto;
	onSubmit: (data: { userId: string; username: string; email: string | null; role: string }) => Promise<unknown>;
	isLoading: boolean;
}

export const EditUserModal = ({ open, close, user, onSubmit, isLoading }: Props) => {
	const { t } = useTranslation(["settings", "auth", "common"]);
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
			username: user.username,
			email: user.email ?? "",
			role: user.roles[0] ?? "User",
		},
		onSubmit: async ({ value }) => {
			setError(null);
			try {
				await onSubmit({
					userId: user.id,
					username: value.username,
					email: value.email || null,
					role: value.role,
				});
				close();
			} catch (e) {
				setError(isApiError(e) ? t(e.message) : t("account.notifications.user-update-failed"));
			}
		},
	});

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("account.users.edit.title")}</BgtDialogTitle>
					<BgtDialogDescription>
						{t("account.users.edit.description", { username: user.username })}
					</BgtDialogDescription>
					<div className="flex flex-col gap-2 mb-3 mt-3">
						<form.Field name="username">
							{(field) => <BgtInputField field={field} type="text" label={t("auth:username")} disabled={isLoading} />}
						</form.Field>
						<form.Field name="email">
							{(field) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("account.profile.email.label")}
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
							{t("common:save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
