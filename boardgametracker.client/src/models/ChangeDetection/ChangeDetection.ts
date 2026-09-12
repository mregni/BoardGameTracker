export interface ChangeDetectionWatchInfo {
	url: string;
	title: string | null;
	lastCheckedAt: string | null;
}

export interface ChangeDetectionConnectionTest {
	ok: boolean;
	version: string | null;
}
