import type { AnyFieldApi } from "@tanstack/react-form";
import { Trans, useTranslation } from "react-i18next";

import { BgtInputField } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import type { BggConfigStatus } from "@/models";
import { withForm } from "@/hooks/form";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { SettingsSection } from "./SettingsSection";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import Info from "@/assets/icons/info.svg?react";

export const BggSettings = withForm({
	...settingsFormOpts,
	props: {
		disabled: false,
		bggStatus: {
			isConfigured: false,
			isReadOnly: false,
			source: "db",
		} as BggConfigStatus,
	},
	render: function Render({ form, disabled, bggStatus }: { form: any; disabled: boolean; bggStatus: BggConfigStatus }) {
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
									link: (
										<a
											href="https://boardgamegeek.com/applications/create"
											target="_blank"
											rel="noopener noreferrer"
											className="text-cyan-400 hover:text-cyan-300 underline"
										>
											link
										</a>
									),
								}}
							/>
						}
						icon={Info}
					/>
				</SettingsSection>
			</div>
		);
	},
});
