import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import CogIcon from "@/assets/icons/cog.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { useAppForm } from "@/hooks/form";
import { usePermissions } from "@/hooks/usePermissions";
import { type Settings, SettingsSchema } from "@/models";
import { getLanguages, getSettings } from "@/services/queries/settings";
import { handleFormSubmit } from "@/utils/formUtils";
import { AccountSettings } from "./-components/AccountSettings";
import { AdvancedSettings } from "./-components/AdvancedSettings";
import { BggSettings } from "./-components/BggSettings";
import { DangerZoneSection } from "./-components/DangerZoneSection";
import { GameNightsSettings } from "./-components/GameNightsSettings";
import { GeneralSettings } from "./-components/GeneralSettings";
import { type SettingsCategory, SettingsSidebar } from "./-components/SettingsSidebar";
import { ShelfOfShameSettings } from "./-components/ShelfOfShameSettings";
import { SsoSettings } from "./-components/SsoSettings";
import { useSettingsData } from "./-hooks/useSettingsData";
import { resolveSettingsCategory } from "./-utils/settingsCategory";
import { settingsFormOpts } from "./-utils/settingsFormOpts";

const settingsCategories = ["general", "shelf-of-shame", "game-nights", "bgg", "advanced", "account", "sso"] as const;

const settingsSearchSchema = z.object({
	tab: z.enum(settingsCategories).optional().catch(undefined),
});

export const Route = createFileRoute("/settings/")({
	component: RouteComponent,
	validateSearch: settingsSearchSchema,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
		queryClient.prefetchQuery(getLanguages());
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
	const { tab } = Route.useSearch();
	const navigate = useNavigate();
	const activeCategory = resolveSettingsCategory(tab, canManageSettings);
	const setActiveCategory = (category: SettingsCategory) =>
		navigate({ to: "/settings", search: { tab: category }, replace: true });
	const { t } = useTranslation("settings");

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
			bggApiKey: settings.bggApiKey,
			changeDetectionBaseUrl: settings.changeDetectionBaseUrl,
			changeDetectionApiKey: settings.changeDetectionApiKey,
		},
		onSubmit: async ({ value }) => {
			const validatedData = SettingsSchema.parse(value);
			await saveSettings({
				...validatedData,
				bggStatus: settings.bggStatus,
				changeDetectionStatus: settings.changeDetectionStatus,
				emailEnabled: settings.emailEnabled,
				ragEnabled: settings.ragEnabled,
				environmentOverrides: settings.environmentOverrides,
			});
		},
	});

	const overrides = settings.environmentOverrides ?? {};

	const renderContent = () => {
		switch (activeCategory) {
			case "general":
				return <GeneralSettings form={form} languages={languages} disabled={isSaving} overrides={overrides} />;
			case "shelf-of-shame":
				return <ShelfOfShameSettings form={form} disabled={isSaving} overrides={overrides} />;
			case "game-nights":
				return <GameNightsSettings form={form} disabled={isSaving} overrides={overrides} />;
			case "bgg":
				return (
					<BggSettings
						form={form}
						disabled={isSaving}
						bggStatus={settings.bggStatus}
						changeDetectionStatus={settings.changeDetectionStatus}
					/>
				);
			case "advanced":
				return <AdvancedSettings form={form} disabled={isSaving} overrides={overrides} />;
			case "account":
				return <AccountSettings />;
			case "sso":
				return <SsoSettings />;
			default:
				return <GeneralSettings form={form} languages={languages} disabled={isSaving} overrides={overrides} />;
		}
	};

	const content = <div className="flex flex-col gap-4 xl:gap-6 lg:pl-4 xl:pl-6 pt-4 lg:pt-0">{renderContent()}</div>;

	return (
		<BgtPage>
			<BgtPageHeader header={t("common:settings")} icon={CogIcon} />
			<BgtPageContent>
				<div className="flex flex-col lg:flex-row">
					<SettingsSidebar
						activeCategory={activeCategory}
						onCategoryChange={setActiveCategory}
						canManageSettings={canManageSettings}
					/>

					<div className="flex-1">
						{activeCategory === "account" || activeCategory === "sso" ? (
							content
						) : (
							<form onSubmit={handleFormSubmit(form)}>
								{content}
								<div className="mt-6 pt-4 lg:ml-4 xl:ml-6 border-t border-white/10">
									<div className="flex justify-between flex-wrap gap-3 items-start">
										<BgtButton type="submit" disabled={isSaving}>
											{t("save.button")}
										</BgtButton>
									</div>
								</div>
							</form>
						)}
						{activeCategory === "advanced" && (
							<div className="mt-6 lg:ml-4 xl:ml-6">
								<DangerZoneSection />
							</div>
						)}
					</div>
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
