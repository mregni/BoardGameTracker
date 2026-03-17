import type { ActivePage, MenuSection } from "./types";

export const menuItems: Record<ActivePage, MenuSection> = {
	"getting-started": {
		titleKey: "sidebar:getting-started.title",
		path: "/documentation/getting-started",
		items: [
			{
				id: "quick-start-guide",
				labelKey: "sidebar:getting-started.quick-start-guide",
			},
			{ id: "docker", labelKey: "sidebar:getting-started.docker" },
			{
				id: "environment-variables",
				labelKey: "sidebar:getting-started.environment-variables",
			},
			{ id: "proxy-setup", labelKey: "sidebar:getting-started.proxy-setup" },
		],
	},
	"user-guide": {
		titleKey: "sidebar:user-guide.title",
		path: "/documentation/user-guide",
		items: [
			{ id: "games", labelKey: "sidebar:user-guide.games" },
			{ id: "players", labelKey: "sidebar:user-guide.players" },
			{ id: "locations", labelKey: "sidebar:user-guide.locations" },
			{ id: "add-new-session", labelKey: "sidebar:user-guide.add-new-session" },
			{ id: "loans", labelKey: "sidebar:user-guide.loans" },
			{ id: "compare", labelKey: "sidebar:user-guide.compare" },
			{ id: "game-nights", labelKey: "sidebar:user-guide.game-nights" },
			{ id: "settings", labelKey: "sidebar:user-guide.settings" },
			{ id: "authentication", labelKey: "sidebar:user-guide.authentication" },
		],
	},
	extra: {
		titleKey: "sidebar:extra.title",
		path: "/documentation/extra",
		items: [
			{ id: "development", labelKey: "sidebar:extra.development" },
			{ id: "translations", labelKey: "sidebar:extra.translations" },
			{ id: "bugs-features", labelKey: "sidebar:extra.bugs-features" },
			{ id: "logging", labelKey: "sidebar:extra.logging" },
		],
	},
};
