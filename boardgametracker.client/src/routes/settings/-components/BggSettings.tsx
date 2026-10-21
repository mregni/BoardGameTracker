import type { AnyFieldApi } from "@tanstack/react-form";
import { Trans, useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtInputField } from "@/components/BgtForm";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { type BggConfigStatus, type ChangeDetectionConfigStatus, SettingsSchema } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { useIntegrationStatus } from "../-hooks/useIntegrationStatus";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { IntegrationStatusRow } from "./IntegrationStatusRow";
import { SettingsSection } from "./SettingsSection";

const REMOVE_BUTTON_CLASS = "h-11 shrink-0 self-start @sm:self-auto md:h-10";

export const BggSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
		timeFormat: "HH:mm",
		bggStatus: {
			isConfigured: false,
			isReadOnly: false,
			source: "db",
		} as BggConfigStatus,
		changeDetectionStatus: {
			isConfigured: false,
			isReadOnly: false,
			source: "db",
		} as ChangeDetectionConfigStatus,
	},
	render: function Render({ form, disabled, timeFormat, bggStatus, changeDetectionStatus }) {
		const { t } = useTranslation("settings");
		const { statusFor, isChecking, testIntegration, testingIntegration } = useIntegrationStatus();

		return (
			<div className="space-y-6">
				<SettingsSection
					title={t("bgg.title")}
					description={t("bgg.description")}
					aside={
						<IntegrationStatusRow
							integration="bgg"
							status={statusFor("bgg")}
							isChecking={isChecking}
							isTesting={testingIntegration === "bgg"}
							canTest={bggStatus.isConfigured}
							timeFormat={timeFormat}
							onTest={testIntegration}
						/>
					}
				>
					{bggStatus.isReadOnly ? (
						<BgtStatus
							variant="info"
							title={t("bgg.api-key.read-only")}
							description={t("bgg.api-key.read-only-description")}
						/>
					) : (
						<div className="flex flex-1 flex-col gap-1">
							<div className="flex flex-col gap-2 @sm:flex-row @sm:items-end">
								<div className="min-w-0 flex-1">
									<form.Field name="bggApiKey">
										{(field: AnyFieldApi) => (
											<BgtInputField
												field={field}
												disabled={disabled}
												type="password"
												label={t("bgg.api-key.label")}
												placeholder={t("bgg.api-key.placeholder")}
											/>
										)}
									</form.Field>
								</div>
								{bggStatus.isConfigured && (
									<BgtButton
										variant="error"
										type="button"
										className={REMOVE_BUTTON_CLASS}
										onClick={() => form.setFieldValue("bggApiKey", null)}
									>
										{t("bgg.api-key.clear")}
									</BgtButton>
								)}
							</div>
							<form.Subscribe selector={(state) => state.values.bggApiKey}>
								{(bggApiKey) =>
									bggStatus.isConfigured && bggApiKey === null ? (
										<BgtText size="1" color="white" opacity={50}>
											{t("bgg.api-key.clear-hint")}
										</BgtText>
									) : null
								}
							</form.Subscribe>
							<BgtText size="1" color="white" opacity={50}>
								<Trans
									i18nKey="bgg.api-key.help-description"
									ns="settings"
									components={{
										requestPage: (
											// biome-ignore lint/a11y/useAnchorContent: Trans injects the link text from the translation
											<a
												href="https://boardgamegeek.com/applications"
												target="_blank"
												rel="noopener noreferrer"
												className="text-cyan-400 hover:text-cyan-300 underline"
											/>
										),
									}}
								/>
							</BgtText>
						</div>
					)}
				</SettingsSection>

				<SettingsSection
					title={t("changedetection.title")}
					description={t("changedetection.description")}
					aside={
						<IntegrationStatusRow
							integration="changeDetection"
							status={statusFor("changeDetection")}
							isChecking={isChecking}
							isTesting={testingIntegration === "changeDetection"}
							canTest={changeDetectionStatus.isConfigured}
							timeFormat={timeFormat}
							onTest={testIntegration}
						/>
					}
				>
					<div className="flex-1">
						<form.Field
							name="changeDetectionBaseUrl"
							validators={zodValidator(SettingsSchema, "changeDetectionBaseUrl")}
						>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={disabled}
									type="text"
									label={t("changedetection.base-url.label")}
									placeholder={t("changedetection.base-url.placeholder")}
								/>
							)}
						</form.Field>
					</div>

					<div className="flex flex-1 flex-col gap-1">
						<div className="flex flex-col gap-2 @sm:flex-row @sm:items-end">
							<div className="min-w-0 flex-1">
								<form.Field name="changeDetectionApiKey">
									{(field: AnyFieldApi) => (
										<BgtInputField
											field={field}
											disabled={disabled}
											type="password"
											label={t("changedetection.api-key.label")}
											placeholder={t("changedetection.api-key.placeholder")}
										/>
									)}
								</form.Field>
							</div>
							{changeDetectionStatus.isConfigured && (
								<BgtButton
									variant="error"
									type="button"
									className={REMOVE_BUTTON_CLASS}
									onClick={() => form.setFieldValue("changeDetectionApiKey", null)}
								>
									{t("changedetection.api-key.clear")}
								</BgtButton>
							)}
						</div>
						<form.Subscribe selector={(state) => state.values.changeDetectionApiKey}>
							{(changeDetectionApiKey) =>
								changeDetectionStatus.isConfigured && changeDetectionApiKey === null ? (
									<BgtText size="1" color="white" opacity={50}>
										{t("changedetection.api-key.clear-hint")}
									</BgtText>
								) : null
							}
						</form.Subscribe>
						<BgtText size="1" color="white" opacity={50}>
							{t("changedetection.help-description")}
						</BgtText>
					</div>
				</SettingsSection>
			</div>
		);
	},
});
