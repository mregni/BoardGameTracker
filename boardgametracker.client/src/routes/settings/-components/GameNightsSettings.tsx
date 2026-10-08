import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { withForm } from "@/hooks/form";
import { SettingsSchema } from "@/models";

import { zodValidator } from "@/utils/zodValidator";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { EnvOverrideHint } from "./EnvOverrideHint";
import { SettingsSection } from "./SettingsSection";
import { SettingsToggle } from "./SettingsToggle";

export const GameNightsSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
		overrides: {} as Record<string, string>,
	},
	render: function Render({ form, disabled, overrides }) {
		const { t } = useTranslation("settings");

		return (
			<div className="space-y-6">
				<SettingsSection title={t("game-nights.title")} description={t("game-nights.description")}>
					<form.Field name="gameNightsEnabled" validators={zodValidator(SettingsSchema, "gameNightsEnabled")}>
						{(field: AnyFieldApi) => (
							<SettingsToggle
								field={field}
								label={t("game-nights.enabled.label")}
								description={t("game-nights.enabled.description")}
								disabled={disabled || !!overrides.gameNightsEnabled}
							/>
						)}
					</form.Field>
					<EnvOverrideHint variable={overrides.gameNightsEnabled} />

					<form.Subscribe
						selector={(state: { values: { gameNightsEnabled: boolean } }) => state.values.gameNightsEnabled}
					>
						{(gameNightsEnabled: boolean) => (
							<>
								<form.Field
									name="rsvpAuthenticationEnabled"
									validators={zodValidator(SettingsSchema, "rsvpAuthenticationEnabled")}
								>
									{(field: AnyFieldApi) => (
										<SettingsToggle
											field={field}
											label={t("game-nights.rsvp-authentication.label")}
											description={t("game-nights.rsvp-authentication.description")}
											disabled={disabled || !gameNightsEnabled || !!overrides.rsvpAuthenticationEnabled}
										/>
									)}
								</form.Field>
								<EnvOverrideHint variable={overrides.rsvpAuthenticationEnabled} />
							</>
						)}
					</form.Subscribe>
				</SettingsSection>
			</div>
		);
	},
});
