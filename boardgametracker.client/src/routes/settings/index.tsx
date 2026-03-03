import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import CogIcon from "@/assets/icons/cog.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { useAppForm } from "@/hooks/form";
import { usePermissions } from "@/hooks/usePermissions";
import { type Settings, SettingsSchema } from "@/models";
import { getEnvironment, getLanguages, getSettings } from "@/services/queries/settings";
import { handleFormSubmit } from "@/utils/formUtils";
import { AccountSettings } from "./-components/AccountSettings";
import { AdvancedSettings } from "./-components/AdvancedSettings";
import { GameNightsSettings } from "./-components/GameNightsSettings";
import { GeneralSettings } from "./-components/GeneralSettings";
import { type SettingsCategory, SettingsSidebar } from "./-components/SettingsSidebar";
import { ShelfOfShameSettings } from "./-components/ShelfOfShameSettings";
import { useSettingsData } from "./-hooks/useSettingsData";
import { settingsFormOpts } from "./-utils/settingsFormOpts";

export const Route = createFileRoute("/settings/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
		queryClient.prefetchQuery(getLanguages());
		queryClient.prefetchQuery(getEnvironment());
	},
});

function RouteComponent() {
	const { settings, saveSettings, isSaving, languages } = useSettingsData();

	if (settings === undefined) {
		return (
			<div className="min-h-full flex items-center justify-center">
				<BgtLoadingSpinner />
			</div>
		);
	}

	return (
		<SettingsPageContent settings={settings} languages={languages} isSaving={isSaving} saveSettings={saveSettings} />
	);
}

interface SettingsPageContentProps {
	settings: Settings;
	languages: { key: string; translationKey: string }[];
	isSaving: boolean;
	saveSettings: (settings: Settings) => Promise<Settings>;
}

function SettingsPageContent({ settings, languages, isSaving, saveSettings }: SettingsPageContentProps) {
	const { canManageSettings } = usePermissions();
	const [activeCategory, setActiveCategory] = useState<SettingsCategory>(canManageSettings ? "general" : "account");
	const { t } = useTranslation();

	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: {
			uiLanguage: settings.uiLanguage,
			dateFormat: settings.dateFormat,
			timeFormat: settings.timeFormat,
			currency: settings.currency,
			statistics: settings.statistics,
			updateCheckEnabled: settings.updateCheckEnabled,
			versionTrack: settings.versionTrack,
			shelfOfShameEnabled: settings.shelfOfShameEnabled,
			shelfOfShameMonthsLimit: settings.shelfOfShameMonthsLimit,
			publicUrl: settings.publicUrl,
			gameNightsEnabled: settings.gameNightsEnabled,
			rsvpAuthenticationEnabled: settings.rsvpAuthenticationEnabled,
		},
		onSubmit: async ({ value }) => {
			const validatedData = SettingsSchema.parse(value);
			await saveSettings(validatedData);
		},
	});

	const renderContent = () => {
		switch (activeCategory) {
			case "general":
				return <GeneralSettings form={form} languages={languages} disabled={isSaving} />;
			case "shelf-of-shame":
				return <ShelfOfShameSettings form={form} disabled={isSaving} />;
			case "game-nights":
				return <GameNightsSettings form={form} disabled={isSaving} />;
			case "advanced":
				return <AdvancedSettings form={form} disabled={isSaving} />;
			case "account":
				return <AccountSettings />;
			default:
				return <GeneralSettings form={form} languages={languages} disabled={isSaving} />;
		}
	};

	return (
		<BgtPage>
			<BgtPageHeader header={"Settings"} icon={CogIcon} />
			<BgtPageContent>
				<div className="flex flex-col lg:flex-row">
					<SettingsSidebar activeCategory={activeCategory} onCategoryChange={setActiveCategory} canManageSettings={canManageSettings} />

					<div className="flex-1">
						<form onSubmit={handleFormSubmit(form)}>
							<div className="flex flex-col gap-4 xl:gap-6 lg:pl-4 xl:pl-6 pt-4 lg:pt-0">{renderContent()}</div>
							{activeCategory !== "account" && (
								<div className="mt-6 pt-4 lg:ml-4 xl:ml-6 border-t border-white/10">
									<div className="flex justify-between flex-wrap gap-3 items-start">
										<BgtButton onClick={form.handleSubmit} type="submit" disabled={isSaving}>
											{t("settings.save.button")}
										</BgtButton>
									</div>
								</div>
							)}
						</form>
					</div>
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
