import type { AnyFieldApi } from "@tanstack/react-form";
import { Trans, useTranslation } from "react-i18next";
import Info from "@/assets/icons/info.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtInputField } from "@/components/BgtForm";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import type { BggConfigStatus, ChangeDetectionConfigStatus } from "@/models";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { SettingsSection } from "./SettingsSection";

export const BggSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
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
	render: function Render({
		form,
		disabled,
		bggStatus,
		changeDetectionStatus,
	}: {
		form: any;
		disabled: boolean;
		bggStatus: BggConfigStatus;
		changeDetectionStatus: ChangeDetectionConfigStatus;
	}) {
		const { t } = useTranslation("settings");

		return (
			<div className="space-y-6">
				<SettingsSection title={t("bgg.title")} description={t("bgg.description")}>
					<BgtStatus
						variant={bggStatus.isConfigured ? "success" : "warning"}
						title={bggStatus.isConfigured ? t("bgg.status.configured") : t("bgg.status.not-configured")}
						description={
							bggStatus.isConfigured
								? bggStatus.isReadOnly
									? t("bgg.status.source-env")
									: t("bgg.status.source-db")
								: t("bgg.status.not-configured-description")
						}
					/>
					{!bggStatus.isReadOnly && bggStatus.isConfigured && (
						<div className="flex flex-row gap-4 items-center">
							<BgtButton
								variant="error"
								onClick={() => {
									form.setFieldValue("bggApiKey", null);
									bggStatus.isConfigured = false;
								}}
							>
								{t("bgg.api-key.clear")}
							</BgtButton>
							<form.Subscribe selector={(state: { values: { bggApiKey?: string } }) => state.values.bggApiKey}>
								{(bggApiKey?: string) =>
									bggApiKey === null ? (
										<BgtText size="1" color="white" opacity={50}>
											{t("bgg.api-key.clear-hint")}
										</BgtText>
									) : null
								}
							</form.Subscribe>
						</div>
					)}
					{bggStatus.isReadOnly ? (
						<BgtStatus
							variant="info"
							title={t("bgg.api-key.read-only")}
							description={t("bgg.api-key.read-only-description")}
						/>
					) : (
						<div className="flex-1">
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
					)}

					<BgtStatus
						variant="info"
						title={t("bgg.api-key.help-title")}
						description={
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
						}
						icon={Info}
					/>
				</SettingsSection>

				<SettingsSection title={t("changedetection.title")} description={t("changedetection.description")}>
					<BgtStatus
						variant={changeDetectionStatus.isConfigured ? "success" : "warning"}
						title={
							changeDetectionStatus.isConfigured
								? t("changedetection.status.configured")
								: t("changedetection.status.not-configured")
						}
						description={
							changeDetectionStatus.isConfigured
								? t("changedetection.status.source-db")
								: t("changedetection.status.not-configured-description")
						}
					/>

					<div className="flex-1">
						<form.Field name="changeDetectionBaseUrl">
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

					{changeDetectionStatus.isConfigured ? (
						<div className="flex flex-row gap-4 items-center">
							<BgtButton
								variant="error"
								onClick={() => {
									form.setFieldValue("changeDetectionApiKey", null);
									changeDetectionStatus.isConfigured = false;
								}}
							>
								{t("changedetection.api-key.clear")}
							</BgtButton>
							<form.Subscribe
								selector={(state: { values: { changeDetectionApiKey?: string } }) => state.values.changeDetectionApiKey}
							>
								{(changeDetectionApiKey?: string) =>
									changeDetectionApiKey === null ? (
										<BgtText size="1" color="white" opacity={50}>
											{t("changedetection.api-key.clear-hint")}
										</BgtText>
									) : null
								}
							</form.Subscribe>
						</div>
					) : (
						<div className="flex-1">
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
					)}

					<BgtStatus
						variant="info"
						title={t("changedetection.help-title")}
						description={t("changedetection.help-description")}
						icon={Info}
					/>
				</SettingsSection>
			</div>
		);
	},
});
