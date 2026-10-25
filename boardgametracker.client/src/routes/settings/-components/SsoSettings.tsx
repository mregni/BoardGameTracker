import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtInputField, BgtSelect } from "@/components/BgtForm";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAppForm } from "@/hooks/form";
import { useModalState } from "@/hooks/useModalState";
import {
	type BgtSelectItem,
	OIDC_CLAIM_DEFAULTS,
	OIDC_DEFAULT_ROLES,
	type OidcProviderConfig,
	type OidcProviderForm,
	OidcProviderSchema,
	type OidcSetup,
} from "@/models";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { DEFAULT_DATE_FORMAT, toDisplay } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useSsoSettings } from "../-hooks/useSsoSettings";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

type ClaimField = keyof typeof OIDC_CLAIM_DEFAULTS;

const claimValue = (provider: OidcProviderConfig | null, field: ClaimField) =>
	provider?.[field]?.trim() || OIDC_CLAIM_DEFAULTS[field];

const CLAIM_FIELDS: { name: ClaimField; label: string }[] = [
	{ name: "usernameClaimType", label: "sso.fields.username-claim.label" },
	{ name: "emailClaimType", label: "sso.fields.email-claim.label" },
	{ name: "displayNameClaimType", label: "sso.fields.display-name-claim.label" },
	{ name: "pictureClaimType", label: "sso.fields.picture-claim.label" },
	{ name: "rolesClaimType", label: "sso.fields.roles-claim.label" },
	{ name: "adminGroupValue", label: "sso.fields.admin-group.label" },
];

const toForm = (provider: OidcProviderConfig | null, httpsRequired: boolean): OidcProviderForm => ({
	displayName: provider?.displayName ?? "",
	authority: provider?.authority ?? "",
	clientId: provider?.clientId ?? "",
	clientSecret: "",
	scopes: provider?.scopes ?? "openid profile email",
	enabled: provider ? provider.enabled : !httpsRequired,
	autoProvisionUsers: provider?.autoProvisionUsers ?? true,
	defaultRole: provider?.defaultRole ?? "User",
	usernameClaimType: claimValue(provider, "usernameClaimType"),
	emailClaimType: claimValue(provider, "emailClaimType"),
	displayNameClaimType: claimValue(provider, "displayNameClaimType"),
	pictureClaimType: claimValue(provider, "pictureClaimType"),
	rolesClaimType: claimValue(provider, "rolesClaimType"),
	adminGroupValue: claimValue(provider, "adminGroupValue"),
});

const CopyValue = ({ label, value, disabled = false }: { label: string; value: string; disabled?: boolean }) => {
	const { t } = useTranslation("settings");
	return (
		<div className="flex flex-col gap-1">
			<BgtText size="1" color="gray">
				{label}
			</BgtText>
			<div className="flex items-center gap-2">
				<code className="flex-1 min-w-0 truncate rounded-lg bg-background border border-white/10 px-3 py-2 text-[12px]">
					{value}
				</code>
				<BgtButton variant="text" size="1" disabled={disabled} onClick={() => navigator.clipboard.writeText(value)}>
					{t("sso.setup.copy")}
				</BgtButton>
			</div>
		</div>
	);
};

const SetupPanel = ({ setup }: { setup: OidcSetup }) => {
	const { t } = useTranslation("settings");
	const pending = setup.httpsRequired;
	return (
		<SettingsSection
			title={t("sso.setup.title")}
			description={pending ? t("sso.setup.https-pending") : t("sso.setup.description")}
		>
			{!pending && !setup.publicUrlConfigured && (
				<BgtText size="2" color="amber">
					{t("sso.setup.public-url-warning", { url: setup.publicBaseUrl })}
				</BgtText>
			)}
			<div className={cx("flex flex-col gap-3", pending && "opacity-50")} data-testid="sso-redirect-uris">
				<CopyValue label={t("sso.setup.redirect-uri")} value={setup.callbackUri} disabled={pending} />
				<CopyValue label={t("sso.setup.link-redirect-uri")} value={setup.linkCallbackUri} disabled={pending} />
				<CopyValue
					label={t("sso.setup.post-logout-redirect-uri")}
					value={setup.postLogoutRedirectUri}
					disabled={pending}
				/>
			</div>
		</SettingsSection>
	);
};

const statusLine = (provider: OidcProviderConfig | null, httpsRequired: boolean, t: TFunction<"settings">) => {
	if (!provider) {
		return t("sso.description.empty");
	}
	return httpsRequired && provider.enabled
		? t("sso.description.inactive", { name: provider.displayName })
		: t("sso.description.configured", { name: provider.displayName });
};

const normalizeAuthority = (authority: string) => authority.trim().replace(/\/+$/, "").toLowerCase();

interface Props {
	dateFormat?: string;
	uiLanguage?: string;
}

export const SsoSettings = ({ dateFormat = DEFAULT_DATE_FORMAT, uiLanguage = "en-US" }: Props) => {
	const { t } = useTranslation(["settings", "common"]);
	const {
		isLoading,
		provider,
		setup,
		save,
		isSaving,
		remove,
		isDeleting,
		testDiscovery,
		isTestingDiscovery,
		discovery,
		discoveryError,
	} = useSsoSettings();
	const deleteModal = useModalState();
	const roleItems: BgtSelectItem[] = OIDC_DEFAULT_ROLES.map((role) => ({
		value: role,
		label: t(`account.users.roles.${role.toLowerCase()}`),
	}));

	const httpsRequired = setup?.httpsRequired ?? false;
	const lockedByHttps = httpsRequired && provider?.enabled === true;

	const form = useAppForm({
		defaultValues: toForm(provider, httpsRequired),
		onSubmit: async ({ value }) => {
			await save(OidcProviderSchema.parse(value));
		},
	});

	if (isLoading || !setup) {
		return <BgtLoadingSpinner />;
	}

	const busy = isSaving || isDeleting;

	return (
		<>
			<SettingsSection title={t("sso.title")} description={statusLine(provider, httpsRequired, t)}>
				{httpsRequired && (
					<div role="alert" className="p-3 bg-error/10 border border-error/20 rounded-md">
						<BgtText size="2" color="amber">
							{lockedByHttps ? t("sso.https-required-locked") : t("sso.https-required")}
						</BgtText>
					</div>
				)}
				<form onSubmit={handleFormSubmit(form)} className="flex flex-col gap-3">
					<form.Field name="displayName" validators={zodValidator(OidcProviderSchema, "displayName")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								type="text"
								label={t("sso.fields.display-name.label")}
								placeholder={t("sso.fields.display-name.placeholder")}
								disabled={busy}
							/>
						)}
					</form.Field>
					<form.Field name="authority" validators={zodValidator(OidcProviderSchema, "authority")}>
						{(field: AnyFieldApi) => (
							<div className="flex flex-col md:flex-row gap-2 md:items-end">
								<div className="flex-1">
									<BgtInputField
										field={field}
										type="text"
										label={t("sso.fields.authority.label")}
										placeholder={t("sso.fields.authority.placeholder")}
										disabled={busy}
									/>
								</div>
								<BgtButton
									variant="cancel"
									disabled={busy || isTestingDiscovery || !String(field.state.value).trim()}
									onClick={() => testDiscovery(String(field.state.value).trim())}
								>
									{isTestingDiscovery ? t("sso.discovery.testing") : t("sso.discovery.button")}
								</BgtButton>
							</div>
						)}
					</form.Field>
					{provider && (
						<form.Subscribe selector={(state) => state.values.authority}>
							{(authority) =>
								normalizeAuthority(authority) !== normalizeAuthority(provider.authority) && (
									<BgtText size="2" color="amber">
										{t("sso.fields.authority.relink-warning")}
									</BgtText>
								)
							}
						</form.Subscribe>
					)}
					{discoveryError && (
						<BgtText size="2" color="red">
							{discoveryError}
						</BgtText>
					)}
					{discovery && (
						<div className="rounded-lg bg-background border border-white/10 p-3 flex flex-col gap-1 text-[12px]">
							<BgtText size="2" color={discovery.issuerMatchesAuthority ? "green" : "amber"}>
								{discovery.issuerMatchesAuthority
									? t("sso.discovery.ok")
									: t("sso.discovery.issuer-mismatch", { issuer: discovery.issuer })}
							</BgtText>
							<span className="truncate">
								{t("sso.discovery.authorization")}: {discovery.authorizationEndpoint}
							</span>
							<span className="truncate">
								{t("sso.discovery.token")}: {discovery.tokenEndpoint}
							</span>
							<span className="truncate">
								{t("sso.discovery.userinfo")}: {discovery.userInfoEndpoint ?? t("sso.discovery.none")}
							</span>
							<span className="truncate">
								{t("sso.discovery.end-session")}: {discovery.endSessionEndpoint ?? t("sso.discovery.none")}
							</span>
							<span className="truncate">
								{t("sso.discovery.signing")}:{" "}
								{discovery.signingAlgorithms.length > 0
									? discovery.signingAlgorithms.join(", ")
									: t("sso.discovery.none")}
							</span>
							{!discovery.signingSupported && (
								<BgtText size="2" color="red">
									{t("sso.discovery.signing-unsupported", {
										algorithms: discovery.signingAlgorithms.join(", "),
									})}
								</BgtText>
							)}
							{discovery.signingCertificateExpiredOn && (
								<BgtText size="2" color="amber">
									{t("sso.discovery.certificate-expired", {
										date: toDisplay(discovery.signingCertificateExpiredOn, dateFormat, uiLanguage),
									})}
								</BgtText>
							)}
						</div>
					)}
					<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
						<form.Field name="clientId" validators={zodValidator(OidcProviderSchema, "clientId")}>
							{(field: AnyFieldApi) => (
								<BgtInputField field={field} type="text" label={t("sso.fields.client-id.label")} disabled={busy} />
							)}
						</form.Field>
						<form.Field name="clientSecret">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("sso.fields.client-secret.label")}
									placeholder={
										provider?.hasClientSecret
											? t("sso.fields.client-secret.unchanged")
											: t("sso.fields.client-secret.placeholder")
									}
									disabled={busy}
								/>
							)}
						</form.Field>
					</div>
					<form.Field name="scopes" validators={zodValidator(OidcProviderSchema, "scopes")}>
						{(field: AnyFieldApi) => (
							<BgtInputField field={field} type="text" label={t("sso.fields.scopes.label")} disabled={busy} />
						)}
					</form.Field>
					<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
						{CLAIM_FIELDS.map(({ name, label }) => (
							<form.Field key={name} name={name}>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										type="text"
										label={t(label)}
										placeholder={t("sso.fields.default-value", { value: OIDC_CLAIM_DEFAULTS[name] })}
										disabled={busy}
									/>
								)}
							</form.Field>
						))}
						<form.Field name="defaultRole">
							{(field: AnyFieldApi) => (
								<BgtSelect field={field} label={t("sso.fields.default-role.label")} items={roleItems} disabled={busy} />
							)}
						</form.Field>
					</div>
					<BgtText size="2" color="gray">
						{t("sso.fields.role-sync")}
					</BgtText>
					<form.Subscribe
						selector={(state) => !state.values.rolesClaimType.trim() || !state.values.adminGroupValue.trim()}
					>
						{(usesRoleDefaults) =>
							usesRoleDefaults && (
								<BgtText size="2" color="amber" data-testid="sso-role-sync-defaults">
									{t("sso.fields.role-sync-defaults", {
										claim: OIDC_CLAIM_DEFAULTS.rolesClaimType,
										group: OIDC_CLAIM_DEFAULTS.adminGroupValue,
									})}
								</BgtText>
							)
						}
					</form.Subscribe>
					<BgtText size="2" color="gray">
						{t("sso.fields.picture-claim.hint")}
					</BgtText>
					<form.Field name="autoProvisionUsers">
						{(field: AnyFieldApi) => (
							<SettingsToggle
								field={field}
								label={t("sso.fields.auto-provision.label")}
								description={t("sso.fields.auto-provision.description")}
								disabled={busy}
							/>
						)}
					</form.Field>
					{provider && (
						<form.Field name="enabled">
							{(field: AnyFieldApi) => (
								<SettingsToggle
									field={field}
									label={t("sso.fields.enabled.label")}
									description={t("sso.fields.enabled.description")}
									disabled={busy || (httpsRequired && !field.state.value)}
								/>
							)}
						</form.Field>
					)}
					<div className="flex flex-wrap gap-3 pt-2">
						<form.Subscribe selector={(state) => state.values.enabled}>
							{(enabled) => (
								<BgtButton type="submit" disabled={busy || (lockedByHttps && enabled)}>
									{provider ? t("sso.save") : t("sso.create")}
								</BgtButton>
							)}
						</form.Subscribe>
						{provider && (
							<BgtButton variant="error" disabled={busy} onClick={deleteModal.show}>
								{t("sso.delete")}
							</BgtButton>
						)}
					</div>
				</form>
			</SettingsSection>
			<SetupPanel setup={setup} />
			{provider && (
				<BgtDeleteModal
					title={provider.displayName}
					open={deleteModal.isOpen}
					close={deleteModal.hide}
					onDelete={() => remove(provider.id).finally(deleteModal.hide)}
					description={t("sso.delete-description")}
				/>
			)}
		</>
	);
};
