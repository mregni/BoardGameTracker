export interface ShameGameNight {
	id: number;
	title: string;
	startDate: Date;
}

export interface ShameStatistics {
	count: number;
	totalValue: number | null;
	averageValue: number | null;
	pricedGameCount: number;
	months: number;
	ownedCount: number;
	collectionValue: number;
	neverPlayedCount: number;
	neverPlayedValue: number;
	plannedCount: number;
	nextGameNight: ShameGameNight | null;
}
