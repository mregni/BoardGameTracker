export interface GameManual {
	id: number;
	gameId: number;
	title: string;
	fileSizeBytes: number;
	uploadDate: Date;
	contentType: string;
}

export interface GameNightManuals {
	gameId: number;
	gameTitle: string;
	manuals: GameManual[];
}
