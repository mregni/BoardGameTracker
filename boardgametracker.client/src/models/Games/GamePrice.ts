export interface GamePrice {
	gameId: number;
	watchId: string | null;
	available: boolean;
	inStock: boolean | null;
	price: number | null;
	fetchedAt: string | null;
}
