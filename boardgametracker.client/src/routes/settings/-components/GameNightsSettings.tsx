import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";

import { zodValidator } from "@/utils/zodValidator";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

export const GameNightsSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation();

		return (
			<div className="space-y-6">
				<SettingsSection title={t("settings.game-nights.title")} description={t("settings.game-nights.description")}>
					<form.Field name="gameNightsEnabled" validators={zodValidator(SettingsSchema, "gameNightsEnabled")}>
						{(field: AnyFieldApi) => (
							<SettingsToggle
								field={field}
								label={t("settings.game-nights.enabled.label")}
								description={t("settings.game-nights.enabled.description")}
								disabled={disabled}
							/>
						)}
					</form.Field>

					<form.Subscribe
						selector={(state: { values: { gameNightsEnabled: boolean } }) => state.values.gameNightsEnabled}
					>
						{(gameNightsEnabled: boolean) =>
							gameNightsEnabled && (
								<form.Field name="publicUrl" validators={zodValidator(SettingsSchema, "publicUrl")}>
									{(field: AnyFieldApi) => (
										<BgtInputField
											field={field}
											disabled={disabled}
											type="text"
											label={t("settings.game-nights.public-url.label")}
											placeholder={t("settings.game-nights.public-url.placeholder")}
										/>
									)}
								</form.Field>
							)
						}
					</form.Subscribe>
				</SettingsSection>
			</div>
		);
	},
});
