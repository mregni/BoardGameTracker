import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtText } from "@/components/BgtText/BgtText";
import type { UserDto } from "@/models";
import type { ExternalLogin } from "@/models/Auth/Oidc";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { getSettings } from "@/services/queries/settings";
import { toDisplayDateTime } from "@/utils/dateUtils";
import { useUserAccess } from "../-hooks/useUserAccess";

interface Props {
	open: boolean;
	close: () => void;
	user: UserDto;
	isCurrentUser: boolean;
}

export const UserAccessModal = ({ open, close, user, isCurrentUser }: Props) => {
	const { t } = useTranslation(["settings", "common"]);
	const { logins, isLoading, disable, enable, unlink, manageRoleByIdentityProvider, isBusy } = useUserAccess(user.id);
	const { data: settings } = useQuery(getSettings());
	const [pendingUnlink, setPendingUnlink] = useState<ExternalLogin | null>(null);
	const [confirmDisable, setConfirmDisable] = useState(false);
	const canDisable = !user.isDisabled && !isCurrentUser;
	const dateFormat = settings?.dateFormat ?? "yyyy-MM-dd";
	const timeFormat = settings?.timeFormat ?? "HH:mm";
	const uiLanguage = settings?.uiLanguage ?? "en-US";

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent aria-describedby={undefined}>
				<BgtDialogTitle>{t("account.users.access.title")}</BgtDialogTitle>
				<div className="flex flex-col gap-4 my-3">
					<section className="flex flex-col gap-2">
						<div className="flex items-center justify-between gap-3">
							<BgtText color={user.isDisabled ? "red" : "green"} className="font-semibold">
								{user.isDisabled ? t("account.users.access.disabled") : t("account.users.access.enabled")}
							</BgtText>
							{user.isDisabled && (
								<BgtButton size="1" variant="cancel" disabled={isBusy} onClick={() => enable()}>
									{t("account.users.access.enable")}
								</BgtButton>
							)}
							{canDisable && (
								<BgtButton size="1" variant="error" disabled={isBusy} onClick={() => setConfirmDisable(true)}>
									{t("account.users.access.disable")}
								</BgtButton>
							)}
						</div>
						{canDisable && (
							<BgtText size="1" color="gray">
								{t("account.users.access.disable-hint")}
							</BgtText>
						)}
					</section>
					<section className="flex flex-col gap-2">
						<BgtText color="white" className="font-semibold">
							{t("account.users.access.sso-title")}
						</BgtText>
						{!isLoading && logins.length === 0 && (
							<BgtText size="2" color="gray">
								{t("account.users.access.sso-none")}
							</BgtText>
						)}
						{logins.length > 0 && (
							<ul className="flex flex-col gap-2">
								{logins.map((login) => (
									<li
										key={login.id}
										className="flex items-center justify-between gap-3 p-3 bg-background rounded-lg border border-white/10"
									>
										<div className="flex flex-col min-w-0">
											<BgtText color="white" className="font-semibold truncate">
												{login.providerDisplayName
													? `${login.provider} · ${login.providerDisplayName}`
													: login.provider}
											</BgtText>
											<BgtText size="1" color="gray" className="truncate">
												{t("account.users.access.sso-linked", {
													subject: login.providerKey,
													date: toDisplayDateTime(login.linkedAt, dateFormat, timeFormat, uiLanguage),
												})}
											</BgtText>
										</div>
										<BgtButton size="1" variant="cancel" disabled={isBusy} onClick={() => setPendingUnlink(login)}>
											{t("account.users.access.unlink")}
										</BgtButton>
									</li>
								))}
							</ul>
						)}
						{logins.length > 0 && (
							<div className="flex flex-col gap-2">
								<BgtText size="2" color="gray">
									{user.roleManagedByIdentityProvider
										? t("account.users.access.role-managed")
										: t("account.users.access.role-local")}
								</BgtText>
								{!user.roleManagedByIdentityProvider && (
									<div>
										<BgtButton
											size="1"
											variant="cancel"
											disabled={isBusy}
											onClick={() => manageRoleByIdentityProvider()}
										>
											{t("account.users.access.manage-role")}
										</BgtButton>
									</div>
								)}
							</div>
						)}
					</section>
				</div>
				<BgtDialogFooter onCancel={close} cancelLabel={t("common:close")} />
				{confirmDisable && (
					<BgtDeleteModal
						open
						close={() => setConfirmDisable(false)}
						title={user.username}
						heading={t("account.users.access.disable-confirm.title", { username: user.username })}
						description={t("account.users.access.disable-confirm.description")}
						confirmLabel={t("account.users.access.disable")}
						onDelete={() => {
							disable();
							setConfirmDisable(false);
						}}
					/>
				)}
				{pendingUnlink && (
					<BgtDeleteModal
						open
						close={() => setPendingUnlink(null)}
						title={pendingUnlink.provider}
						heading={t("account.external-logins.unlink-title", { provider: pendingUnlink.provider })}
						description={t("account.users.access.unlink-confirm", { username: user.username })}
						confirmLabel={t("account.users.access.unlink")}
						onDelete={() => {
							unlink(pendingUnlink.id);
							setPendingUnlink(null);
						}}
					/>
				)}
			</BgtDialogContent>
		</BgtDialog>
	);
};
