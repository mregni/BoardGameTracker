export interface Shame {
	id: number;
	title: string;
	image: string | null;
	additionDate: Date | null;
	price: number | null;
	lastSessionDate: Date | null;
	playCount: number;
	minPlayers: number | null;
	maxPlayers: number | null;
	minPlayTime: number | null;
	maxPlayTime: number | null;
	plannedGameNightId: number | null;
	plannedFor: Date | null;
}
