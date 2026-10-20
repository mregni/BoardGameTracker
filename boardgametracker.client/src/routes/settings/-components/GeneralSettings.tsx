import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField, BgtSelect } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";
import { effectivePublicUrl, isPublicUrlConfigured } from "@/utils/publicUrl";
import { zodValidator } from "@/utils/zodValidator";
import { useSingleSignOnEnabled } from "../-hooks/useSingleSignOnEnabled";
import { breaksSingleSignOn, getPublicUrlWarning } from "../-utils/publicUrlWarning";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { EnvOverrideHint } from "./EnvOverrideHint";
import { SettingsSection } from "./SettingsSection";

export const GeneralSettings = withForm({
	...settingsFormOpts,
	props: {
		languages: [] as { key: string; translationKey: string }[],
		disabled: false,
		overrides: {} as Record<string, string>,
	},
	render: function Render({ form, languages, disabled, overrides }) {
		const { t } = useTranslation(["settings", "language"]);
		const ssoEnabled = useSingleSignOnEnabled();

		return (
			<>
				<SettingsSection title={t("general.regional.title")}>
					<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
						<form.Field name="uiLanguage" validators={zodValidator(SettingsSchema, "uiLanguage")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									disabled={disabled || !!overrides.uiLanguage}
									label={t("general.regional.language")}
									items={languages.map((value: { key: string; translationKey: string }) => ({
										label: t(`language:${value.translationKey}`),
										value: value.key,
									}))}
								/>
							)}
						</form.Field>
						<form.Field name="currency" validators={zodValidator(SettingsSchema, "currency")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled || !!overrides.currency}
									type="text"
									label={t("general.regional.currency")}
									placeholder="€"
								/>
							)}
						</form.Field>
						<form.Field name="dateFormat" validators={zodValidator(SettingsSchema, "dateFormat")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled || !!overrides.dateFormat}
									type="text"
									label={t("general.regional.date-format")}
									placeholder="dd-MM-yyyy"
								/>
							)}
						</form.Field>
						<form.Field name="timeFormat" validators={zodValidator(SettingsSchema, "timeFormat")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled || !!overrides.timeFormat}
									type="text"
									label={t("general.regional.time-format")}
									placeholder="HH:mm"
								/>
							)}
						</form.Field>
					</div>
					<EnvOverrideHint variable={overrides.uiLanguage} />
					<EnvOverrideHint variable={overrides.currency} />
					<EnvOverrideHint variable={overrides.dateFormat} />
					<EnvOverrideHint variable={overrides.timeFormat} />
				</SettingsSection>

				<SettingsSection title={t("general.public-url.title")} description={t("general.public-url.description")}>
					<form.Field name="publicUrl" validators={zodValidator(SettingsSchema, "publicUrl")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								disabled={disabled || !!overrides.publicUrl}
								type="text"
								label={t("general.public-url.label")}
								placeholder={t("general.public-url.placeholder")}
							/>
						)}
					</form.Field>
					<EnvOverrideHint variable={overrides.publicUrl} />
					<form.Subscribe selector={(state) => state.values.publicUrl}>
						{(publicUrl) => {
							const ssoBreaks = ssoEnabled && breaksSingleSignOn(effectivePublicUrl(publicUrl, window.location.origin));
							const ssoWarning = ssoBreaks && (
								<div role="alert" className="mt-2 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3">
									<BgtText size="2" color="amber">
										{t("general.public-url.sso-warning")}
									</BgtText>
								</div>
							);
							if (!isPublicUrlConfigured(publicUrl)) {
								return (
									<>
										<BgtText size="2" color="gray" className="mt-2" data-testid="public-url-not-set">
											{t("general.public-url.not-set")}
										</BgtText>
										{ssoWarning}
									</>
								);
							}
							const warning = getPublicUrlWarning(publicUrl, window.location);
							return (
								<>
									{warning && (
										<div role="alert" className="mt-2 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3">
											<BgtText size="2" color="amber">
												{t(`general.public-url.${warning}-warning`, { host: window.location.host })}
											</BgtText>
										</div>
									)}
									{ssoWarning}
								</>
							);
						}}
					</form.Subscribe>
				</SettingsSection>
			</>
		);
	},
});
