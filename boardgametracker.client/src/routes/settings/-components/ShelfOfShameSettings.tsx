import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";

import { zodValidator } from "@/utils/zodValidator";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { EnvOverrideHint } from "./EnvOverrideHint";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

export const ShelfOfShameSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
		overrides: {} as Record<string, string>,
	},
	render: function Render({ form, disabled, overrides }) {
		const { t } = useTranslation("settings");

		return (
			<div className="space-y-6">
				<SettingsSection title={t("shame.title")} description={t("shame.description")}>
					<form.Field name="shelfOfShameEnabled" validators={zodValidator(SettingsSchema, "shelfOfShameEnabled")}>
						{(field: AnyFieldApi) => (
							<SettingsToggle
								field={field}
								label={t("shame.enabled.label")}
								description={t("shame.enabled.description")}
								disabled={disabled || !!overrides.shelfOfShameEnabled}
							/>
						)}
					</form.Field>
					<EnvOverrideHint variable={overrides.shelfOfShameEnabled} />

					<form.Subscribe
						selector={(state: { values: { shelfOfShameEnabled: boolean } }) => state.values.shelfOfShameEnabled}
					>
						{(shelfOfShameEnabled: boolean) => (
							<form.Field
								name="shelfOfShameMonthsLimit"
								validators={zodValidator(SettingsSchema, "shelfOfShameMonthsLimit")}
							>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										disabled={disabled || !shelfOfShameEnabled || !!overrides.shelfOfShameMonthsLimit}
										type="number"
										label={t("shame.months.label")}
										placeholder="3"
									/>
								)}
							</form.Field>
						)}
					</form.Subscribe>
					<EnvOverrideHint variable={overrides.shelfOfShameMonthsLimit} />
				</SettingsSection>
			</div>
		);
	},
});
