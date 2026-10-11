interface ActivityItem {
	name: string;
	playCount: number;
}

export const sortByActivity = <T extends ActivityItem>(items: T[]): T[] =>
	[...items].sort((a, b) => b.playCount - a.playCount || a.name.localeCompare(b.name));
