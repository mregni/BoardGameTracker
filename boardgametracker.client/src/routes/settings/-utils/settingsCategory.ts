import { z } from "zod";
import type { SettingsCategory } from "../-components/SettingsSidebar";

const settingsCategories = [
	"general",
	"shelf-of-shame",
	"game-nights",
	"integrations",
	"advanced",
	"account",
	"sso",
] as const satisfies readonly SettingsCategory[];

const tabAliases: Record<string, SettingsCategory> = { bgg: "integrations" };

export const settingsSearchSchema = z.object({
	tab: z
		.preprocess(
			(tab) => (typeof tab === "string" ? (tabAliases[tab] ?? tab) : tab),
			z.enum(settingsCategories).optional(),
		)
		.catch(undefined),
});

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
