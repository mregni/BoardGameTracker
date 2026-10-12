import type { SettingsCategory } from "../-components/SettingsSidebar";

const userCategories: SettingsCategory[] = ["account", "sso"];

export const resolveSettingsCategory = (
	tab: SettingsCategory | undefined,
	canManageSettings: boolean,
): SettingsCategory => {
	const fallback: SettingsCategory = canManageSettings ? "general" : "account";
	if (!tab) return fallback;
	if (!canManageSettings && !userCategories.includes(tab)) return fallback;
	return tab;
};
