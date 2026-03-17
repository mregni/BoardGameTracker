export interface MenuItem {
	id: string;
	labelKey: string;
}

export interface MenuSection {
	titleKey: string;
	path: string;
	items: MenuItem[];
}

export type ActivePage = "getting-started" | "user-guide" | "extra";
