export interface RagCitation {
	manualId: number;
	title: string;
	page: number | null;
	snippet: string;
	score: number;
	imageUrl: string | null;
}

export interface RagAnswer {
	answer: string;
	hasContext: boolean;
	citations: RagCitation[];
}
