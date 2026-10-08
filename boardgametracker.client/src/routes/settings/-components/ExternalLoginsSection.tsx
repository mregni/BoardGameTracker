import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtText } from "@/components/BgtText/BgtText";
import type { ExternalLogin } from "@/models/Auth/Oidc";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { getSettings } from "@/services/queries/settings";
import { toDisplayDateTime } from "@/utils/dateUtils";
import { useExternalLogins } from "../-hooks/useExternalLogins";
import { SettingsSection } from "./SettingsSection";

export const ExternalLoginsSection = () => {
	const { t } = useTranslation(["settings", "common"]);
	const { logins, provider, isLoading, unlink, isUnlinking, link, isLinking } = useExternalLogins();
	const { data: settings } = useQuery(getSettings());
	const [pendingUnlink, setPendingUnlink] = useState<ExternalLogin | null>(null);
	const dateFormat = settings?.dateFormat ?? "yyyy-MM-dd";
	const timeFormat = settings?.timeFormat ?? "HH:mm";
	const uiLanguage = settings?.uiLanguage ?? "en-US";

	if (isLoading) {
		return <BgtLoadingSpinner />;
	}

	if (!provider && logins.length === 0) {
		return null;
	}

	const canLink = provider !== null && !logins.some((login) => login.provider === provider.name);
	const providerLabel = (login: ExternalLogin) =>
		provider?.name === login.provider ? provider.displayName : login.provider;

	return (
		<SettingsSection title={t("account.external-logins.title")} description={t("account.external-logins.description")}>
			{logins.length === 0 ? (
				<BgtText size="2" color="gray">
					{t("account.external-logins.none")}
				</BgtText>
			) : (
				<ul className="flex flex-col gap-2">
					{logins.map((login) => (
						<li
							key={login.id}
							className="flex items-center justify-between gap-3 p-3 bg-background rounded-lg border border-white/10"
						>
							<div className="flex flex-col min-w-0">
								<BgtText color="white" className="font-semibold truncate">
									{providerLabel(login)}
								</BgtText>
								<BgtText size="1" color="gray">
									{login.providerDisplayName ? `${login.providerDisplayName} · ` : ""}
									{t("account.external-logins.linked-on", {
										date: toDisplayDateTime(login.linkedAt, dateFormat, timeFormat, uiLanguage),
									})}
								</BgtText>
							</div>
							<BgtButton variant="cancel" size="1" disabled={isUnlinking} onClick={() => setPendingUnlink(login)}>
								{t("account.external-logins.unlink")}
							</BgtButton>
						</li>
					))}
				</ul>
			)}
			{canLink && (
				<div>
					<BgtButton disabled={isLinking} onClick={() => link(provider.name)}>
						{t("account.external-logins.link", { provider: provider.displayName })}
					</BgtButton>
				</div>
			)}
			{pendingUnlink && (
				<BgtDeleteModal
					open
					close={() => setPendingUnlink(null)}
					title={providerLabel(pendingUnlink)}
					description={t("account.external-logins.unlink-confirm")}
					onDelete={() => {
						unlink(pendingUnlink.id);
						setPendingUnlink(null);
					}}
				/>
			)}
		</SettingsSection>
	);
};
