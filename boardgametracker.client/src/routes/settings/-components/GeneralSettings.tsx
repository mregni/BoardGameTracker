import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField, BgtSelect } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { SettingsSection } from "./SettingsSection";

export const GeneralSettings = withForm({
	...settingsFormOpts,
	props: {
		languages: [] as { key: string; translationKey: string }[],
		disabled: false,
	},
	render: function Render({ form, languages, disabled }) {
		const { t } = useTranslation(["settings", "language"]);

		return (
			<>
				<SettingsSection title={t("general.language.title")} description={t("general.language.description")}>
					<form.Field name="uiLanguage" validators={zodValidator(SettingsSchema, "uiLanguage")}>
						{(field: AnyFieldApi) => (
							<BgtSelect
								field={field}
								disabled={disabled}
								label={t("general.language.label")}
								items={languages.map((value: { key: string; translationKey: string }) => ({
									label: t(`language:${value.translationKey}`),
									value: value.key,
								}))}
							/>
						)}
					</form.Field>
				</SettingsSection>

				<SettingsSection title={t("general.date-time.title")} description={t("general.date-time.description")}>
					<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
						<form.Field name="dateFormat" validators={zodValidator(SettingsSchema, "dateFormat")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled}
									type="text"
									label={t("general.date-time.date-format")}
									placeholder="MM/DD/YYYY"
								/>
							)}
						</form.Field>
						<form.Field name="timeFormat" validators={zodValidator(SettingsSchema, "timeFormat")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled}
									type="text"
									label={t("general.date-time.time-format")}
									placeholder="HH:mm"
								/>
							)}
						</form.Field>
					</div>
				</SettingsSection>

				<SettingsSection title={t("general.currency.title")} description={t("general.currency.description")}>
					<form.Field name="currency" validators={zodValidator(SettingsSchema, "currency")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								disabled={disabled}
								type="text"
								label={t("general.currency.label")}
								placeholder="USD"
							/>
						)}
					</form.Field>
				</SettingsSection>
			</>
		);
	},
});
