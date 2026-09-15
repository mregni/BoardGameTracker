import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtInputField } from "@/components/BgtForm";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAppForm } from "@/hooks/form";
import { useModalState } from "@/hooks/useModalState";
import { type OidcProviderConfig, type OidcProviderForm, OidcProviderSchema, type OidcSetup } from "@/models";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useSsoSettings } from "../-hooks/useSsoSettings";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

const toForm = (provider: OidcProviderConfig | null): OidcProviderForm => ({
	name: provider?.name ?? "",
	displayName: provider?.displayName ?? "",
	authority: provider?.authority ?? "",
	clientId: provider?.clientId ?? "",
	clientSecret: "",
	scopes: provider?.scopes ?? "openid profile email",
	enabled: provider?.enabled ?? true,
	autoProvisionUsers: provider?.autoProvisionUsers ?? true,
	usernameClaimType: provider?.usernameClaimType ?? "",
	emailClaimType: provider?.emailClaimType ?? "",
	displayNameClaimType: provider?.displayNameClaimType ?? "",
	rolesClaimType: provider?.rolesClaimType ?? "",
	adminGroupValue: provider?.adminGroupValue ?? "",
});

const CopyValue = ({ label, value }: { label: string; value: string }) => {
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
				<BgtButton variant="text" size="1" onClick={() => navigator.clipboard.writeText(value)}>
					{t("sso.setup.copy")}
				</BgtButton>
			</div>
		</div>
	);
};

const SetupPanel = ({ setup, name }: { setup: OidcSetup; name: string }) => {
	const { t } = useTranslation("settings");
	const providerName = name.trim() || "provider";
	return (
		<SettingsSection title={t("sso.setup.title")} description={t("sso.setup.description")}>
			{!setup.publicUrlConfigured && (
				<BgtText size="2" color="amber">
					{t("sso.setup.public-url-warning", { url: setup.publicBaseUrl })}
				</BgtText>
			)}
			<CopyValue
				label={t("sso.setup.redirect-uri")}
				value={setup.callbackUriTemplate.replace("{name}", providerName)}
			/>
			<CopyValue
				label={t("sso.setup.link-redirect-uri")}
				value={setup.linkCallbackUriTemplate.replace("{name}", providerName)}
			/>
		</SettingsSection>
	);
};

export const SsoSettings = () => {
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

	const form = useAppForm({
		defaultValues: toForm(provider),
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
			<SettingsSection
				title={t("sso.title")}
				description={
					provider ? t("sso.description.configured", { name: provider.displayName }) : t("sso.description.empty")
				}
			>
				<form onSubmit={handleFormSubmit(form)} className="flex flex-col gap-3">
					<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
						<form.Field name="name" validators={zodValidator(OidcProviderSchema, "name")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.name.label")}
									placeholder={t("sso.fields.name.placeholder")}
									disabled={busy || provider !== null}
								/>
							)}
						</form.Field>
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
					</div>
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
								{t("sso.discovery.userinfo")}: {discovery.userInfoEndpoint}
							</span>
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
						<form.Field name="usernameClaimType">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.username-claim.label")}
									placeholder="preferred_username"
									disabled={busy}
								/>
							)}
						</form.Field>
						<form.Field name="emailClaimType">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.email-claim.label")}
									placeholder="email"
									disabled={busy}
								/>
							)}
						</form.Field>
						<form.Field name="displayNameClaimType">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.display-name-claim.label")}
									placeholder="name"
									disabled={busy}
								/>
							)}
						</form.Field>
						<form.Field name="rolesClaimType">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.roles-claim.label")}
									placeholder="groups"
									disabled={busy}
								/>
							)}
						</form.Field>
						<form.Field name="adminGroupValue">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("sso.fields.admin-group.label")}
									placeholder={t("sso.fields.admin-group.placeholder")}
									disabled={busy}
								/>
							)}
						</form.Field>
					</div>
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
									disabled={busy}
								/>
							)}
						</form.Field>
					)}
					<div className="flex flex-wrap gap-3 pt-2">
						<BgtButton type="submit" disabled={busy}>
							{provider ? t("sso.save") : t("sso.create")}
						</BgtButton>
						{provider && (
							<BgtButton variant="error" disabled={busy} onClick={deleteModal.show}>
								{t("sso.delete")}
							</BgtButton>
						)}
					</div>
				</form>
			</SettingsSection>
			<form.Subscribe selector={(state) => state.values.name}>
				{(name) => <SetupPanel setup={setup} name={name} />}
			</form.Subscribe>
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
