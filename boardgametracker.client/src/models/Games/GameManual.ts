export type ManualIndexStatus = "pending" | "indexing" | "indexed" | "failed";

export interface GameManual {
	id: number;
	gameId: number;
	title: string;
	fileSizeBytes: number;
	uploadDate: Date;
	contentType: string;
	indexStatus: ManualIndexStatus;
	indexedChunkCount: number;
	indexError: string | null;
	indexedDate: Date | string | null;
}

export interface GameNightManuals {
	gameId: number;
	gameTitle: string;
	manuals: GameManual[];
}
