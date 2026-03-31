import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";

import { zodValidator } from "@/utils/zodValidator";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

export const ShelfOfShameSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
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
								disabled={disabled}
							/>
						)}
					</form.Field>

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
										disabled={disabled || !shelfOfShameEnabled}
										type="number"
										label={t("shame.months.label")}
										placeholder="3"
									/>
								)}
							</form.Field>
						)}
					</form.Subscribe>
				</SettingsSection>
			</div>
		);
	},
});
