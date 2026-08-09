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
import { type BgtSelectItem, isApiError, type Player, type RegisterRequest, type UserDto } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { buildLinkablePlayerItems } from "../-utils/playerLinkOptions";

interface Props {
	open: boolean;
	close: () => void;
	players: Player[];
	users: UserDto[];
	onSubmit: (request: RegisterRequest) => Promise<unknown>;
	isLoading: boolean;
}

export const CreateUserModal = ({ open, close, players, users, onSubmit, isLoading }: Props) => {
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

	const playerModeItems: BgtSelectItem[] = useMemo(
		() => [
			{ value: "create", label: t("account.users.create.player.create") },
			{ value: "link", label: t("account.users.create.player.link") },
			{ value: "none", label: t("account.users.create.player.none") },
		],
		[t],
	);

	const playerItems: BgtSelectItem[] = useMemo(() => buildLinkablePlayerItems(players, users), [players, users]);

	const form = useForm({
		defaultValues: {
			username: "",
			email: "",
			password: "",
			role: "User",
			playerMode: "create",
			playerId: 0,
		},
		onSubmit: async ({ value }) => {
			setError(null);
			try {
				await onSubmit({
					username: value.username,
					email: value.email,
					password: value.password,
					role: value.role,
					createPlayer: value.playerMode === "create",
					playerId: value.playerMode === "link" && value.playerId ? value.playerId : null,
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
						<form.Field name="playerMode">
							{(field) => (
								<BgtSelect
									field={field}
									label={t("account.users.create.player.label")}
									items={playerModeItems}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Subscribe selector={(state) => state.values.playerMode}>
							{(playerMode) =>
								playerMode === "link" ? (
									<form.Field name="playerId">
										{(field) => (
											<BgtSelect
												field={field}
												label={t("account.users.player.label")}
												items={playerItems}
												hasSearch
												disabled={isLoading}
											/>
										)}
									</form.Field>
								) : null
							}
						</form.Subscribe>
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
